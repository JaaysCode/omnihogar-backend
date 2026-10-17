using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Reports;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Reports;

public class GetSalesByChannelQueryTests
{
    private static async Task<InMemoryApplicationDbContext> Seed(params (string Channel, string Status, decimal Total, DateTime CreatedAt)[] orders)
    {
        var context = InMemoryApplicationDbContext.Create();
        var user = new User { UserType = UserType.customer, FirstName = "A", LastName = "A", Email = "a@example.com", PasswordHash = "h" };
        context.Users.Add(user);
        var n = 0;
        foreach (var (channel, status, total, createdAt) in orders)
        {
            context.Orders.Add(new Order
            {
                OrderNumber = $"ORD-{++n}",
                UserId = user.Id,
                User = user,
                Channel = channel,
                Status = status,
                Subtotal = total,
                Total = total,
                CreatedAt = createdAt,
            });
        }
        await context.SaveChangesAsync(CancellationToken.None);
        return context;
    }

    private static Task<SalesByChannelReportDto> Run(InMemoryApplicationDbContext context, DateOnly? from = null, DateOnly? to = null) =>
        new GetSalesByChannelQueryHandler(context).Handle(new GetSalesByChannelQuery(from, to), CancellationToken.None);

    private static DateTime Day(int month, int day, int hour = 12) => new(2026, month, day, hour, 0, 0, DateTimeKind.Utc);

    // --- Generar reporte / Información del reporte ---

    [Fact]
    public async Task GroupsSalesByChannel_WithCountAndTotalSold()
    {
        var context = await Seed(
            ("web", "payment_approved", 100m, Day(10, 1)),
            ("web", "delivered", 50m, Day(10, 2)),
            ("store", "payment_approved", 30m, Day(10, 3)),
            ("chat", "preparing", 70m, Day(10, 4)));

        var report = await Run(context);

        var web = report.Channels.Single(c => c.Channel == "web");
        Assert.Equal(2, web.SalesCount);
        Assert.Equal(150m, web.TotalSold);
        var store = report.Channels.Single(c => c.Channel == "store");
        Assert.Equal(1, store.SalesCount);
        Assert.Equal(30m, store.TotalSold);
        var chat = report.Channels.Single(c => c.Channel == "chat");
        Assert.Equal(1, chat.SalesCount);
        Assert.Equal(70m, chat.TotalSold);
        Assert.Equal(4, report.TotalSalesCount);
        Assert.Equal(250m, report.TotalSold);
    }

    [Fact]
    public async Task ChannelsWithoutSales_AppearWithZeros_InFixedOrder()
    {
        var context = await Seed(("store", "payment_approved", 30m, Day(10, 3)));

        var report = await Run(context);

        Assert.Equal(["web", "store", "chat"], report.Channels.Select(c => c.Channel));
        Assert.Equal(0, report.Channels[0].SalesCount);
        Assert.Equal(0m, report.Channels[0].TotalSold);
    }

    [Fact]
    public async Task NoOrdersAtAll_ReturnsAnEmptyReport()
    {
        var report = await Run(InMemoryApplicationDbContext.Create());

        Assert.Equal(3, report.Channels.Count);
        Assert.Equal(0, report.TotalSalesCount);
        Assert.Equal(0m, report.TotalSold);
    }

    [Theory]
    [InlineData("pending_payment")]
    [InlineData("payment_rejected")]
    [InlineData("cancelled")]
    public async Task OrdersThatWereNeverPaid_AreNotSales(string status)
    {
        var context = await Seed(
            ("web", status, 999m, Day(10, 1)),
            ("web", "payment_approved", 10m, Day(10, 1)));

        var report = await Run(context);

        Assert.Equal(1, report.TotalSalesCount);
        Assert.Equal(10m, report.TotalSold);
    }

    // --- Filtrar periodo ---

    [Fact]
    public async Task Period_OnlyIncludesSalesInsideItInclusive()
    {
        var context = await Seed(
            ("web", "payment_approved", 1m, Day(9, 30, 23)),   // before
            ("web", "payment_approved", 10m, Day(10, 1, 0)),   // first day, start
            ("store", "payment_approved", 20m, Day(10, 15)),   // inside
            ("chat", "payment_approved", 30m, Day(10, 31, 23)), // last day, late
            ("web", "payment_approved", 2m, Day(11, 1, 0)));   // after

        var report = await Run(context, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal(3, report.TotalSalesCount);
        Assert.Equal(60m, report.TotalSold);
        Assert.Equal(new DateOnly(2026, 10, 1), report.From);
        Assert.Equal(new DateOnly(2026, 10, 31), report.To);
    }

    [Fact]
    public async Task SingleDayPeriod_IncludesThatWholeDay()
    {
        var context = await Seed(
            ("web", "payment_approved", 10m, Day(10, 5, 0)),
            ("web", "payment_approved", 20m, Day(10, 5, 23)),
            ("web", "payment_approved", 99m, Day(10, 6, 0)));

        var report = await Run(context, new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 5));

        Assert.Equal(30m, report.TotalSold);
    }

    [Fact]
    public async Task OpenEndedPeriods_OnlyBoundTheSideGiven()
    {
        var context = await Seed(
            ("web", "payment_approved", 1m, Day(9, 1)),
            ("web", "payment_approved", 2m, Day(10, 1)),
            ("web", "payment_approved", 4m, Day(11, 1)));

        Assert.Equal(6m, (await Run(context, from: new DateOnly(2026, 10, 1))).TotalSold);
        Assert.Equal(3m, (await Run(context, to: new DateOnly(2026, 10, 1))).TotalSold);
    }

    [Fact]
    public async Task PeriodWithoutSales_ReturnsZeros()
    {
        var context = await Seed(("web", "payment_approved", 10m, Day(10, 1)));

        var report = await Run(context, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(0, report.TotalSalesCount);
        Assert.All(report.Channels, c => Assert.Equal(0, c.SalesCount));
    }

    // --- Validación ---

    [Fact]
    public async Task Validator_RejectsFromAfterTo()
    {
        var result = await new GetSalesByChannelQueryValidator()
            .TestValidateAsync(new GetSalesByChannelQuery(new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 1)));

        result.ShouldHaveValidationErrorFor("From");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("2026-10-01", null)]
    [InlineData(null, "2026-10-01")]
    [InlineData("2026-10-01", "2026-10-01")]
    [InlineData("2026-10-01", "2026-10-31")]
    public async Task Validator_AcceptsValidPeriods(string? from, string? to)
    {
        var query = new GetSalesByChannelQuery(from is null ? null : DateOnly.Parse(from), to is null ? null : DateOnly.Parse(to));

        var result = await new GetSalesByChannelQueryValidator().TestValidateAsync(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
