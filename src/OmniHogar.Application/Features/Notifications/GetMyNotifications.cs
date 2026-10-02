using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Notifications;

/// <summary>The authenticated user's in-app notifications (topbar bell, HU-13) — newest first,
/// capped at the most recent 50 so the dropdown stays light.</summary>
public record GetMyNotificationsQuery : IRequest<List<NotificationDto>>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private const int MaxResults = 50;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetMyNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IMapper mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<List<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Channel == "in_app")
            .OrderByDescending(n => n.CreatedAt)
            .Take(MaxResults)
            .ProjectTo<NotificationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
