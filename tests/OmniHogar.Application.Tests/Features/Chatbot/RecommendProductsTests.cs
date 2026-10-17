using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Application.Features.Chatbot;
using OmniHogar.Application.Features.Products;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Chatbot;

public class RecommendProductsQueryHandlerTests
{
    private static async Task<InMemoryApplicationDbContext> SeedCatalog()
    {
        var context = InMemoryApplicationDbContext.Create();
        var cleaning = new ProductCategory { Name = "Limpieza" };
        var kitchen = new ProductCategory { Name = "Cocina" };
        context.ProductCategories.AddRange(cleaning, kitchen);

        var warehouse = new Facility { Name = "Bodega", Type = FacilityType.WAREHOUSE, Address = "Calle 1", City = "Bogotá" };
        context.Facilities.Add(warehouse);

        var products = new[]
        {
            new Product { Sku = "A", Name = "Aspiradora Robot", Description = "Limpia pisos solo", Price = 899_900m, CategoryId = cleaning.Id },
            new Product { Sku = "B", Name = "Trapero Giratorio", Description = "Ideal para limpiar el piso", Price = 59_900m, CategoryId = cleaning.Id },
            new Product { Sku = "C", Name = "Licuadora", Description = "Para batidos", Price = 120_000m, CategoryId = kitchen.Id },
            new Product { Sku = "D", Name = "Escoba descontinuada", Description = "Para barrer", Price = 10_000m, CategoryId = cleaning.Id, Status = "discontinued" },
        };
        context.Products.AddRange(products);
        foreach (var p in products.Take(3))
        {
            context.Inventory.Add(new Inventory { ProductId = p.Id, FacilityId = warehouse.Id, AvailableQuantity = 5 });
        }
        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static Task<List<ProductDto>> Recommend(InMemoryApplicationDbContext context, params string[] keywords) =>
        new RecommendProductsQueryHandler(context).Handle(new RecommendProductsQuery(keywords), CancellationToken.None);

    [Fact]
    public async Task MatchesByNameDescriptionAndCategory()
    {
        var context = await SeedCatalog();

        var result = await Recommend(context, "limpiar");

        // "limpiar" is in Trapero's description; "limpieza" (category) does not contain "limpiar".
        Assert.Equal(["Trapero Giratorio"], result.Select(p => p.Name));
    }

    [Fact]
    public async Task CategoryMatch_ReturnsEveryActiveProductInThatCategory()
    {
        var context = await SeedCatalog();

        var result = await Recommend(context, "limpieza");

        Assert.Equal(["Aspiradora Robot", "Trapero Giratorio"], result.Select(p => p.Name).OrderBy(n => n));
    }

    [Fact]
    public async Task IgnoresCaseAccentsAndPluralS()
    {
        var context = await SeedCatalog();

        var result = await Recommend(context, "ASPIRADÓRAS");

        Assert.Equal(["Aspiradora Robot"], result.Select(p => p.Name));
    }

    [Fact]
    public async Task BestMatchComesFirst_AndDoesNotRecommendDiscontinuedProducts()
    {
        var context = await SeedCatalog();

        var result = await Recommend(context, "piso", "aspiradora", "escoba");

        Assert.Equal("Aspiradora Robot", result[0].Name); // name hit (3) + description hit "pisos" (1)
        Assert.DoesNotContain(result, p => p.Name.Contains("Escoba"));
    }

    [Fact]
    public async Task ReturnsNameAndPrice()
    {
        var context = await SeedCatalog();

        var result = await Recommend(context, "licuadora");

        var match = Assert.Single(result);
        Assert.Equal("Licuadora", match.Name);
        Assert.Equal(120_000m, match.Price);
        Assert.True(match.InStock);
    }

    [Fact]
    public async Task NothingRelated_ReturnsEmpty()
    {
        var context = await SeedCatalog();

        Assert.Empty(await Recommend(context, "bicicleta"));
    }

    [Fact]
    public async Task BlankKeywords_ReturnsEmpty()
    {
        var context = await SeedCatalog();

        Assert.Empty(await Recommend(context, "", "  ", "a"));
    }

    [Fact]
    public async Task CapsTheNumberOfRecommendations()
    {
        var context = InMemoryApplicationDbContext.Create();
        for (var i = 0; i < 8; i++)
        {
            context.Products.Add(new Product { Sku = $"S{i}", Name = $"Lámpara {i}", Price = 10m });
        }
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await Recommend(context, "lampara");

        Assert.Equal(RecommendProductsQueryHandler.MaxRecommendations, result.Count);
    }
}

public class SendChatMessageRecommendationTests
{
    private static async Task<(InMemoryApplicationDbContext Context, FakeCurrentUserService User, FakeLlmClient Llm)> Seed()
    {
        var context = InMemoryApplicationDbContext.Create();
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, UserType = UserType.customer, Email = $"{userId:N}@x.test", FirstName = "C", LastName = "X", PasswordHash = "h" });
        context.Products.Add(new Product { Sku = "A", Name = "Aspiradora Robot", Description = "Limpia pisos", Price = 899_900m });
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, new FakeCurrentUserService { UserId = userId.ToString() }, new FakeLlmClient());
    }

    private static SendChatMessageCommandHandler Handler(InMemoryApplicationDbContext context, FakeCurrentUserService user, FakeLlmClient llm) =>
        new(context, user, llm, new SearchProductsQueryHandler(context), new RecommendProductsQueryHandler(context));

    [Fact]
    public async Task Recommendation_WithMatches_ShowsNameAndPriceOfEachProduct()
    {
        var (context, user, llm) = await Seed();
        llm.IntentToReturn = new ChatIntent(ChatIntentType.RecommendProducts, Keywords: ["aspiradora", "piso"]);

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "recomiéndame algo para limpiar el piso", []), CancellationToken.None);

        Assert.True(result.IsRecommendation);
        var match = Assert.Single(result.MatchedProducts!);
        Assert.Equal("Aspiradora Robot", match.Name);
        Assert.Contains("Aspiradora Robot", result.Reply);
        Assert.Contains("899", result.Reply);
        Assert.Null(result.OrderCreated);
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task Recommendation_WithoutMatches_SaysNothingRelatedWasFound_AndInvitesANewQuery()
    {
        var (context, user, llm) = await Seed();
        llm.IntentToReturn = new ChatIntent(ChatIntentType.RecommendProducts, Keywords: ["bicicleta"]);

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "recomiéndame una bicicleta", []), CancellationToken.None);

        Assert.True(result.IsRecommendation);
        Assert.Empty(result.MatchedProducts!);
        Assert.Contains("No encontré productos relacionados", result.Reply);
        Assert.Contains("nueva consulta", result.Reply);
    }

    [Fact]
    public async Task Recommendation_WithoutKeywords_AsksForMoreDetail()
    {
        var (context, user, llm) = await Seed();
        llm.IntentToReturn = new ChatIntent(ChatIntentType.RecommendProducts);

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "recomiéndame algo", []), CancellationToken.None);

        Assert.Empty(result.MatchedProducts!);
        Assert.Contains("Cuéntame un poco más", result.Reply);
    }

    [Fact]
    public async Task Search_IsNotFlaggedAsRecommendation()
    {
        var (context, user, llm) = await Seed();
        llm.IntentToReturn = new ChatIntent(ChatIntentType.SearchProduct, ProductQuery: "aspiradora");

        var result = await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "busco una aspiradora", []), CancellationToken.None);

        Assert.False(result.IsRecommendation);
    }

    [Fact]
    public async Task Recommendation_IsPersistedInTheConversation()
    {
        var (context, user, llm) = await Seed();
        llm.IntentToReturn = new ChatIntent(ChatIntentType.RecommendProducts, Keywords: ["aspiradora"]);

        await Handler(context, user, llm).Handle(
            new SendChatMessageCommand(null, "recomiéndame una aspiradora", []), CancellationToken.None);

        Assert.Equal(2, context.Messages.Count()); // customer + bot
    }
}
