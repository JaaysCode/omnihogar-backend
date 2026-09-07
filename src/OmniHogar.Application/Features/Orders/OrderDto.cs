namespace OmniHogar.Application.Features.Orders;

/// <summary>Row shape for the cross-channel order consultation list.</summary>
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>web, store, or chat — the channel the order was registered through.</summary>
    public string Channel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}
