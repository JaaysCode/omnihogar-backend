using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Products;

/// <summary>
/// Manually add units to a product's stock (HU inventario — "Agregar Unidades"). MVP has a single
/// facility, so the target isn't picked by the caller: it resolves to that facility the same way
/// <see cref="CreateProductCommandHandler"/> seeds initial stock.
/// </summary>
public record AddProductStockCommand(Guid ProductId, int Quantity, string? Reason) : IRequest;

public class AddProductStockCommandValidator : AbstractValidator<AddProductStockCommand>
{
    public AddProductStockCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
        RuleFor(x => x.Reason).MaximumLength(255).WithMessage("El motivo debe tener como máximo 255 caracteres.");
    }
}

public class AddProductStockCommandHandler : IRequestHandler<AddProductStockCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddProductStockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AddProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw NotFoundException.Producto(request.ProductId);
        }

        // Single-facility MVP: no facility picker in the UI yet, so every manual stock add lands
        // in the one facility that exists (see the SeedDefaultFacility migration). Once multiple
        // facilities are a real feature, this becomes a required FacilityId on the command instead.
        var facility = await _context.Facilities.OrderBy(f => f.Name).FirstOrDefaultAsync(cancellationToken);
        if (facility is null)
        {
            throw new OmniHogar.Domain.Exceptions.ValidationException(
                new Dictionary<string, string[]> { ["facility"] = new[] { "No hay ninguna bodega o tienda configurada para recibir el stock." } });
        }

        var inventory = await _context.Inventory
            .FirstOrDefaultAsync(i => i.ProductId == product.Id && i.FacilityId == facility.Id, cancellationToken);
        if (inventory is null)
        {
            inventory = new Inventory { ProductId = product.Id, FacilityId = facility.Id, AvailableQuantity = 0 };
            _context.Inventory.Add(inventory);
        }

        inventory.AvailableQuantity += request.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        _context.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = product.Id,
            FacilityId = facility.Id,
            Type = "inbound",
            Quantity = request.Quantity,
            Reason = request.Reason,
            UserId = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : Guid.Empty,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
