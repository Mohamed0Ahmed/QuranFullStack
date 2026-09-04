using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessProtectedStateRehearsalCollection))]
public sealed class AccessProtectedStateRehearsalTests(LegacyAccessTestFixture fixture)
{
    [Fact]
    public async Task ReplacePermissions_RejectsARetiredCode()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-retired-code-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        await RetirePermissionAsync(AbwabPermissions.Doors.Create);
        using var client = CreateOwnerClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = target.Version,
                permissionCodes = new[] { AbwabPermissions.Doors.Create },
                reason = "Reject a retired permission.",
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationInvalidPermissionCodes);
        (await GetGrantCodesAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task AuditInsertionFailure_RollsBackTheUserTransitionAndAllAuditRows()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-audit-failure-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Pending);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        using var client = CreateOwnerClient();
        await AddAuditFailureConstraintAsync();

        try
        {
            using var response = await client.PostAsJsonAsync(
                $"/api/access/users/{targetId}/accept",
                new
                {
                    expectedVersion = target.Version,
                    permissionCodes = new[] { AbwabPermissions.Doors.Create },
                    reason = "This audit write must fail atomically.",
                });

            await ApiEnvelope.AssertFailureEnvelopeAsync(
                response,
                HttpStatusCode.InternalServerError,
                ApiMessages.UnexpectedError);
        }
        finally
        {
            await DropAuditFailureConstraintAsync();
        }

        var persisted = (await fixture.GetUserBySubAsync(targetSub))!;
        persisted.Status.Should().Be(UserStatus.Pending);
        persisted.RoleId.Should().BeNull();
        persisted.LogtoSub.Should().Be(targetSub);
        persisted.Version.Should().Be(target.Version);
        (await GetGrantCodesAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task PermissionCatalogue_WithARetiredCanonicalCode_OmitsItAndStaysAssignable()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await SeedActiveOwnerAsync();
        await RetirePermissionAsync(AbwabPermissionCatalogue.All[0].Code);
        using var client = CreateOwnerClient();

        using var response = await client.GetAsync("/api/access/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalogue = await ApiEnvelope.ReadDataAsync(response);
        catalogue.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Equal(AbwabPermissionCatalogue.All.Skip(1).Select(permission => permission.Code));
        catalogue.GetProperty("assignmentReady").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PermissionCatalogue_OnAnEmptyPermissionsTable_StillAnswersWithTheCanonicalCatalogue()
    {
        await fixture.ResetAsync();
        await SeedActiveOwnerAsync();
        using var client = CreateOwnerClient();

        using var response = await client.GetAsync("/api/access/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        var catalogue = await ApiEnvelope.ReadDataAsync(response);
        catalogue.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("code").GetString())
            .Should().Equal(AbwabPermissionCatalogue.All.Select(permission => permission.Code));
        catalogue.GetProperty("assignmentReady").GetBoolean().Should().BeFalse();
    }

    private async Task SynchronizePermissionsAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }

    private Task<int> SeedActiveOwnerAsync()
    {
        return SeedActiveOwnerAsync(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail);
    }

    private async Task<int> SeedActiveOwnerAsync(string sub, string email)
    {
        var ownerRoleId = (await fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id;
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = email,
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task<int> SeedUserAsync(string sub, UserStatus status)
    {
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private HttpClient CreateOwnerClient()
    {
        var client = fixture.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(AccessTestFixture.OwnerSub));
        return client;
    }

    private async Task RetirePermissionAsync(string permissionCode)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE permissions SET retired_at = {DateTimeOffset.UtcNow} WHERE code = {permissionCode};");
    }

    private async Task<IReadOnlyList<string>> GetGrantCodesAsync(int targetUserId)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUserPermissions.AsNoTracking()
            .Where(grant => grant.UserId == targetUserId)
            .OrderBy(grant => grant.Permission.DisplayOrder)
            .Select(grant => grant.Permission.Code)
            .ToListAsync();
    }

    private async Task<IReadOnlyList<AccessAuditEvent>> GetAuditEventsAsync(int targetUserId)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessAuditEvents.AsNoTracking()
            .Where(eventItem => eventItem.TargetUserId == targetUserId)
            .OrderBy(eventItem => eventItem.Id)
            .ToListAsync();
    }

    private async Task<int> GetAuditEventCountAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessAuditEvents.CountAsync();
    }

    private async Task AddAuditFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE access_audit_events
            ADD CONSTRAINT ck_test_reject_user_accepted_audit
            CHECK (action_type <> 'UserAccepted');
            """);
    }

    private async Task DropAuditFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE access_audit_events DROP CONSTRAINT ck_test_reject_user_accepted_audit;");
    }
}

[Collection(nameof(AccessProtectedStateRehearsalCollection))]
public sealed class EmailIdentityPreflightRehearsalTests(LegacyAccessTestFixture fixture)
{
    [Fact]
    public async Task ScanAsync_HandlesNullNormalizedEmailDuringStagedMigration()
    {
        await fixture.ResetAsync();
        var userId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = "preflight-staged-null",
            Email = "Staged@Example.Test",
            Status = UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        await using (var setupScope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE users ALTER COLUMN normalized_email DROP NOT NULL;");
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET normalized_email = NULL WHERE id = {0};",
                userId);
        }

        try
        {
            using var apiScope = fixture.ApiServices.CreateScope();
            var preflight = apiScope.ServiceProvider.GetRequiredService<IEmailIdentityPreflight>();

            var result = await preflight.ScanAsync(CancellationToken.None);

            result.MissingNormalizedEmailUserIds.Should().ContainSingle().Which.Should().Be(userId);
        }
        finally
        {
            await using var restoreScope = fixture.QueryServices.CreateAsyncScope();
            var db = restoreScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET normalized_email = 'STAGED@EXAMPLE.TEST' WHERE id = {0};",
                userId);
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE users ALTER COLUMN normalized_email SET NOT NULL;");
        }
    }
}

[Collection(nameof(AccessProtectedStateRehearsalCollection))]
public sealed class AuthorizationStateResolverRehearsalTests(LegacyAccessTestFixture fixture)
{
    [Fact]
    public async Task ResolveAsync_ExcludesRetiredDirectPermissions_FromAnActiveNonOwner()
    {
        await fixture.ResetAsync();
        const string sub = "authorization-state-retired-permission";
        await using (var syncScope = fixture.ApiServices.CreateAsyncScope())
        {
            await syncScope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        var userId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = "authorization-state-retired-permission@example.test",
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await using (var setupScope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var permissionId = await db.AccessPermissions
                .Where(permission => permission.Code == AbwabPermissions.Doors.Create)
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
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE permissions SET retired_at = {DateTimeOffset.UtcNow} WHERE code = {AbwabPermissions.Doors.Create};");
        }

        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var state = await scope.ServiceProvider.GetRequiredService<IAuthorizationStateResolver>()
            .ResolveAsync(sub, CancellationToken.None);

        state.Should().NotBeNull();
        state!.PermissionCodes.Should().BeEmpty();
    }
}

[Collection(nameof(AccessProtectedStateRehearsalCollection))]
public sealed class DeviceSessionLifecycleRehearsalTests(LegacyAccessTestFixture fixture)
{
    private const string SessionsPath = "/api/auth/sessions";
    private const string MePath = "/api/access/me";
    private const string IdentityEvidenceHeader = "X-Interactive-Identity-Evidence";

    [Fact]
    public async Task Bootstrap_ReplacementInsertFailure_PreservesPreviousSession()
    {
        await fixture.ResetAsync();
        using var bootstrapClient = fixture.CreateApiClient();

        using var first = await BootstrapAsync(bootstrapClient, "device-session-failed-replacement");
        var firstToken = CookieValue(first, DeviceSessionAuthentication.SessionCookieName);

        await AddSuccessorInsertFailureConstraintAsync();
        try
        {
            using var second = await BootstrapAsync(bootstrapClient, "device-session-failed-replacement");
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                second,
                HttpStatusCode.InternalServerError,
                ApiMessages.UnexpectedError);
        }
        finally
        {
            await DropSuccessorInsertFailureConstraintAsync();
        }

        using var previousSessionClient = fixture.CreateApiClient();
        using var previousRequest = CookieBackedMeRequest(firstToken);
        using var previousResponse = await previousSessionClient.SendAsync(previousRequest);
        previousResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> BootstrapAsync(HttpClient client, string subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SessionsPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(subject));
        request.Headers.Add(
            IdentityEvidenceHeader,
            TestJwtTokens.MintIdentityToken(
                subject,
                FakeExternalUserProfileSource.EmailFor(subject),
                true));
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CookieBackedMeRequest(string sessionToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Add(
            "Cookie",
            $"{DeviceSessionAuthentication.SessionCookieName}={sessionToken}");
        return request;
    }

    private static string CookieValue(HttpResponseMessage response, string name)
    {
        var header = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{name}=", StringComparison.Ordinal));
        var pair = header.Split(';', 2)[0];
        return Uri.UnescapeDataString(pair[(name.Length + 1)..]);
    }

    private async Task AddSuccessorInsertFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE user_device_sessions
            ADD CONSTRAINT ck_test_reject_unrevoked_device_session
            CHECK (revoked_at IS NOT NULL) NOT VALID;
            """);
    }

    private async Task DropSuccessorInsertFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE user_device_sessions DROP CONSTRAINT ck_test_reject_unrevoked_device_session;");
    }
}

[Collection(nameof(AccessProtectedStateRehearsalCollection))]
public sealed class OwnerReconciliationProtectedStateRehearsalTests(LegacyAccessTestFixture fixture)
{
    [Fact]
    public async Task ReconcileInteractiveSignInAsync_AuditInsertFailure_RollsBackPromotionAndGrantRevocation()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var targetId = await SeedConfiguredOwnerAsync(
            AccessTestFixture.SecondOwnerSub,
            UserStatus.Pending,
            owner: false);
        var grantorId = await SeedUserAsync(
            "logto-audit-failure-grantor",
            "audit-failure-grantor@example.test",
            UserStatus.Active);
        await SeedDirectGrantAsync(targetId, grantorId);
        await AddAuditFailureConstraintAsync();

        try
        {
            using var scope = fixture.ApiServices.CreateScope();
            var reconcile = () => scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
                .ReconcileInteractiveSignInAsync(
                    Identity(AccessTestFixture.SecondOwnerSub),
                    CancellationToken.None);

            await reconcile.Should().ThrowAsync<DbUpdateException>();
        }
        finally
        {
            await DropAuditFailureConstraintAsync();
        }

        var user = await fixture.GetUserBySubAsync(AccessTestFixture.SecondOwnerSub);
        user!.Status.Should().Be(UserStatus.Pending);
        user.RoleId.Should().BeNull();
        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == targetId)).Should().Be(1);
        (await db.AccessAuditEvents.CountAsync(eventItem => eventItem.TargetUserId == targetId)).Should().Be(0);
    }

    private async Task SynchronizePermissionsAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }

    private async Task<int> SeedConfiguredOwnerAsync(string sub, UserStatus status, bool owner)
    {
        var email = sub == AccessTestFixture.OwnerSub
            ? AccessTestFixture.OwnerEmail
            : AccessTestFixture.SecondOwnerEmail;
        var roleId = owner
            ? (await fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id
            : (int?)null;
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = email,
            RoleId = roleId,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task<int> SeedUserAsync(string sub, string email, UserStatus status)
    {
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = email,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task SeedDirectGrantAsync(int userId, int grantorId)
    {
        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionId = await db.AccessPermissions
            .Where(permission => permission.Code == AbwabPermissions.Doors.Create)
            .Select(permission => permission.Id)
            .SingleAsync();
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            GrantedByUserId = grantorId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task AddAuditFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE access_audit_events
            ADD CONSTRAINT ck_test_reject_owner_grant_audit
            CHECK (action_type <> 'OwnerGrantedByReconciliation');
            """);
    }

    private async Task DropAuditFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE access_audit_events DROP CONSTRAINT ck_test_reject_owner_grant_audit;");
    }

    private static AuthenticatedInteractiveIdentity Identity(string sub)
    {
        var email = sub == AccessTestFixture.OwnerSub
            ? AccessTestFixture.OwnerEmail
            : AccessTestFixture.SecondOwnerEmail;
        return new AuthenticatedInteractiveIdentity(sub, email, true);
    }
}
