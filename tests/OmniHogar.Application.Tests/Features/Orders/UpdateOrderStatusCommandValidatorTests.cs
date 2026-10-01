using FluentValidation.TestHelper;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Orders;

public class UpdateOrderStatusCommandValidatorTests
{
    private static async Task<(InMemoryApplicationDbContext Context, Guid OrderId)> ContextWithOrder(string status)
    {
        var context = InMemoryApplicationDbContext.Create();
        var customer = new User { UserType = UserType.customer, FirstName = "C", LastName = "R", Email = "c@example.com", PasswordHash = "h" };
        context.Users.Add(customer);
        var order = new Order { OrderNumber = "ORD-1", UserId = customer.Id, User = customer, Channel = "web", Status = status };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);
        return (context, order.Id);
    }

    [Fact]
    public async Task ForwardTransition_HasNoValidationErrors()
    {
        var (context, orderId) = await ContextWithOrder("preparing");
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(orderId, "packed", null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task SkippingStepsForward_HasNoValidationErrors()
    {
        var (context, orderId) = await ContextWithOrder("preparing");
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(orderId, "shipped", null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task InvalidStatusString_ReportsError()
    {
        var (context, orderId) = await ContextWithOrder("preparing");
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(orderId, "bogus", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "El estado seleccionado no es válido.");
    }

    [Fact]
    public async Task NonexistentOrder_ReportsError()
    {
        var context = InMemoryApplicationDbContext.Create();
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(Guid.NewGuid(), "packed", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "El pedido no existe.");
    }

    [Fact]
    public async Task BackwardTransition_ReportsError()
    {
        var (context, orderId) = await ContextWithOrder("shipped");
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(orderId, "preparing", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "No es posible pasar del estado actual al estado seleccionado.");
    }

    [Fact]
    public async Task TransitionFromTerminalState_ReportsError()
    {
        var (context, orderId) = await ContextWithOrder("delivered");
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(orderId, "preparing", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "No es posible pasar del estado actual al estado seleccionado.");
    }

    [Fact]
    public async Task CancelFromNonTerminalState_HasNoValidationErrors()
    {
        var (context, orderId) = await ContextWithOrder("preparing");
        var validator = new UpdateOrderStatusCommandValidator(context);

        var result = await validator.TestValidateAsync(new UpdateOrderStatusCommand(orderId, "cancelled", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
