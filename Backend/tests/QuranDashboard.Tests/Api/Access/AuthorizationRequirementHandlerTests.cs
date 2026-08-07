using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Authorization.Owner;
using QuranDashboard.Api.Authorization.Permissions;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using ApiAuthorizationFailureReason = QuranDashboard.Api.Authorization.AuthorizationFailureReason;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AuthorizationRequirementHandlerTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task PermissionRequirement_ActiveOwnerWithoutDirectGrants_Succeeds()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-owner";
        await AddUserAsync(sub, UserStatus.Active, RoleNames.Owner);

        var attempt = await AuthorizeAsync(
            AuthenticatedPrincipal(sub),
            new PermissionRequirement(AbwabPermissions.Doors.Create));

        attempt.Result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionRequirement_ActiveNonOwnerWithExactGrant_Succeeds()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-exact-grant";
        await SynchronizePermissionsAsync();
        var userId = await AddUserAsync(sub, UserStatus.Active, roleName: null);
        await GrantAsync(userId, AbwabPermissions.Doors.Create);

        var attempt = await AuthorizeAsync(
            AuthenticatedPrincipal(sub),
            new PermissionRequirement(AbwabPermissions.Doors.Create));

        attempt.Result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task OwnerOnlyRequirement_ActiveOwner_Succeeds()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-owner-only";
        await AddUserAsync(sub, UserStatus.Active, RoleNames.Owner);

        var attempt = await AuthorizeAsync(AuthenticatedPrincipal(sub), OwnerOnlyRequirement.Instance);

        attempt.Result.Succeeded.Should().BeTrue();
    }

    public static TheoryData<UserStatus?, string?, ApiAuthorizationFailureReason> PermissionDenialCases => new()
    {
        { null, null, ApiAuthorizationFailureReason.Unprovisioned },
        { UserStatus.Pending, null, ApiAuthorizationFailureReason.Inactive },
        { UserStatus.Disabled, AbwabPermissions.Doors.Create, ApiAuthorizationFailureReason.Inactive },
        { UserStatus.Active, null, ApiAuthorizationFailureReason.MissingPermission },
        { UserStatus.Active, AbwabPermissions.Doors.Edit, ApiAuthorizationFailureReason.MissingPermission },
    };

    [Theory]
    [MemberData(nameof(PermissionDenialCases))]
    public async Task PermissionRequirement_UnknownInactiveOrInsufficientUser_FailsClosed(
        UserStatus? status,
        string? grantedPermission,
        ApiAuthorizationFailureReason expectedFailure)
    {
        await fixture.ResetAsync();
        const string sub = "authorization-permission-denial";
        if (status is { } resolvedStatus)
        {
            if (grantedPermission is not null)
            {
                await SynchronizePermissionsAsync();
            }

            var userId = await AddUserAsync(sub, resolvedStatus, roleName: null);
            if (grantedPermission is not null)
            {
                await GrantAsync(userId, grantedPermission);
            }
        }

        var attempt = await AuthorizeAsync(
            AuthenticatedPrincipal(sub),
            new PermissionRequirement(AbwabPermissions.Doors.Create));

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(expectedFailure);
    }

    [Fact]
    public async Task PermissionRequirement_TokenRoleAndPermissionClaims_DoNotGrantAccess()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-claim-smuggling";
        await AddUserAsync(sub, UserStatus.Active, roleName: null);

        var attempt = await AuthorizeAsync(
            AuthenticatedPrincipal(
                sub,
                new Claim(ClaimTypes.Role, RoleNames.Owner),
                new Claim("permission", AbwabPermissions.Doors.Create)),
            new PermissionRequirement(AbwabPermissions.Doors.Create));

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(ApiAuthorizationFailureReason.MissingPermission);
    }

    [Fact]
    public async Task PermissionRequirement_AuthenticatedPrincipalWithoutSub_FailsClosed()
    {
        await fixture.ResetAsync();

        var attempt = await AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test")),
            new PermissionRequirement(AbwabPermissions.Doors.Create));

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(ApiAuthorizationFailureReason.Unprovisioned);
    }

    [Fact]
    public async Task PermissionRequirement_AuthenticatedPrincipalWithWhitespaceSub_FailsClosed()
    {
        await fixture.ResetAsync();

        var attempt = await AuthorizeAsync(
            AuthenticatedPrincipal(" "),
            new PermissionRequirement(AbwabPermissions.Doors.Create));

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(ApiAuthorizationFailureReason.Unprovisioned);
    }

    [Fact]
    public async Task OwnerOnlyRequirement_ActiveNonOwnerWithDirectGrant_Fails()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-owner-only-grant";
        await SynchronizePermissionsAsync();
        var userId = await AddUserAsync(sub, UserStatus.Active, roleName: null);
        await GrantAsync(userId, AbwabPermissions.Doors.Create);

        var attempt = await AuthorizeAsync(AuthenticatedPrincipal(sub), OwnerOnlyRequirement.Instance);

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(ApiAuthorizationFailureReason.OwnerRequired);
    }

    [Fact]
    public async Task OwnerOnlyRequirement_DisabledOwner_FailsAsInactive()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-disabled-owner";
        await AddUserAsync(sub, UserStatus.Disabled, RoleNames.Owner);

        var attempt = await AuthorizeAsync(AuthenticatedPrincipal(sub), OwnerOnlyRequirement.Instance);

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(ApiAuthorizationFailureReason.Inactive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Requirement_ConflictingEndpointMetadata_FailsClosedEvenForAnActiveOwner(bool permissionRequirement)
    {
        await fixture.ResetAsync();
        const string sub = "authorization-invalid-endpoint-metadata";
        await AddUserAsync(sub, UserStatus.Active, RoleNames.Owner);
        var requirement = permissionRequirement
            ? (IAuthorizationRequirement)new PermissionRequirement(AbwabPermissions.Doors.Create)
            : OwnerOnlyRequirement.Instance;

        var attempt = await AuthorizeAsync(
            AuthenticatedPrincipal(sub),
            requirement,
            CreateConflictingUnsafeEndpoint());

        attempt.Result.Succeeded.Should().BeFalse();
        attempt.FailureReason.Should().Be(
            permissionRequirement
                ? ApiAuthorizationFailureReason.MissingPermission
                : ApiAuthorizationFailureReason.OwnerRequired);
    }

    [Fact]
    public async Task PermissionRequirement_ResolverFailure_FailsClosed()
    {
        var resolver = new ThrowingAuthorizationStateResolver();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthorization();
        services.AddScoped<ICurrentUser>(_ => new FixedCurrentUser("authorization-resolver-failure"));
        services.AddScoped<AuthorizationFailureState>();
        services.AddScoped<AuthorizationStateAccessEvaluator>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationStateResolver>(_ => resolver);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(
                AuthenticatedPrincipal("authorization-resolver-failure"),
                resource: null,
                [new PermissionRequirement(AbwabPermissions.Doors.Create)]);

        result.Succeeded.Should().BeFalse();
        scope.ServiceProvider.GetRequiredService<AuthorizationFailureState>().Reason
            .Should().Be(ApiAuthorizationFailureReason.InfrastructureUnavailable);
    }

    [Fact]
    public async Task PermissionRequirement_UsesTheCurrentUserSubBoundary()
    {
        var resolver = new MemoizedAuthorizationStateResolver(new AuthorizationState(
            1,
            UserStatus.Active,
            IsOwner: true,
            PermissionCodes: new HashSet<string>()));
        await using var provider = BuildAuthorizationProvider(
            resolver,
            new FixedCurrentUser("authorization-current-user-sub"));
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(
                AuthenticatedPrincipal("authorization-token-sub"),
                resource: null,
                [new PermissionRequirement(AbwabPermissions.Doors.Create)]);

        result.Succeeded.Should().BeTrue();
        resolver.ResolvedSubs.Should().Equal("authorization-current-user-sub");
    }

    [Fact]
    public async Task PermissionAndOwnerRequirements_ShareOneScopedResolverQuery()
    {
        var resolver = new MemoizedAuthorizationStateResolver(new AuthorizationState(
            1,
            UserStatus.Active,
            IsOwner: true,
            PermissionCodes: new HashSet<string>()));
        await using var provider = BuildAuthorizationProvider(
            resolver,
            new FixedCurrentUser("authorization-shared-resolver"));
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(
                AuthenticatedPrincipal("authorization-token-sub"),
                resource: null,
                [
                    new PermissionRequirement(AbwabPermissions.Doors.Create),
                    OwnerOnlyRequirement.Instance,
                ]);

        result.Succeeded.Should().BeTrue();
        resolver.QueryCount.Should().Be(1);
        resolver.ResolvedSubs.Should().Equal(
            "authorization-shared-resolver",
            "authorization-shared-resolver");
    }

    private async Task<AuthorizationAttempt> AuthorizeAsync(
        ClaimsPrincipal principal,
        IAuthorizationRequirement requirement,
        Endpoint? endpoint = null)
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal };
        if (endpoint is not null)
        {
            httpContext.SetEndpoint(endpoint);
        }
        httpContextAccessor.HttpContext = httpContext;

        try
        {
            var result = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(principal, resource: null, [requirement]);
            var failureReason = scope.ServiceProvider.GetRequiredService<AuthorizationFailureState>().Reason;
            return new AuthorizationAttempt(result, failureReason);
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
        }
    }

    private async Task<int> AddUserAsync(string sub, UserStatus status, string? roleName)
    {
        var roleId = roleName is null
            ? (int?)null
            : (await fixture.GetRolesAsync()).Single(role => role.Name == roleName).Id;
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            RoleId = roleId,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task SynchronizePermissionsAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }

    private async Task GrantAsync(int userId, string permissionCode)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionId = await db.AccessPermissions
            .Where(permission => permission.Code == permissionCode)
            .Select(permission => permission.Id)
            .SingleAsync();
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            GrantedByUserId = userId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(string sub, params Claim[] additionalClaims) =>
        new(new ClaimsIdentity([new Claim("sub", sub), .. additionalClaims], "test"));

    private static ServiceProvider BuildAuthorizationProvider(
        IAuthorizationStateResolver resolver,
        ICurrentUser currentUser)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthorization();
        services.AddScoped<ICurrentUser>(_ => currentUser);
        services.AddScoped<AuthorizationFailureState>();
        services.AddScoped<AuthorizationStateAccessEvaluator>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, OwnerAuthorizationHandler>();
        services.AddScoped<IAuthorizationStateResolver>(_ => resolver);
        return services.BuildServiceProvider();
    }

    private static RouteEndpoint CreateConflictingUnsafeEndpoint()
    {
        var builder = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/test"),
            order: 0);
        builder.Metadata.Add(new HttpMethodMetadata([HttpMethods.Post]));
        builder.Metadata.Add(new RequirePermissionAttribute(AbwabPermissions.Doors.Create));
        builder.Metadata.Add(new RequireOwnerAttribute());
        return (RouteEndpoint)builder.Build();
    }

    private sealed record AuthorizationAttempt(
        AuthorizationResult Result,
        ApiAuthorizationFailureReason? FailureReason);

    private sealed class ThrowingAuthorizationStateResolver : IAuthorizationStateResolver
    {
        public Task<AuthorizationState?> ResolveAsync(string logtoSub, CancellationToken cancellationToken) =>
            Task.FromException<AuthorizationState?>(new InvalidOperationException("database unavailable"));
    }

    private sealed class FixedCurrentUser(string sub) : ICurrentUser
    {
        public AuthenticatedInteractiveIdentity Identity { get; } = new(sub, null, false);
    }

    private sealed class MemoizedAuthorizationStateResolver(AuthorizationState state) : IAuthorizationStateResolver
    {
        private Task<AuthorizationState?>? _resolution;

        public int QueryCount { get; private set; }

        public List<string> ResolvedSubs { get; } = [];

        public Task<AuthorizationState?> ResolveAsync(string logtoSub, CancellationToken cancellationToken)
        {
            ResolvedSubs.Add(logtoSub);
            if (_resolution is not null)
            {
                return _resolution;
            }

            QueryCount++;
            return _resolution = Task.FromResult<AuthorizationState?>(state);
        }
    }
}
