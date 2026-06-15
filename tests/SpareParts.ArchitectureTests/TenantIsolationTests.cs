using System.Reflection;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class TenantIsolationTests
{
    [Fact]
    public void TenantsController_ShouldRequireSuperAdminPolicy()
    {
        var authorize = typeof(TenantsController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single(a => !string.IsNullOrWhiteSpace(a.Policy));

        Assert.Equal(AuthorizationPolicies.SuperAdmin, authorize.Policy);
    }

    [Fact]
    public void AuthorizationPolicies_ShouldHaveSuperAdminRoleId_MapToUserRole()
    {
        Assert.Equal((int)UserRole.SuperAdmin, AuthorizationPolicies.SuperAdminRoleId);
        Assert.Equal(5, AuthorizationPolicies.SuperAdminRoleId);
    }

    [Fact]
    public void TenantContext_Legacy_ShouldHaveTenantIdZero()
    {
        var context = TenantContext.Legacy;
        Assert.Equal(0, context.TenantId);
        Assert.True(context.IsResolved);
        Assert.False(context.IsSuperAdmin);
    }

    [Fact]
    public void UsersService_GetAll_ShouldFilterByTenantId()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        using (var conn = factory.CreateConnection())
        {
            conn.Execute(
                @"INSERT INTO Users (Id, Username, FullName, Email, PasswordHash, RoleId, TenantId, IsActive)
                  VALUES
                    (1, 'user_t1', 'Tenant1 User', 't1@test.com', 'hash', 1, 1, 1),
                    (2, 'user_t2', 'Tenant2 User', 't2@test.com', 'hash', 1, 2, 1);");
        }

        var tenant1Context = InMemorySqliteConnectionFactory.ForTenant(1, "t1");
        var tenant2Context = InMemorySqliteConnectionFactory.ForTenant(2, "t2");

        var service1 = new UsersService(factory, tenant1Context, TestDoubles.UnlimitedSubscriptionLimitService.Instance);
        var service2 = new UsersService(factory, tenant2Context, TestDoubles.UnlimitedSubscriptionLimitService.Instance);

        var users1 = service1.GetAll().ToList();
        var users2 = service2.GetAll().ToList();

        Assert.Single(users1);
        Assert.Equal("user_t1", users1[0].Username);

        Assert.Single(users2);
        Assert.Equal("user_t2", users2[0].Username);
    }

    [Fact]
    public void UsersService_Deactivate_ShouldNotAffectOtherTenants()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        using (var conn = factory.CreateConnection())
        {
            conn.Execute(
                @"INSERT INTO Users (Id, Username, FullName, Email, PasswordHash, RoleId, TenantId, IsActive)
                  VALUES
                    (1, 'user_t1', 'Tenant1 User', 't1@test.com', 'hash', 1, 1, 1),
                    (2, 'user_t2', 'Tenant2 User', 't2@test.com', 'hash', 1, 2, 1);");
        }

        var tenant1Context = InMemorySqliteConnectionFactory.ForTenant(1, "t1");
        var service = new UsersService(factory, tenant1Context, TestDoubles.UnlimitedSubscriptionLimitService.Instance);

        // Tenant 1 tries to deactivate User 2 (belongs to Tenant 2) — should throw NotFoundException.
        Assert.Throws<NotFoundException>(() => service.Deactivate(2));

        // Verify user 2 is still active.
        using var verificationConnection = factory.CreateConnection();
        var isActive = verificationConnection.ExecuteScalar<int>("SELECT IsActive FROM Users WHERE Id = 2");
        Assert.Equal(1, isActive);
    }

    [Fact]
    public void TenantsService_ExistsAndActive_ShouldReturnFalseForMissingTenant()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        var service = new TenantsService(factory);

        Assert.False(service.ExistsAndActive(999));
    }

    [Fact]
    public void TenantsService_ExistsAndActive_ShouldReturnTrueForDefaultTenant()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        var service = new TenantsService(factory);

        Assert.True(service.ExistsAndActive(1));
    }
}
