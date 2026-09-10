using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Cart;

/// <summary>The current client's active cart (HU-05 crit. 1-3). Never 404s — returns an empty cart.</summary>
public record GetCartQuery : IRequest<CartDto>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCartQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return new CartDto();
        }

        var cart = await _context.Carts
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Status == "active")
            .Select(c => new CartDto
            {
                Id = c.Id,
                Items = c.Items
                    .OrderBy(ci => ci.Product.Name)
                    .Select(ci => new CartItemDto
                    {
                        ProductId = ci.ProductId,
                        Sku = ci.Product.Sku,
                        Name = ci.Product.Name,
                        ImageUrl = ci.Product.ImageUrl,
                        UnitPrice = ci.UnitPrice,
                        Quantity = ci.Quantity,
                        Subtotal = ci.UnitPrice * ci.Quantity,
                        AvailableQuantity = _context.Inventory
                            .Where(i => i.ProductId == ci.ProductId)
                            .Sum(i => (int?)i.AvailableQuantity) ?? 0,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cart is null)
        {
            return new CartDto();
        }

        cart.Subtotal = cart.Items.Sum(i => i.Subtotal);
        cart.Total = cart.Subtotal;
        cart.ItemCount = cart.Items.Sum(i => i.Quantity);
        return cart;
    }
}
