using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Application.Features.Products;
using OmniHogar.Domain.Entities;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Chatbot;

/// <summary>One product/quantity pair in the customer's in-progress chat order (HU-19).</summary>
public record DraftItemInput(Guid ProductId, int Quantity);

/// <summary>
/// One turn of the shopping-assistant chat (HU-19). The LLM (via <see cref="ILlmClient"/>) only
/// extracts intent from the customer's free-text message — this handler decides and executes
/// what actually happens and composes the reply text itself, so behavior stays deterministic
/// and testable regardless of model phrasing.
/// </summary>
public record SendChatMessageCommand(Guid? ConversationId, string Message, IReadOnlyList<DraftItemInput> DraftItems)
    : IRequest<ChatTurnResultDto>;

public class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Escribe un mensaje.");

        RuleForEach(x => x.DraftItems).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
        });
    }
}

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatTurnResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILlmClient _llm;
    private readonly IRequestHandler<SearchProductsQuery, List<ProductDto>> _searchProducts;

    public SendChatMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILlmClient llm,
        IRequestHandler<SearchProductsQuery, List<ProductDto>> searchProducts)
    {
        _context = context;
        _currentUser = currentUser;
        _llm = llm;
        _searchProducts = searchProducts;
    }

    public async Task<ChatTurnResultDto> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var customerId = Guid.Parse(_currentUser.UserId!);

        var conversation = request.ConversationId is { } id
            ? await _context.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId, cancellationToken)
            : null;

        var isNewConversation = conversation is null;
        conversation ??= new Conversation { CustomerId = customerId, Channel = "web", Status = "open" };
        if (isNewConversation)
        {
            _context.Conversations.Add(conversation);
        }

        _context.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            Sender = "customer",
            Content = request.Message,
        });

        var draftProductIds = request.DraftItems.Select(d => d.ProductId).ToList();
        var draftProducts = await _context.Products
            .Where(p => draftProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var draftSummaries = request.DraftItems
            .Where(d => draftProducts.ContainsKey(d.ProductId))
            .Select(d => new DraftItemSummary(draftProducts[d.ProductId].Name, d.Quantity))
            .ToList();

        var intent = await _llm.InterpretAsync(request.Message, draftSummaries, cancellationToken);

        string reply;
        List<ProductDto>? matchedProducts = null;
        ChatOrderCreatedDto? orderCreated = null;

        switch (intent.Type)
        {
            case ChatIntentType.SearchProduct:
                matchedProducts = await _searchProducts.Handle(new SearchProductsQuery(intent.ProductQuery, null), cancellationToken);
                reply = ComposeSearchReply(intent.ProductQuery, matchedProducts);
                break;

            case ChatIntentType.ConfirmOrder:
                (reply, orderCreated) = await ConfirmOrderAsync(conversation, request.DraftItems, customerId, cancellationToken);
                break;

            default:
                reply = "No entendí bien. Cuéntame qué producto estás buscando, o escribe \"confirmar\" cuando quieras cerrar tu pedido.";
                break;
        }

        _context.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            Sender = "bot",
            Content = reply,
        });

        await _context.SaveChangesAsync(cancellationToken);

        var draftDto = BuildDraftDto(request.DraftItems, draftProducts);

        return new ChatTurnResultDto(conversation.Id, reply, draftDto, matchedProducts, orderCreated);
    }

    private static string ComposeSearchReply(string? query, List<ProductDto> matches)
    {
        if (matches.Count == 0)
        {
            return string.IsNullOrWhiteSpace(query)
                ? "No encontré productos. ¿Puedes darme más detalles de lo que buscas?"
                : $"No encontré productos que coincidan con \"{query}\". ¿Puedes darme más detalles?";
        }

        var lines = matches.Select(p =>
        {
            var availability = p.InStock == true
                ? $"disponible ({p.AvailableQuantity} unidades)"
                : "sin disponibilidad por ahora";
            return $"- {p.Name}: ${p.Price:N0} — {availability}";
        });

        return $"Encontré esto:\n{string.Join("\n", lines)}\n¿Quieres agregar alguno a tu pedido? Dime cuál y cuántas unidades.";
    }

    private static List<ChatDraftItemDto> BuildDraftDto(IReadOnlyList<DraftItemInput> items, Dictionary<Guid, Product> products)
    {
        return items
            .Where(i => products.ContainsKey(i.ProductId))
            .Select(i =>
            {
                var product = products[i.ProductId];
                var subtotal = product.Price * i.Quantity;
                return new ChatDraftItemDto(product.Id, product.Name, i.Quantity, product.Price, subtotal);
            })
            .ToList();
    }

    private async Task<(string Reply, ChatOrderCreatedDto? OrderCreated)> ConfirmOrderAsync(
        Conversation conversation, IReadOnlyList<DraftItemInput> draftItems, Guid customerId, CancellationToken cancellationToken)
    {
        if (draftItems.Count == 0)
        {
            return ("Todavía no has agregado productos a tu pedido. Dime qué quieres comprar.", null);
        }

        var quantityByProduct = draftItems
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var facility = await _context.Facilities.OrderBy(f => f.Name).FirstOrDefaultAsync(cancellationToken);
        if (facility is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["DraftItems"] = ["No hay ninguna bodega o tienda configurada para vender."],
            });
        }

        var products = await _context.Products
            .Where(p => quantityByProduct.Keys.Contains(p.Id) && p.Status == "active")
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var inventories = await _context.Inventory
            .Where(i => i.FacilityId == facility.Id && quantityByProduct.Keys.Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId, cancellationToken);

        // Validate every line before writing anything (mirrors RegisterStoreSale crit. 2).
        foreach (var (productId, quantity) in quantityByProduct)
        {
            if (!products.TryGetValue(productId, out var product))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["DraftItems"] = ["Uno o más productos ya no están disponibles."],
                });
            }

            var available = inventories.TryGetValue(productId, out var inv) ? inv.AvailableQuantity : 0;
            if (available < quantity)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["DraftItems"] = [$"No hay suficiente inventario de '{product.Name}'. Disponible: {available}."],
                });
            }
        }

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = customerId,
            // Deliberately no AdvisorId — unlike RegisterStoreSale (a POS cashier who is both
            // "customer of record" and "advisor"), this is a real customer's self-service order.
            FacilityId = facility.Id,
            Channel = "chat",
            Status = "payment_approved",
        };

        decimal subtotal = 0m;

        foreach (var (productId, quantity) in quantityByProduct)
        {
            var product = products[productId];
            var lineSubtotal = product.Price * quantity;
            subtotal += lineSubtotal;

            order.Items.Add(new OrderItem
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                Subtotal = lineSubtotal,
            });

            var inventory = inventories[productId];
            inventory.AvailableQuantity -= quantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            _context.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = productId,
                FacilityId = facility.Id,
                Type = "outbound",
                Quantity = quantity,
                Reason = "Pedido por chatbot",
                OrderId = order.Id,
                UserId = customerId,
            });
        }

        order.Subtotal = subtotal;
        order.Total = subtotal;

        _context.Orders.Add(order);
        _context.ConversationOrders.Add(new ConversationOrder { ConversationId = conversation.Id, OrderId = order.Id });
        conversation.Status = "closed";

        var reply = $"¡Listo! Tu pedido {order.OrderNumber} quedó registrado por un total de ${order.Total:N0}.";
        return (reply, new ChatOrderCreatedDto(order.Id, order.OrderNumber, order.Total));
    }

    private static string GenerateOrderNumber() =>
        $"CHAT-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}";
}
