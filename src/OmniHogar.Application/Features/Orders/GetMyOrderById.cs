using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Exceptions;

namespace OmniHogar.Application.Features.Orders;

/// <summary>
/// Full detail of one of the authenticated customer's own orders (HU-14 crit. 2). Ownership-scoped
/// — an order that exists but belongs to someone else reports as not found, same as the Checkout
/// status/retry endpoints.
/// </summary>
public record GetMyOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public class GetMyOrderByIdQueryHandler : IRequestHandler<GetMyOrderByIdQuery, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetMyOrderByIdQueryHandler(IApplicationDbContext context, IMapper mapper, ICurrentUserService currentUser)
    {
        _context = context;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<OrderDetailDto> Handle(GetMyOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == request.Id && o.UserId == userId, cancellationToken)
            ?? throw NotFoundException.Pedido(request.Id);

        return _mapper.Map<OrderDetailDto>(order);
    }
}
