using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AuthorizationPipelineTests(AccessTestFixture fixture)
{
    private const string PermissionPath = "/api/test/authorization/permission";
    private const string OwnerPath = "/api/test/authorization/owner";

    [Fact]
    public async Task PermissionEndpoint_AnonymousRequest_Returns401EnvelopeWithoutInvokingAction()
    {
        await fixture.ResetAsync();
        await using var factory = fixture.CreateAuthorizationPipelineFactory();
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var client = fixture.CreateApiClient(factory);

        using var response = await client.PostAsync(PermissionPath, content: null);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
        probe.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task PermissionEndpoint_ActiveUserWithoutExactGrant_Returns403EnvelopeWithoutInvokingAction()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-pipeline-without-grant";
        await AddActiveUserAsync(sub);
        await using var factory = fixture.CreateAuthorizationPipelineFactory();
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var client = fixture.CreateApiClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));

        using var response = await client.PostAsync(PermissionPath, content: null);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Forbidden, ApiMessages.AccessPermissionDenied);
        probe.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task PermissionEndpoint_ActiveUserWithExactGrant_InvokesAction()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-pipeline-exact-grant";
        await SynchronizePermissionsAsync();
        var userId = await AddActiveUserAsync(sub);
        await GrantAsync(userId, AbwabPermissions.Doors.Create);
        await using var factory = fixture.CreateAuthorizationPipelineFactory();
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var client = fixture.CreateApiClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));

        using var response = await client.PostAsync(PermissionPath, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("authorization").GetString().Should().Be("permission");
        probe.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task PermissionEndpoint_OwnerGrantRevokeAndDisable_TracksNonOwnerAuthorityImmediately()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await AddActiveUserAsync(AccessTestFixture.OwnerSub, RoleNames.Owner);
        const string targetSub = "authorization-pipeline-permission-lifecycle";
        var targetId = await AddActiveUserAsync(targetSub);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        await using var factory = fixture.CreateAuthorizationPipelineFactory();
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var ownerClient = fixture.CreateApiClient(factory);
        using var targetClient = fixture.CreateApiClient(factory);
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(AccessTestFixture.OwnerSub));
        targetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(targetSub));

        using var beforeGrant = await targetClient.PostAsync(PermissionPath, content: null);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            beforeGrant, HttpStatusCode.Forbidden, ApiMessages.AccessPermissionDenied);

        using var grantResponse = await ownerClient.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = target.Version,
                permissionCodes = new[] { AbwabPermissions.Doors.Create },
                reason = "Grant the exact direct Permission for lifecycle verification.",
            });
        grantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var granted = await ApiEnvelope.ReadDataAsync(grantResponse);
        granted.GetProperty("permissionCodes").EnumerateArray()
            .Select(code => code.GetString()).Should().Equal(AbwabPermissions.Doors.Create);

        using var afterGrant = await targetClient.PostAsync(PermissionPath, content: null);
        afterGrant.StatusCode.Should().Be(HttpStatusCode.OK);
        probe.InvocationCount.Should().Be(1);

        using var revokeResponse = await ownerClient.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = granted.GetProperty("version").GetUInt32(),
                permissionCodes = Array.Empty<string>(),
                reason = "Revoke the direct Permission for lifecycle verification.",
            });
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revoked = await ApiEnvelope.ReadDataAsync(revokeResponse);
        revoked.GetProperty("permissionCodes").GetArrayLength().Should().Be(0);

        using var afterRevoke = await targetClient.PostAsync(PermissionPath, content: null);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            afterRevoke, HttpStatusCode.Forbidden, ApiMessages.AccessPermissionDenied);
        probe.InvocationCount.Should().Be(1);

        using var regrantResponse = await ownerClient.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = revoked.GetProperty("version").GetUInt32(),
                permissionCodes = new[] { AbwabPermissions.Doors.Create },
                reason = "Restore the direct Permission before the disable check.",
            });
        regrantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regranted = await ApiEnvelope.ReadDataAsync(regrantResponse);

        using var disableResponse = await ownerClient.PostAsJsonAsync(
            $"/api/access/users/{targetId}/disable",
            new
            {
                expectedVersion = regranted.GetProperty("version").GetUInt32(),
                reason = "Disable the account for lifecycle verification.",
            });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var afterDisable = await targetClient.PostAsync(PermissionPath, content: null);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            afterDisable, HttpStatusCode.Forbidden, ApiMessages.AccessInactive);
        probe.InvocationCount.Should().Be(1);

        using var permissionRead = await ownerClient.GetAsync($"/api/access/users/{targetId}/permissions");
        permissionRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalPermissions = await ApiEnvelope.ReadDataAsync(permissionRead);
        finalPermissions.GetProperty("status").GetString().Should().Be("disabled");
        finalPermissions.GetProperty("permissionCodes").GetArrayLength().Should().Be(0);

        using var auditRead = await ownerClient.GetAsync(
            $"/api/access/audit-events?targetUserId={targetId}&pageSize=25");
        auditRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var audit = await ApiEnvelope.ReadDataAsync(auditRead);
        audit.GetProperty("items").EnumerateArray()
            .Select(eventItem => eventItem.GetProperty("actionType").GetString())
            .Reverse()
            .Should().Equal(
                nameof(AccessAuditActionType.PermissionGranted),
                nameof(AccessAuditActionType.PermissionRevoked),
                nameof(AccessAuditActionType.PermissionGranted),
                nameof(AccessAuditActionType.PermissionRevoked),
                nameof(AccessAuditActionType.UserDisabled));
    }

    [Fact]
    public async Task OwnerEndpoint_ActiveNonOwnerWithDirectGrant_Returns403EnvelopeWithoutInvokingAction()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-pipeline-owner-grant";
        await SynchronizePermissionsAsync();
        var userId = await AddActiveUserAsync(sub);
        await GrantAsync(userId, AbwabPermissions.Doors.Create);
        await using var factory = fixture.CreateAuthorizationPipelineFactory();
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var client = fixture.CreateApiClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));

        using var response = await client.PostAsync(OwnerPath, content: null);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Forbidden, ApiMessages.AccessOwnerRequired);
        probe.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task OwnerEndpoint_ActiveOwner_InvokesAction()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-pipeline-owner";
        await AddActiveUserAsync(sub, RoleNames.Owner);
        await using var factory = fixture.CreateAuthorizationPipelineFactory();
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var client = fixture.CreateApiClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));

        using var response = await client.PostAsync(OwnerPath, content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("authorization").GetString().Should().Be("owner");
        probe.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task PermissionEndpoint_AuthorizationStateFailure_Returns503EnvelopeWithoutInvokingAction()
    {
        await fixture.ResetAsync();
        await using var factory = fixture.CreateAuthorizationPipelineFactory(services =>
        {
            services.RemoveAll<IAuthorizationStateResolver>();
            services.AddScoped<IAuthorizationStateResolver>(_ => new ThrowingAuthorizationStateResolver());
        });
        var probe = factory.Services.GetRequiredService<AuthorizationPipelineProbe>();
        using var client = fixture.CreateApiClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint("authorization-pipeline-unavailable"));

        using var response = await client.PostAsync(PermissionPath, content: null);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.ServiceUnavailable, ApiMessages.AuthorizationUnavailable);
        probe.InvocationCount.Should().Be(0);
    }

    private async Task<int> AddActiveUserAsync(string sub, string? roleName = null)
    {
        var roleId = roleName is null
            ? (int?)null
            : (await fixture.GetRolesAsync()).Single(role => role.Name == roleName).Id;
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            RoleId = roleId,
            Status = UserStatus.Active,
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

    private sealed class ThrowingAuthorizationStateResolver : IAuthorizationStateResolver
    {
        public Task<AuthorizationState?> ResolveAsync(string logtoSub, CancellationToken cancellationToken) =>
            Task.FromException<AuthorizationState?>(new InvalidOperationException("database unavailable"));
    }
}
