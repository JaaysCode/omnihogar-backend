using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.InventoryTransfers;

/// <summary>
/// Confirm a pending transfer: deducts the transferred units from the origin's inventory, logs a
/// <c>transfer</c> movement against the origin, and moves the transfer to <c>in_transit</c>.
/// Stock is re-checked at confirmation — other sales or transfers may have used it since the
/// transfer was registered.
/// </summary>
public record ConfirmInventoryTransferCommand(Guid TransferId) : IRequest<InventoryTransferDto>;

public class ConfirmInventoryTransferCommandHandler : IRequestHandler<ConfirmInventoryTransferCommand, InventoryTransferDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ConfirmInventoryTransferCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<InventoryTransferDto> Handle(ConfirmInventoryTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _context.InventoryTransfers
            .Include(t => t.Product)
            .Include(t => t.SourceFacility)
            .Include(t => t.DestinationFacility)
            .FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken)
            ?? throw NotFoundException.Transferencia(request.TransferId);

        if (transfer.Status != "pending")
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["La transferencia ya fue confirmada."],
            });
        }

        var inventory = await _context.Inventory
            .FirstOrDefaultAsync(i => i.ProductId == transfer.ProductId && i.FacilityId == transfer.SourceFacilityId, cancellationToken);
        var available = inventory?.AvailableQuantity ?? 0;

        if (inventory is null || transfer.Quantity > available)
        {
            throw InventoryTransferMapping.InsufficientStock(transfer.Quantity, available);
        }

        inventory.AvailableQuantity -= transfer.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        _context.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = transfer.ProductId,
            FacilityId = transfer.SourceFacilityId,
            Type = "transfer",
            Quantity = transfer.Quantity,
            Reason = $"Transferencia a {transfer.DestinationFacility.Name}",
            UserId = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : Guid.Empty,
        });

        transfer.Status = "in_transit";

        await _context.SaveChangesAsync(cancellationToken);

        return transfer.ToDto();
    }
}
