using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Cart;

/// <summary>Set the quantity of a line already in the cart (HU-05 crit. 2).</summary>
public record UpdateCartItemQuantityCommand(Guid ProductId, int Quantity) : IRequest;

public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.")
            .LessThanOrEqualTo(99).WithMessage("La cantidad no puede superar 99 unidades.");
    }
}

public class UpdateCartItemQuantityCommandHandler : IRequestHandler<UpdateCartItemQuantityCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCartItemQuantityCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "active", cancellationToken);

        var item = cart?.Items.FirstOrDefault(ci => ci.ProductId == request.ProductId);
        if (item is null)
        {
            throw NotFoundException.ItemEnCarrito(request.ProductId);
        }

        var available = await _context.Inventory
            .Where(i => i.ProductId == request.ProductId)
            .SumAsync(i => (int?)i.AvailableQuantity, cancellationToken) ?? 0;

        if (available < request.Quantity)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Quantity"] = [$"Solo quedan {available} unidades disponibles."],
            });
        }

        item.Quantity = request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
