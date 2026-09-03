using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class LogtoSubjectRelinkEndpointTests(AccessTestFixture fixture) : AccessMutableWriterTest(fixture)
{
    [Fact]
    public async Task PreviewThenConfirm_WithValidEvidence_PreservesTheTargetAuthority()
    {
        await Fixture.VerifyPermissionCatalogueAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-relink-target";
        const string newSub = "access-admin-relink-replacement";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        await AddGrantAsync(targetId, ownerId, AbwabPermissions.Doors.Create);
        var target = (await Fixture.GetUserBySubAsync(targetSub))!;
        Fixture.ProfileSource.ReturnEmailFor(newSub, target.Email);
        var evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: true);
        using var client = CreateOwnerClient();

        using var previewResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/preview",
            new { newSub, evidenceToken });

        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await ApiEnvelope.ReadDataAsync(previewResponse);
        preview.GetProperty("oldSub").GetString().Should().Be(targetSub);
        preview.GetProperty("newSub").GetString().Should().Be(newSub);
        preview.GetProperty("version").GetUInt32().Should().Be(target.Version);
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "Replace a recreated identity-provider subject.",
                confirmed = true,
                evidenceToken,
            });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var persisted = (await Fixture.GetUserBySubAsync(newSub))!;
        persisted.Id.Should().Be(targetId);
        persisted.Status.Should().Be(UserStatus.Active);
        persisted.RoleId.Should().BeNull();
        (await GetGrantCodesAsync(targetId)).Should().Equal(AbwabPermissions.Doors.Create);

        var audit = (await GetAuditEventsAsync(targetId)).Should().ContainSingle().Subject;
        audit.ActionType.Should().Be(AccessAuditActionType.LogtoSubjectRelinked);
        using var beforeState = JsonDocument.Parse(audit.BeforeStateJson!);
        using var afterState = JsonDocument.Parse(audit.AfterStateJson!);
        using var targetSnapshot = JsonDocument.Parse(audit.TargetSnapshotJson);
        beforeState.RootElement.GetProperty("logtoSub").GetString().Should().Be(targetSub);
        afterState.RootElement.GetProperty("logtoSub").GetString().Should().Be(newSub);
        targetSnapshot.RootElement.GetProperty("logtoSub").GetString().Should().Be(newSub);
        audit.Metadata.Provenance.Should().Contain("operation", "relink-logto-sub");
        string.Concat(
                audit.ActorSnapshotJson,
                audit.TargetSnapshotJson,
                audit.BeforeStateJson,
                audit.AfterStateJson,
                JsonSerializer.Serialize(audit.Metadata))
            .Should().NotContain(evidenceToken);
    }

    [Fact]
    public async Task PreviewThenConfirm_WithInvalidConfirmationEvidence_LeavesSubjectAuthorityVersionAndAuditUnchanged()
    {
        await Fixture.VerifyPermissionCatalogueAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-relink-confirmation-target";
        const string newSub = "access-admin-relink-confirmation-replacement";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        await AddGrantAsync(targetId, ownerId, AbwabPermissions.Doors.Create);
        var target = (await Fixture.GetUserBySubAsync(targetSub))!;
        Fixture.ProfileSource.ReturnEmailFor(newSub, target.Email);
        using var client = CreateOwnerClient();

        using var previewResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/preview",
            new
            {
                newSub,
                evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: true),
            });

        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "Confirmation evidence must still prove the replacement identity.",
                confirmed = true,
                evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: false),
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            confirmResponse,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationInvalidRelinkEvidence);
        var persisted = (await Fixture.GetUserBySubAsync(targetSub))!;
        persisted.Id.Should().Be(targetId);
        persisted.LogtoSub.Should().Be(targetSub);
        persisted.Status.Should().Be(UserStatus.Active);
        persisted.RoleId.Should().BeNull();
        persisted.Version.Should().Be(target.Version);
        (await Fixture.GetUserBySubAsync(newSub)).Should().BeNull();
        (await GetGrantCodesAsync(targetId)).Should().Equal(AbwabPermissions.Doors.Create);
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Confirm_WithABlankReason_IsStillRejectedWithoutTouchingTheSubjectOrAudit()
    {
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-relink-blank-reason-target";
        const string newSub = "access-admin-relink-blank-reason-replacement";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var target = (await Fixture.GetUserBySubAsync(targetSub))!;
        Fixture.ProfileSource.ReturnEmailFor(newSub, target.Email);
        using var client = CreateOwnerClient();

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "   ",
                confirmed = true,
                evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: true),
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            confirmResponse,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationInvalidRequest);
        var persisted = (await Fixture.GetUserBySubAsync(targetSub))!;
        persisted.Id.Should().Be(targetId);
        persisted.LogtoSub.Should().Be(targetSub);
        persisted.Version.Should().Be(target.Version);
        (await Fixture.GetUserBySubAsync(newSub)).Should().BeNull();
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task OwnerRelink_RecordsTheAuthenticatedOldSubjectAsTheAuditActor()
    {
        var ownerId = await SeedActiveOwnerAsync();
        const string newSub = "access-admin-owner-self-relink-replacement";
        var owner = (await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!;
        Fixture.ProfileSource.ReturnEmailFor(AccessTestFixture.OwnerSub, owner.Email);
        Fixture.ProfileSource.ReturnEmailFor(newSub, owner.Email);
        var evidenceToken = EvidenceToken(newSub, owner.Email, emailVerified: true);
        using var client = CreateOwnerClient();

        using var previewResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{ownerId}/logto-sub/relink/preview",
            new { newSub, evidenceToken });

        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{ownerId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = owner.Version,
                oldSub = AccessTestFixture.OwnerSub,
                newSub,
                reason = "Replace the Owner identity-provider subject.",
                confirmed = true,
                evidenceToken,
            });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Fixture.GetUserBySubAsync(newSub))!.Id.Should().Be(ownerId);
        var audit = (await GetAuditEventsAsync(ownerId)).Should().ContainSingle().Subject;
        audit.ActionType.Should().Be(AccessAuditActionType.LogtoSubjectRelinked);
        using var actorSnapshot = JsonDocument.Parse(audit.ActorSnapshotJson);
        using var targetSnapshot = JsonDocument.Parse(audit.TargetSnapshotJson);
        actorSnapshot.RootElement.GetProperty("id").GetInt32().Should().Be(ownerId);
        actorSnapshot.RootElement.GetProperty("logtoSub").GetString().Should().Be(AccessTestFixture.OwnerSub);
        targetSnapshot.RootElement.GetProperty("logtoSub").GetString().Should().Be(newSub);
    }

    [Fact]
    public async Task Confirm_RejectsUnverifiedEvidenceAndAnAlreadyLinkedSubjectWithoutAudit()
    {
        await Fixture.VerifyPermissionCatalogueAsync();
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-relink-invalid-target";
        const string newSub = "access-admin-relink-existing-subject";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        await SeedUserAsync(newSub, UserStatus.Pending);
        var target = (await Fixture.GetUserBySubAsync(targetSub))!;
        Fixture.ProfileSource.ReturnEmailFor(newSub, target.Email);
        using var client = CreateOwnerClient();

        using var unverifiedResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/preview",
            new { newSub, evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: false) });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            unverifiedResponse,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationInvalidRelinkEvidence);

        using var linkedResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "This linked subject must be refused.",
                confirmed = true,
                evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: true),
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            linkedResponse,
            HttpStatusCode.Conflict,
            ApiMessages.AccessAdministrationSubjectAlreadyLinked);
        (await Fixture.GetUserBySubAsync(targetSub))!.Status.Should().Be(UserStatus.Active);
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Confirm_RejectsAStalePreviewAndLeavesTheOldSubjectUntouched()
    {
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-relink-stale-target";
        const string newSub = "access-admin-relink-stale-replacement";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var target = (await Fixture.GetUserBySubAsync(targetSub))!;
        Fixture.ProfileSource.ReturnEmailFor(newSub, target.Email);
        var evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: true);
        using var client = CreateOwnerClient();

        using var previewResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/preview",
            new { newSub, evidenceToken });
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await UpdateDisplayNameAsync(targetId, "Changed after preview");

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "A stale confirmation must not apply.",
                confirmed = true,
                evidenceToken,
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            confirmResponse,
            HttpStatusCode.Conflict,
            ApiMessages.AccessAdministrationStaleVersion);
        (await Fixture.GetUserBySubAsync(targetSub))!.DisplayName.Should().Be("Changed after preview");
        (await Fixture.GetUserBySubAsync(newSub)).Should().BeNull();
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentOwners_RelinkingTheSameTargetToTheSameSubject_CommitsOnceAndRejectsTheOtherAsStale()
    {
        await Fixture.VerifyPermissionCatalogueAsync();
        var firstOwnerId = await SeedActiveOwnerAsync();
        var secondOwnerId = await SeedActiveOwnerAsync(
            AccessTestFixture.SecondOwnerSub,
            AccessTestFixture.SecondOwnerEmail);
        const string targetSub = "access-admin-concurrent-relink-target";
        const string newSub = "access-admin-concurrent-relink-replacement";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        await AddGrantAsync(targetId, firstOwnerId, AbwabPermissions.Doors.Create);
        var target = (await Fixture.GetUserBySubAsync(targetSub))!;
        Fixture.ProfileSource.ReturnEmailFor(newSub, target.Email);
        var evidenceToken = EvidenceToken(newSub, target.Email, emailVerified: true);
        var profileBlock = Fixture.ProfileSource.BlockNextProfileFor(newSub);
        using var firstClient = CreateOwnerClient();
        using var secondClient = CreateOwnerClient(AccessTestFixture.SecondOwnerSub);

        var firstRequest = firstClient.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "Relink the target from the first Owner.",
                confirmed = true,
                evidenceToken,
            });
        var secondRequest = secondClient.PostAsJsonAsync(
            $"/api/access/users/{targetId}/logto-sub/relink/confirm",
            new
            {
                expectedVersion = target.Version,
                oldSub = targetSub,
                newSub,
                reason = "Relink the target from the second Owner.",
                confirmed = true,
                evidenceToken,
            });

        await profileBlock.WaitUntilEnteredAsync();
        profileBlock.Release();
        var responses = await Task.WhenAll(firstRequest, secondRequest);
        try
        {
            responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
                [HttpStatusCode.OK, HttpStatusCode.Conflict]);
            var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                conflict,
                HttpStatusCode.Conflict,
                ApiMessages.AccessAdministrationStaleVersion);

            var persisted = (await Fixture.GetUserBySubAsync(newSub))!;
            persisted.Id.Should().Be(targetId);
            persisted.Status.Should().Be(UserStatus.Active);
            persisted.RoleId.Should().BeNull();
            (await GetGrantCodesAsync(targetId)).Should().Equal(AbwabPermissions.Doors.Create);
            var audit = (await GetAuditEventsAsync(targetId)).Should().ContainSingle().Subject;
            audit.ActionType.Should().Be(AccessAuditActionType.LogtoSubjectRelinked);
            audit.ActorUserId.Should().Be(
                responses[0].StatusCode == HttpStatusCode.OK
                    ? firstOwnerId
                    : secondOwnerId);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task OwnerRelink_RequiresTheConfiguredOwnerToBeSuccessfullyReconciled()
    {
        var ownerId = await SeedActiveOwnerAsync();
        const string newSub = "access-admin-owner-relink-replacement";
        var owner = (await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!;
        Fixture.ProfileSource.ReturnEmailFor(AccessTestFixture.OwnerSub, "mismatched-owner@example.test");
        Fixture.ProfileSource.ReturnEmailFor(newSub, owner.Email);
        using var client = CreateOwnerClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/access/users/{ownerId}/logto-sub/relink/preview",
            new
            {
                newSub,
                evidenceToken = EvidenceToken(newSub, owner.Email, emailVerified: true),
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationOwnerRelinkNotReconciled);
        (await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!.RoleId.Should().NotBeNull();
        (await GetAuditEventsAsync(ownerId)).Should().BeEmpty();
    }

    private Task<int> SeedActiveOwnerAsync()
    {
        return SeedActiveOwnerAsync(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail);
    }

    private async Task<int> SeedActiveOwnerAsync(string sub, string email)
    {
        var ownerRoleId = (await Fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id;
        return await Fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = email,
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private HttpClient CreateOwnerClient(string ownerSub = AccessTestFixture.OwnerSub)
    {
        var client = Fixture.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(ownerSub));
        return client;
    }

    private async Task<int> SeedUserAsync(string sub, UserStatus status)
    {
        return await Fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task AddGrantAsync(int targetUserId, int actorUserId, string permissionCode)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionId = await db.AccessPermissions
            .Where(permission => permission.Code == permissionCode)
            .Select(permission => permission.Id)
            .SingleAsync();
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = targetUserId,
            PermissionId = permissionId,
            GrantedByUserId = actorUserId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task UpdateDisplayNameAsync(int userId, string displayName)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var user = await db.AccessUsers.SingleAsync(candidate => candidate.Id == userId);
        user.DisplayName = displayName;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<string>> GetGrantCodesAsync(int targetUserId)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUserPermissions.AsNoTracking()
            .Where(grant => grant.UserId == targetUserId)
            .OrderBy(grant => grant.Permission.DisplayOrder)
            .Select(grant => grant.Permission.Code)
            .ToListAsync();
    }

    private async Task<IReadOnlyList<AccessAuditEvent>> GetAuditEventsAsync(int targetUserId)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessAuditEvents.AsNoTracking()
            .Where(eventItem => eventItem.TargetUserId == targetUserId)
            .OrderBy(eventItem => eventItem.Id)
            .ToListAsync();
    }

    private static string EvidenceToken(string sub, string email, bool emailVerified) => TestJwtTokens.MintIdentityToken(
        sub,
        email,
        emailVerified);
}
