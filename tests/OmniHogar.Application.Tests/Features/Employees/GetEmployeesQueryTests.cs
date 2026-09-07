using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniHogar.Application.Common.Mappings;
using OmniHogar.Application.Features.Employees;
using OmniHogar.Application.Tests.TestSupport;
using OmniHogar.Domain.Entities;
using OmniHogar.Domain.Enums;

namespace OmniHogar.Application.Tests.Features.Employees;

public class GetEmployeesQueryTests
{
    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static User SampleEmployee(string firstName, string lastName) => new()
    {
        UserType = UserType.employee,
        FirstName = firstName,
        LastName = lastName,
        Email = $"{firstName}.{lastName}@omnihogar.com".ToLowerInvariant(),
        PasswordHash = "hash",
    };

    [Fact]
    public async Task Employees_AreReturnedAlphabetically_WithAssignedRole()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var role = new Role { Name = "Admin", Description = "Full system access." };
        context.Roles.Add(role);

        var carlos = SampleEmployee("Carlos", "Gómez");
        var ana = SampleEmployee("Ana", "Rodríguez");
        context.Users.AddRange(carlos, ana);
        context.UserRoles.Add(new UserRole { User = ana, RoleId = role.Id });
        context.UserRoles.Add(new UserRole { User = carlos, RoleId = role.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetEmployeesQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetEmployeesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Ana", result[0].FirstName);
        Assert.Equal("Carlos", result[1].FirstName);
        Assert.All(result, dto => Assert.Equal("Admin", dto.RoleName));
    }

    [Fact]
    public async Task Employees_ExcludeCustomerAccounts()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var customer = new User
        {
            UserType = UserType.customer,
            FirstName = "Luis",
            LastName = "Pérez",
            Email = "luis@example.com",
            PasswordHash = "hash",
        };
        var employee = SampleEmployee("Laura", "Martínez");
        context.Users.AddRange(customer, employee);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetEmployeesQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetEmployeesQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("Laura", dto.FirstName);
        Assert.Null(dto.RoleName);
    }

    [Fact]
    public async Task Employee_WithMultipleRoles_ReturnsRoleWithEarliestAssignedAt()
    {
        await using var context = InMemoryApplicationDbContext.Create();
        var admin = new Role { Name = "Admin", Description = "Full system access." };
        var support = new Role { Name = "Soporte", Description = "Customer support access." };
        context.Roles.AddRange(admin, support);

        var employee = SampleEmployee("Mateo", "Silva");
        context.Users.Add(employee);

        // Inserted out of chronological order on purpose — the earliest AssignedAt must win
        // regardless of insertion order, not just "whichever role happens to be added first".
        context.UserRoles.Add(new UserRole
        {
            User = employee,
            RoleId = admin.Id,
            AssignedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        context.UserRoles.Add(new UserRole
        {
            User = employee,
            RoleId = support.Id,
            AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetEmployeesQueryHandler(context, CreateMapper());
        var result = await handler.Handle(new GetEmployeesQuery(), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("Soporte", dto.RoleName);
    }
}
