using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Orders;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Orders;

public class GetOrdersQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static User SampleUser(string firstName, string lastName) => new()
    {
        UserType = UserType.customer,
        FirstName = firstName,
        LastName = lastName,
        Email = $"{firstName}.{lastName}@example.com".ToLowerInvariant(),
        PasswordHash = "hash",
    };

    [Fact]
    public async Task Orders_AreReturnedNewestFirst_WithChannelAndCustomerName()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = SampleUser("Ana", "Gómez");
        context.Users.Add(customer);

        var older = new Order
        {
            OrderNumber = "ORD-001",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = "delivered",
            Subtotal = 100_000m,
            Total = 119_000m,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        var newer = new Order
        {
            OrderNumber = "ORD-002",
            UserId = customer.Id,
            User = customer,
            Channel = "store",
            Status = "pending_payment",
            Subtotal = 50_000m,
            Total = 59_500m,
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        context.Orders.AddRange(older, newer);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrdersQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("ORD-002", result[0].OrderNumber);
        Assert.Equal("store", result[0].Channel);
        Assert.Equal("ORD-001", result[1].OrderNumber);
        Assert.All(result, dto => Assert.Equal("Ana Gómez", dto.CustomerName));
    }

    [Fact]
    public async Task Order_ItemCount_ReflectsLineItemsRegistered()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = SampleUser("Luis", "Pérez");
        var product = new Product { Sku = "SKU-100", Name = "Licuadora", Price = 120_000m };
        context.Users.Add(customer);
        context.Products.Add(product);

        var order = new Order
        {
            OrderNumber = "ORD-010",
            UserId = customer.Id,
            User = customer,
            Channel = "chat",
            Status = "preparing",
            Subtotal = 240_000m,
            Total = 285_600m,
        };
        order.Items.Add(new OrderItem { Order = order, ProductId = product.Id, Product = product, Quantity = 2, UnitPrice = 120_000m, Subtotal = 240_000m });
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrdersQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("chat", dto.Channel);
        Assert.Equal(1, dto.ItemCount);
    }

    [Fact]
    public async Task StatusFilter_OnlyReturnsOrdersInThatStatus()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = SampleUser("Carla", "Ríos");
        context.Users.Add(customer);

        var preparing = new Order
        {
            OrderNumber = "ORD-020",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = "preparing",
            Subtotal = 10_000m,
            Total = 11_900m,
        };
        var packed = new Order
        {
            OrderNumber = "ORD-021",
            UserId = customer.Id,
            User = customer,
            Channel = "web",
            Status = "packed",
            Subtotal = 20_000m,
            Total = 23_800m,
        };
        context.Orders.AddRange(preparing, packed);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrdersQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrdersQuery("preparing"), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("ORD-020", dto.OrderNumber);
    }

    [Fact]
    public async Task NoStatusFilter_ReturnsEveryOrderRegardlessOfStatus()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = SampleUser("Diego", "Salas");
        context.Users.Add(customer);

        context.Orders.AddRange(
            new Order { OrderNumber = "ORD-030", UserId = customer.Id, User = customer, Channel = "web", Status = "preparing", Subtotal = 1m, Total = 1m },
            new Order { OrderNumber = "ORD-031", UserId = customer.Id, User = customer, Channel = "web", Status = "packed", Subtotal = 1m, Total = 1m });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrdersQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task NoOrders_ReturnsEmptyList()
    {
        await using var context = InMemoryApplicationDbContext.Create();

        var handler = new GetOrdersQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
