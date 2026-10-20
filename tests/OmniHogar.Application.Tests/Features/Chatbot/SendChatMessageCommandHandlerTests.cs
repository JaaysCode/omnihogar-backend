using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Application.Features.Chatbot;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Tests.Features.Chatbot;

public class SendChatMessageCommandHandlerTests
{
    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, FakeLlmClient Llm, Guid ProductId, Facility Facility)> Seed(
        int available, string productName = "Taladro", decimal price = 100m)
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var facility = new Facility { Name = "Bodega Central", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };

        context.Users.Add(new User { Id = userId, UserType = UserType.customer, Email = $"{userId:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" });
        context.Products.Add(new Product { Id = productId, Sku = "SKU-1", Name = productName, Price = price, Status = "active" });
        context.Facilities.Add(facility);
        context.Inventory.Add(new Inventory { ProductId = productId, FacilityId = facility.Id, AvailableQuantity = available });
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, new FakeLlmClient(), productId, facility);
    }

    private static SendChatMessageCommandHandler Handler(InMemoryApplicationDbContext context, FakeCurrentUserService user, FakeLlmClient llm) =>
        new(context, user, llm, new SearchProductsQueryHandler(context), new RecommendProductsQueryHandler(context));

    [Fact]
    public async Task SearchWithMatch_ReturnsMatchedProductsAndDoesNotCreateAnOrder()
    {
        var (context, user, llm, _, _) = await Seed(available: 5, productName: "Taladro");
        llm.IntentToReturn = new ChatIntent(ChatIntentType.SearchProduct, ProductQuery: "taladro");
        var handler = Handler(context, user, llm);

        var result = await handler.Handle(
            new SendChatMessageCommand(null, "busco un taladro", []), CancellationToken.None);

        var match = Assert.Single(result.MatchedProducts!);
        Assert.Equal("Taladro", match.Name);
        Assert.Null(result.OrderCreated);
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task SearchWithoutMatch_RepliesNotFoundAndDoesNotCreateAnOrder()
    {
        var (context, user, llm, _, _) = await Seed(available: 5, productName: "Taladro");
        llm.IntentToReturn = new ChatIntent(ChatIntentType.SearchProduct, ProductQuery: "nevera");
        var handler = Handler(context, user, llm);

        var result = await handler.Handle(
            new SendChatMessageCommand(null, "busco una nevera", []), CancellationToken.None);

        Assert.Empty(result.MatchedProducts!);
        Assert.Contains("No encontré", result.Reply);
        Assert.Null(result.OrderCreated);
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task ConfirmWithValidStock_CreatesChatOrderAndClosesTheConversation()
    {
        var (context, user, llm, productId, facility) = await Seed(available: 10);
        llm.IntentToReturn = new ChatIntent(ChatIntentType.ConfirmOrder);
        var handler = Handler(context, user, llm);

        var result = await handler.Handle(
            new SendChatMessageCommand(null, "confirmar", [new DraftItemInput(productId, 2)]), CancellationToken.None);

        Assert.NotNull(result.OrderCreated);
        var order = await context.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal("chat", order.Channel);
        Assert.Equal(Guid.Parse(user.UserId!), order.UserId);
        Assert.Null(order.AdvisorId);
        Assert.Equal(facility.Id, order.FacilityId);
        Assert.Equal("payment_approved", order.Status);

        var line = Assert.Single(order.Items);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(2, line.Quantity);

        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(8, inventory.AvailableQuantity);

        var movement = await context.InventoryMovements.SingleAsync();
        Assert.Equal("outbound", movement.Type);
        Assert.Equal(2, movement.Quantity);
        Assert.Equal(order.Id, movement.OrderId);

        var conversationOrder = await context.ConversationOrders.SingleAsync();
        Assert.Equal(order.Id, conversationOrder.OrderId);
        Assert.Equal(result.ConversationId, conversationOrder.ConversationId);

        var conversation = await context.Conversations.SingleAsync();
        Assert.Equal("closed", conversation.Status);
    }

    [Fact]
    public async Task ConfirmWithInsufficientStock_ThrowsValidationExceptionAndWritesNothing()
    {
        var (context, user, llm, productId, _) = await Seed(available: 1);
        llm.IntentToReturn = new ChatIntent(ChatIntentType.ConfirmOrder);
        var handler = Handler(context, user, llm);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new SendChatMessageCommand(null, "confirmar", [new DraftItemInput(productId, 5)]), CancellationToken.None));

        Assert.Empty(context.Orders);
        Assert.Empty(context.InventoryMovements);
        Assert.Empty(context.ConversationOrders);
        var inventory = await context.Inventory.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(1, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task UnknownIntent_RepliesGenericallyAndWritesNoOrder()
    {
        var (context, user, llm, productId, _) = await Seed(available: 5);
        llm.IntentToReturn = new ChatIntent(ChatIntentType.Unknown);
        var handler = Handler(context, user, llm);

        var result = await handler.Handle(
            new SendChatMessageCommand(null, "hola", [new DraftItemInput(productId, 1)]), CancellationToken.None);

        Assert.Null(result.OrderCreated);
        Assert.Null(result.MatchedProducts);
        Assert.Empty(context.Orders);
    }

    // --- HU-28: consultar disponibilidad ---

    [Fact]
    public async Task CheckAvailability_WithStock_RepliesWithUnitsAndCreatesNothing()
    {
        var (context, user, llm, _, _) = await Seed(available: 7, productName: "Taladro");
        llm.IntentToReturn = new ChatIntent(ChatIntentType.CheckAvailability, ProductQuery: "taladro");

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "¿hay taladros?", []), CancellationToken.None);

        Assert.Contains("Taladro", result.Reply);
        Assert.Contains("7 unidades disponibles", result.Reply);
        Assert.Single(result.MatchedProducts!);
        Assert.Null(result.OrderCreated);
        Assert.Empty(context.Orders);
        Assert.Empty(context.InventoryMovements);
    }

    [Fact]
    public async Task CheckAvailability_ProductWithoutStock_SaysThereAreNoUnits()
    {
        var (context, user, llm, _, _) = await Seed(available: 0, productName: "Taladro");
        llm.IntentToReturn = new ChatIntent(ChatIntentType.CheckAvailability, ProductQuery: "taladro");

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "¿hay taladros?", []), CancellationToken.None);

        Assert.Contains("sin existencias", result.Reply);
    }

    [Fact]
    public async Task CheckAvailability_UnknownProduct_TellsNotFoundAndInvitesAnotherQuery()
    {
        var (context, user, llm, _, _) = await Seed(available: 5, productName: "Taladro");
        llm.IntentToReturn = new ChatIntent(ChatIntentType.CheckAvailability, ProductQuery: "nevera");

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "¿hay neveras?", []), CancellationToken.None);

        Assert.Contains("No encontré el producto \"nevera\"", result.Reply);
        Assert.Contains("consultar otro", result.Reply);
        Assert.Empty(result.MatchedProducts!);
    }

    [Fact]
    public async Task CheckAvailability_WithoutAProductName_AsksWhichProduct()
    {
        var (context, user, llm, _, _) = await Seed(available: 5);
        llm.IntentToReturn = new ChatIntent(ChatIntentType.CheckAvailability, ProductQuery: "  ");

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "¿qué hay?", []), CancellationToken.None);

        Assert.Contains("¿De qué producto", result.Reply);
        Assert.Null(result.MatchedProducts);
    }
}
