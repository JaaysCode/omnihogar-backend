using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Orders;

/// <summary>One product/quantity pair on a store sale request (HU-06).</summary>
public record StoreSaleItem(Guid ProductId, int Quantity);

/// <summary>
/// Register a walk-in sale at the register (HU-06). Creates the <see cref="Order"/>
/// (<c>channel = "store"</c>), decrements <see cref="Inventory"/> the same way
/// <see cref="Products.AddProductStockCommandHandler"/> increments it (single-facility MVP,
/// <c>InventoryMovement.Type = "outbound"</c>), and returns a receipt.
/// </summary>
public record RegisterStoreSaleCommand(IReadOnlyList<StoreSaleItem> Items) : IRequest<StoreSaleResultDto>;

public class RegisterStoreSaleCommandValidator : AbstractValidator<RegisterStoreSaleCommand>
{
    public RegisterStoreSaleCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Agrega al menos un producto a la venta.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");

            item.RuleFor(i => i.ProductId)
                .MustAsync((id, ct) => context.Products.AnyAsync(p => p.Id == id && p.Status == "active", ct))
                .WithMessage("Uno o más productos no están disponibles.");
        });
    }
}

public class RegisterStoreSaleCommandHandler : IRequestHandler<RegisterStoreSaleCommand, StoreSaleResultDto>
{
    // Product.Price is the final, IVA-inclusive price shown to the public — Colombian retail
    // prices must already include tax, so nothing is added at the register. Tax below is only
    // backed OUT of that price for the receipt/bookkeeping (what to remit to DIAN), never added
    // on top of what the customer pays.
    private const decimal TaxRate = 0.19m;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RegisterStoreSaleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<StoreSaleResultDto> Handle(RegisterStoreSaleCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        // Same product listed twice on the ticket collapses into one line.
        var quantityByProduct = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        // Single-facility MVP — same resolution as AddProductStockCommandHandler.
        var facility = await _context.Facilities.OrderBy(f => f.Name).FirstOrDefaultAsync(cancellationToken);
        if (facility is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Items"] = ["No hay ninguna bodega o tienda configurada para vender."],
            });
        }

        var products = await _context.Products
            .Where(p => quantityByProduct.Keys.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var inventories = await _context.Inventory
            .Where(i => i.FacilityId == facility.Id && quantityByProduct.Keys.Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId, cancellationToken);

        // Validate every line's availability before writing anything (crit. 2).
        foreach (var (productId, quantity) in quantityByProduct)
        {
            var available = inventories.TryGetValue(productId, out var inv) ? inv.AvailableQuantity : 0;
            if (available < quantity)
            {
                var name = products[productId].Name;
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["Items"] = [$"No hay suficiente inventario de '{name}'. Disponible: {available}."],
                });
            }
        }

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            AdvisorId = userId,
            FacilityId = facility.Id,
            Channel = "store",
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
                Reason = "Venta en tienda",
                OrderId = order.Id,
                UserId = userId,
            });
        }

        // subtotal is the sum of Prices already shown to the customer — nothing added here.
        var netBase = Math.Round(subtotal / (1 + TaxRate), 2, MidpointRounding.AwayFromZero);
        var embeddedTax = subtotal - netBase;

        order.Subtotal = subtotal;
        order.Total = subtotal;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return new StoreSaleResultDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Subtotal = order.Subtotal,
            Tax = embeddedTax,
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new StoreSaleItemResultDto
            {
                ProductId = i.ProductId,
                Sku = products[i.ProductId].Sku,
                Name = products[i.ProductId].Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
            }).ToList(),
        };
    }

    private static string GenerateOrderNumber() =>
        $"POS-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}";
}
