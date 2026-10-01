using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Advance an order's status (HU-14). Writes an <see cref="OrderStatusHistory"/> audit row
/// alongside the update.
/// </summary>
public record UpdateOrderStatusCommand(Guid OrderId, string NewStatus, string? Comment) : IRequest<OrderDetailDto>;

public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly string[] AllowedStatuses =
        ["pending_payment", "payment_approved", "preparing", "packed", "shipped", "delivered", "cancelled", "payment_rejected"];

    // No transition is allowed out of a terminal state.
    private static readonly string[] TerminalStatuses = ["delivered", "cancelled"];

    // Logical forward order for the fulfillment lifecycle — used only to reject regressions
    // (e.g. shipped -> preparing). Skipping steps forward (preparing -> shipped) is allowed,
    // since dispatch realities vary.
    private static readonly string[] ForwardSequence =
        ["pending_payment", "payment_approved", "preparing", "packed", "shipped", "delivered"];

    public UpdateOrderStatusCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.NewStatus)
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("El estado seleccionado no es válido.");

        RuleFor(x => x.OrderId)
            .MustAsync((id, ct) => context.Orders.AnyAsync(o => o.Id == id, ct))
            .WithMessage("El pedido no existe.");

        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
            {
                var currentStatus = await context.Orders
                    .Where(o => o.Id == command.OrderId)
                    .Select(o => o.Status)
                    .FirstOrDefaultAsync(ct);

                return currentStatus is not null && IsAllowedTransition(currentStatus, command.NewStatus);
            })
            .WithMessage("No es posible pasar del estado actual al estado seleccionado.")
            .WithName("NewStatus")
            .When(x => AllowedStatuses.Contains(x.NewStatus), ApplyConditionTo.CurrentValidator);
    }

    private static bool IsAllowedTransition(string current, string next)
    {
        if (current == next || TerminalStatuses.Contains(current))
        {
            return false;
        }

        // Cancellation can happen from any non-terminal state at any point.
        if (next == "cancelled")
        {
            return true;
        }

        // payment_rejected only makes sense straight out of pending_payment.
        if (next == "payment_rejected")
        {
            return current == "pending_payment";
        }

        var currentIndex = Array.IndexOf(ForwardSequence, current);
        var nextIndex = Array.IndexOf(ForwardSequence, next);

        return currentIndex >= 0 && nextIndex > currentIndex;
    }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext context, IMapper mapper, ICurrentUserService currentUser)
    {
        _context = context;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<OrderDetailDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.OrderId);

        var previousStatus = order.Status;
        order.Status = request.NewStatus;

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            PreviousStatus = previousStatus,
            NewStatus = request.NewStatus,
            UserId = Guid.Parse(_currentUser.UserId!),
            Comment = request.Comment,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderDetailDto>(order);
    }
}
