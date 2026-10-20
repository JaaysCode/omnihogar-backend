using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Products;

/// <summary>
/// Configures the minimum stock level of a product at a facility (HU-24 crit. 1: "un producto tiene
/// configurado un nivel mínimo"). 0 clears it. Creates the inventory row (with 0 units) if the
/// product has none at that facility yet, so a minimum can be set before the first restock.
/// </summary>
public record SetInventoryMinimumStockCommand(Guid ProductId, Guid FacilityId, int MinimumStock) : IRequest<Unit>;

public class SetInventoryMinimumStockCommandValidator : AbstractValidator<SetInventoryMinimumStockCommand>
{
    public SetInventoryMinimumStockCommandValidator()
    {
        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("El nivel mínimo no puede ser negativo.");
    }
}

public class SetInventoryMinimumStockCommandHandler : IRequestHandler<SetInventoryMinimumStockCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public SetInventoryMinimumStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(SetInventoryMinimumStockCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken))
        {
            throw NotFoundException.Producto(request.ProductId);
        }

        if (!await _context.Facilities.AnyAsync(f => f.Id == request.FacilityId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["facilityId"] = ["La bodega o tienda seleccionada no existe."],
            });
        }

        var inventory = await _context.Inventory
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.FacilityId == request.FacilityId, cancellationToken);
        if (inventory is null)
        {
            inventory = new Inventory { ProductId = request.ProductId, FacilityId = request.FacilityId, AvailableQuantity = 0 };
            _context.Inventory.Add(inventory);
        }

        inventory.MinimumStock = request.MinimumStock;
        inventory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
