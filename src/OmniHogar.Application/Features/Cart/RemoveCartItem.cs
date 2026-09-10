using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Cart;

/// <summary>Drop a line from the current client's cart (HU-05 crit. 3). Idempotent — no error if absent.</summary>
public record RemoveCartItemCommand(Guid ProductId) : IRequest;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RemoveCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return;
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "active", cancellationToken);

        var item = cart?.Items.FirstOrDefault(ci => ci.ProductId == request.ProductId);
        if (item is null)
        {
            return;
        }

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
