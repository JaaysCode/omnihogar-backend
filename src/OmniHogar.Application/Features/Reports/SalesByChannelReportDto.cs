namespace OmniHogar.Application.Features.Reports;

/// <summary>Sales of one channel within the report period.</summary>
public class ChannelSalesDto
{
    /// <summary>web, store, or chat.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Number of sales (orders) registered through this channel.</summary>
    public int SalesCount { get; set; }

    /// <summary>Total value sold through this channel.</summary>
    public decimal TotalSold { get; set; }
}

/// <summary>
/// Sales grouped by channel for a period. Every channel appears, with zeros when it had no
/// sales, so the report always has the same shape.
/// </summary>
public class SalesByChannelReportDto
{
    /// <summary>First day included, or null when the report isn't bounded below.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Last day included, or null when the report isn't bounded above.</summary>
    public DateOnly? To { get; set; }

    public List<ChannelSalesDto> Channels { get; set; } = new();
    public int TotalSalesCount { get; set; }
    public decimal TotalSold { get; set; }
}
