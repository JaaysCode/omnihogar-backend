using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;

namespace OmniHogar.Application.Features.Reports;

/// <summary>
/// Sales report grouped by channel (administrador). A "sale" is an order whose payment went
/// through: <c>pending_payment</c>, <c>payment_rejected</c> and <c>cancelled</c> orders are left
/// out. <paramref name="From"/>/<paramref name="To"/> are inclusive UTC calendar days matched
/// against the order's creation date; either may be omitted to leave that side open.
/// </summary>
public record GetSalesByChannelQuery(DateOnly? From, DateOnly? To) : IRequest<SalesByChannelReportDto>;

public class GetSalesByChannelQueryValidator : AbstractValidator<GetSalesByChannelQuery>
{
    public GetSalesByChannelQueryValidator()
    {
        RuleFor(x => x.To)
            .Must((query, to) => to >= query.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("La fecha inicial no puede ser posterior a la fecha final.")
            .OverridePropertyName("From");
    }
}

public class GetSalesByChannelQueryHandler : IRequestHandler<GetSalesByChannelQuery, SalesByChannelReportDto>
{
    /// <summary>Channels an order can be registered through, in report order.</summary>
    public static readonly string[] Channels = ["web", "store", "chat"];

    private static readonly string[] NonSaleStatuses = ["pending_payment", "payment_rejected", "cancelled"];

    private readonly IApplicationDbContext _context;

    public GetSalesByChannelQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesByChannelReportDto> Handle(GetSalesByChannelQuery request, CancellationToken cancellationToken)
    {
        var sales = _context.Orders
            .AsNoTracking()
            .Where(o => !NonSaleStatuses.Contains(o.Status));

        if (request.From is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            sales = sales.Where(o => o.CreatedAt >= start);
        }

        if (request.To is { } to)
        {
            var endExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            sales = sales.Where(o => o.CreatedAt < endExclusive);
        }

        var grouped = await sales
            .GroupBy(o => o.Channel)
            .Select(g => new { Channel = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) })
            .ToListAsync(cancellationToken);

        var rows = Channels
            .Select(channel =>
            {
                var match = grouped.FirstOrDefault(g => g.Channel == channel);
                return new ChannelSalesDto { Channel = channel, SalesCount = match?.Count ?? 0, TotalSold = match?.Total ?? 0m };
            })
            .ToList();

        return new SalesByChannelReportDto
        {
            From = request.From,
            To = request.To,
            Channels = rows,
            TotalSalesCount = rows.Sum(r => r.SalesCount),
            TotalSold = rows.Sum(r => r.TotalSold),
        };
    }
}
