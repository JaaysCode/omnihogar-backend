using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.InventoryTransfers;

/// <summary>
/// Mark an in-transit transfer as received: credits the transferred units to the destination's
/// inventory (creating its row if the product was never stocked there), logs a <c>transfer</c>
/// movement against the destination, and moves the transfer to <c>completed</c>.
/// </summary>
public record ReceiveInventoryTransferCommand(Guid TransferId) : IRequest<InventoryTransferDto>;

public class ReceiveInventoryTransferCommandHandler : IRequestHandler<ReceiveInventoryTransferCommand, InventoryTransferDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReceiveInventoryTransferCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<InventoryTransferDto> Handle(ReceiveInventoryTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _context.InventoryTransfers
            .Include(t => t.Product)
            .Include(t => t.SourceFacility)
            .Include(t => t.DestinationFacility)
            .FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken)
            ?? throw NotFoundException.Transferencia(request.TransferId);

        if (transfer.Status != "in_transit")
        {
            var message = transfer.Status == "completed"
                ? "La transferencia ya fue recibida."
                : "La transferencia debe confirmarse antes de recibirla.";
            throw new ValidationException(new Dictionary<string, string[]> { ["status"] = [message] });
        }

        var inventory = await _context.Inventory
            .FirstOrDefaultAsync(i => i.ProductId == transfer.ProductId && i.FacilityId == transfer.DestinationFacilityId, cancellationToken);
        if (inventory is null)
        {
            inventory = new Inventory { ProductId = transfer.ProductId, FacilityId = transfer.DestinationFacilityId, AvailableQuantity = 0 };
            _context.Inventory.Add(inventory);
        }

        inventory.AvailableQuantity += transfer.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        _context.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = transfer.ProductId,
            FacilityId = transfer.DestinationFacilityId,
            Type = "transfer",
            Quantity = transfer.Quantity,
            Reason = $"Transferencia desde {transfer.SourceFacility.Name}",
            UserId = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : Guid.Empty,
        });

        transfer.Status = "completed";
        transfer.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return transfer.ToDto();
    }
}
