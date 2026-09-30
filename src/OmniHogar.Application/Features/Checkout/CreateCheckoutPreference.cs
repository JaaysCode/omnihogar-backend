using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniHogar.Application.Common.Interfaces;
using OmniHogar.Domain.Entities;
using ValidationException = OmniHogar.Domain.Exceptions.ValidationException;

namespace OmniHogar.Application.Features.Checkout;

/// <summary>Delivery address entered at checkout (HU-08 crit. 1).</summary>
public record CustomerAddressInput(string Address, string City, string? Neighborhood, string? Reference);

/// <summary>
/// Turns the client's active cart into an order and starts a Stripe Checkout payment
/// (HU-08 + HU-09). <see cref="PaymentMethod"/> is one of the values <see cref="Payment"/>
/// already accepts: card, pse, wallet — recorded for business reporting even though every one
/// of them is charged as a card through Stripe today (see <c>StripeCheckoutClient</c>).
/// </summary>
public record CreateCheckoutPreferenceCommand(CustomerAddressInput Address, string PaymentMethod) : IRequest<CheckoutDto>;

public class CreateCheckoutPreferenceCommandValidator : AbstractValidator<CreateCheckoutPreferenceCommand>
{
    private static readonly string[] AllowedPaymentMethods = ["card", "pse", "wallet"];

    public CreateCheckoutPreferenceCommandValidator()
    {
        RuleFor(x => x.Address.Address)
            .NotEmpty().WithMessage("La dirección es obligatoria.")
            .MaximumLength(255).WithMessage("La dirección debe tener como máximo 255 caracteres.");

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("La ciudad es obligatoria.")
            .MaximumLength(100).WithMessage("La ciudad debe tener como máximo 100 caracteres.");

        RuleFor(x => x.Address.Neighborhood)
            .MaximumLength(100).WithMessage("El barrio debe tener como máximo 100 caracteres.");

        RuleFor(x => x.Address.Reference)
            .MaximumLength(255).WithMessage("La referencia debe tener como máximo 255 caracteres.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Selecciona un método de pago.")
            .Must(m => AllowedPaymentMethods.Contains(m)).WithMessage("El método de pago no es válido.");
    }
}

public class CreateCheckoutPreferenceCommandHandler : IRequestHandler<CreateCheckoutPreferenceCommand, CheckoutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayClient _paymentGateway;
    private readonly IPaymentGatewayUrls _urls;

    public CreateCheckoutPreferenceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPaymentGatewayClient paymentGateway,
        IPaymentGatewayUrls urls)
    {
        _context = context;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _urls = urls;
    }

    public async Task<CheckoutDto> Handle(CreateCheckoutPreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUser.UserId!);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "active", cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Cart"] = ["Tu carrito está vacío."],
            });
        }

        // Single-facility MVP — same resolution as AddProductStock/RegisterStoreSale.
        var facility = await _context.Facilities.OrderBy(f => f.Name).FirstOrDefaultAsync(cancellationToken);
        if (facility is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Cart"] = ["No hay ninguna bodega o tienda configurada para despachar el pedido."],
            });
        }

        foreach (var line in cart.Items)
        {
            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ProductId == line.ProductId && i.FacilityId == facility.Id, cancellationToken);
            var available = inventory?.AvailableQuantity ?? 0;

            if (available < line.Quantity)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["Cart"] = [$"No hay suficiente inventario de '{line.Product.Name}'. Disponible: {available}."],
                });
            }
        }

        var address = new CustomerAddress
        {
            UserId = userId,
            Address = request.Address.Address,
            City = request.Address.City,
            Neighborhood = request.Address.Neighborhood,
            Reference = request.Address.Reference,
        };
        _context.CustomerAddresses.Add(address);

        // Product.Price is the final, IVA-inclusive price shown in the catalog/cart — Colombian
        // retail prices must already include tax, so nothing is added here. What the buyer sees
        // at checkout is exactly what Stripe charges.
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            FacilityId = facility.Id,
            ShippingAddress = address,
            Channel = "web",
            Status = "pending_payment",
            Subtotal = subtotal,
            Total = subtotal,
        };

        foreach (var line in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Subtotal = line.UnitPrice * line.Quantity,
            });
        }

        _context.Orders.Add(order);

        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethod = request.PaymentMethod,
            Amount = order.Total,
            Status = "pending",
        };
        _context.Payments.Add(payment);

        // Persist before calling the gateway: if the call below fails, the order and payment
        // already exist — nothing is lost (HU-09 crit. 3).
        await _context.SaveChangesAsync(cancellationToken);

        string initPoint;
        try
        {
            var resultUrl = $"{_urls.FrontendBaseUrl}/checkout/result?order={order.Id}";
            var preference = await _paymentGateway.CreatePreferenceAsync(new PaymentGatewayPreferenceRequest
            {
                Title = $"Pedido {order.OrderNumber}",
                UnitPrice = order.Total,
                ExternalReference = order.Id.ToString(),
                PayerEmail = _currentUser.Email,
                SuccessUrl = resultUrl,
                FailureUrl = resultUrl,
                PendingUrl = resultUrl,
                NotificationUrl = $"{_urls.BackendPublicBaseUrl}/api/checkout/webhook",
            }, cancellationToken);

            initPoint = preference.InitPoint;
        }
        catch (Exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Gateway"] = ["No fue posible conectar con la pasarela de pago. Tu pedido quedó guardado; puedes intentarlo de nuevo."],
            });
        }

        return new CheckoutDto { OrderId = order.Id, OrderNumber = order.OrderNumber, InitPoint = initPoint };
    }

    private static string GenerateOrderNumber() =>
        $"WEB-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}";
}
