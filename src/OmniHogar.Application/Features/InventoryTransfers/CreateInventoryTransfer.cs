using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.InventoryTransfers;

/// <summary>
/// Register an inventory transfer between two facilities (jefe de bodega). Creates it as
/// <c>pending</c> — stock is not touched until <see cref="ConfirmInventoryTransferCommand"/>.
/// Rejected when the requested quantity exceeds what the origin has available.
/// </summary>
public record CreateInventoryTransferCommand(Guid ProductId, Guid SourceFacilityId, Guid DestinationFacilityId, int Quantity)
    : IRequest<InventoryTransferDto>;

public class CreateInventoryTransferCommandValidator : AbstractValidator<CreateInventoryTransferCommand>
{
    public CreateInventoryTransferCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Selecciona un producto.");
        RuleFor(x => x.SourceFacilityId).NotEmpty().WithMessage("Selecciona la ubicación de origen.");
        RuleFor(x => x.DestinationFacilityId).NotEmpty().WithMessage("Selecciona la ubicación de destino.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
        RuleFor(x => x.DestinationFacilityId)
            .NotEqual(x => x.SourceFacilityId)
            .When(x => x.SourceFacilityId != Guid.Empty && x.DestinationFacilityId != Guid.Empty)
            .WithMessage("El origen y el destino deben ser ubicaciones distintas.");
    }
}

public class CreateInventoryTransferCommandHandler : IRequestHandler<CreateInventoryTransferCommand, InventoryTransferDto>
{
    private readonly IApplicationDbContext _context;

    public CreateInventoryTransferCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryTransferDto> Handle(CreateInventoryTransferCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw NotFoundException.Producto(request.ProductId);

        var facilities = await _context.Facilities
            .Where(f => f.Id == request.SourceFacilityId || f.Id == request.DestinationFacilityId)
            .ToListAsync(cancellationToken);
        var source = facilities.FirstOrDefault(f => f.Id == request.SourceFacilityId);
        var destination = facilities.FirstOrDefault(f => f.Id == request.DestinationFacilityId);

        var errors = new Dictionary<string, string[]>();
        if (source is null)
        {
            errors["sourceFacilityId"] = ["La ubicación de origen no existe."];
        }
        if (destination is null)
        {
            errors["destinationFacilityId"] = ["La ubicación de destino no existe."];
        }
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var available = await _context.Inventory
            .Where(i => i.ProductId == product.Id && i.FacilityId == source!.Id)
            .Select(i => i.AvailableQuantity)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.Quantity > available)
        {
            throw InventoryTransferMapping.InsufficientStock(request.Quantity, available);
        }

        var transfer = new InventoryTransfer
        {
            ProductId = product.Id,
            Product = product,
            SourceFacilityId = source!.Id,
            SourceFacility = source,
            DestinationFacilityId = destination!.Id,
            DestinationFacility = destination,
            Quantity = request.Quantity,
            Status = "pending",
        };
        _context.InventoryTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        return transfer.ToDto();
    }
}
