namespace OmniHogar.Application.Features.Checkout;

/// <summary>Response of creating (or retrying) a Stripe Checkout session (HU-08/HU-09).</summary>
public class CheckoutDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Stripe's hosted checkout URL — the browser does a full-page redirect here.</summary>
    public string InitPoint { get; set; } = string.Empty;
}

/// <summary>Current state of an order's payment, for the /checkout/result page (HU-09).</summary>
public class CheckoutStatusDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Order.Status — pending_payment, payment_approved, payment_rejected, etc.</summary>
    public string OrderStatus { get; set; } = string.Empty;

    /// <summary>Payment.Status — pending, approved, rejected, reversed.</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    public decimal Total { get; set; }

    /// <summary>True when the last attempt to reach the payment gateway failed — the order is still
    /// preserved (HU-09 crit. 3), the client should offer "reintentar"/"actualizar estado".</summary>
    public bool GatewayUnavailable { get; set; }
}
