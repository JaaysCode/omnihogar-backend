using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using CartEntity = OmniHogar.Domain.Entities.Cart;
using CartItemEntity = OmniHogar.Domain.Entities.CartItem;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Cart;

/// <summary>Add a product to the current client's cart, or bump its quantity (HU-05 crit. 1).</summary>
public record AddCartItemCommand(Guid ProductId, int Quantity) : IRequest;

public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.")
            .LessThanOrEqualTo(99).WithMessage("La cantidad no puede superar 99 unidades.");

        RuleFor(x => x.ProductId)
            .MustAsync((id, ct) => context.Products.AnyAsync(p => p.Id == id && p.Status == "active", ct))
            .WithMessage("El producto no está disponible.");
    }
}

public class AddCartItemCommandHandler : IRequestHandler<AddCartItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var product = await _context.Products
            .FirstAsync(p => p.Id == request.ProductId, cancellationToken);

        var available = await _context.Inventory
            .Where(i => i.ProductId == request.ProductId)
            .SumAsync(i => (int?)i.AvailableQuantity, cancellationToken) ?? 0;

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "active", cancellationToken);

        if (cart is null)
        {
            cart = new CartEntity { UserId = userId, Channel = "web", Status = "active" };
            _context.Carts.Add(cart);
        }

        var existing = cart.Items.FirstOrDefault(ci => ci.ProductId == request.ProductId);
        var desired = (existing?.Quantity ?? 0) + request.Quantity;

        if (available <= 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Quantity"] = ["El producto no está disponible."],
            });
        }

        if (available < desired)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Quantity"] = [$"Solo quedan {available} unidades disponibles."],
            });
        }

        if (existing is null)
        {
            cart.Items.Add(new CartItemEntity
            {
                ProductId = request.ProductId,
                Quantity = desired,
                UnitPrice = product.Price,
            });
        }
        else
        {
            existing.Quantity = desired;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two near-simultaneous requests for the same line (double-click, two tabs) can race
            // between the read above and this save — surface a retryable 400 instead of a 500.
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Quantity"] = ["El carrito cambió mientras se procesaba tu solicitud. Inténtalo de nuevo."],
            });
        }
    }
}
