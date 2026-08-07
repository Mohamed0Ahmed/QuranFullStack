using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Application.Access.OwnerReconciliation;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class OwnerReconciliationServiceTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task GetStatusAsync_UnprovisionedConfiguredOwner_AwaitsVerifiedSignInWithoutAddingIt()
    {
        await fixture.ResetAsync();

        using var scope = fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);

        result.Candidates.Should().Contain(candidate => candidate.NormalizedEmail == AccessTestFixture.OwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
        result.IsReady.Should().BeFalse();
        (await fixture.GetUsersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusAsync_OneConfiguredOwnerActiveWhileAnotherAwaitsLogin_IsReady()
    {
        await fixture.ResetAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);

        using var scope = fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);

        result.IsReady.Should().BeTrue();
        result.Candidates.Should().Contain(candidate => candidate.NormalizedEmail == AccessTestFixture.SecondOwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
    }

    [Fact]
    public async Task GetStatusAsync_UnconfiguredLastOwnerWhileConfiguredCandidatesAwaitSignIn_IsNotReady()
    {
        await fixture.ResetAsync();
        const string unconfiguredSub = "logto-status-unconfigured-owner";
        var unconfiguredOwnerId = await SeedOwnerAsync(
            unconfiguredSub,
            FakeExternalUserProfileSource.EmailFor(unconfiguredSub));

        using var scope = fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);

        result.IsReady.Should().BeFalse();
        result.Candidates.Should().Contain(candidate => candidate.UserId == unconfiguredOwnerId
            && candidate.State == OwnerReconciliationCandidateState.RemovalBlockedByLastOwner);
        result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn)
            .Should().Be(2);
    }

    [Fact]
    public async Task ReconcileAsync_AlreadyReadyState_IsAnIdempotentReadyNoOp()
    {
        await fixture.ResetAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);

        using var scope = fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Confirm a ready Owner state.", CancellationToken.None);

        result.CanApply.Should().BeTrue();
        result.IsReady.Should().BeTrue();
        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_MismatchedSubjectDoesNotPromoteTheConfiguredUser()
    {
        await fixture.ResetAsync();
        var userId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Pending, owner: false);

        using var scope = fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileInteractiveSignInAsync(
                new AuthenticatedInteractiveIdentity(
                    "different-logto-sub",
                    AccessTestFixture.OwnerEmail,
                    true),
                CancellationToken.None);

        result.CanApply.Should().BeFalse();
        result.Candidates.Should().Contain(candidate => candidate.UserId == userId
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
        (await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_ConfigurationRemovedBeforeLockAcquisition_DoesNotPromoteTheRemovedCandidate()
    {
        await fixture.ResetAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Pending, owner: false);
        var configurationSource = new MutableOwnerBootstrapConfigurationSource(
            Configuration("before-lock", AccessTestFixture.OwnerEmail));

        using var scope = fixture.ApiServices.CreateScope();
        var store = new ConfigurationChangingOwnerReconciliationStore(
            scope.ServiceProvider.GetRequiredService<IOwnerReconciliationStore>(),
            () => configurationSource.SetCurrent(Configuration("under-lock", AccessTestFixture.SecondOwnerEmail)));
        var reconciliation = new OwnerReconciliationService(
            store,
            configurationSource,
            scope.ServiceProvider.GetRequiredService<IExternalUserProfileSource>(),
            scope.ServiceProvider.GetRequiredService<IEmailIdentityNormalizer>());

        var result = await reconciliation.ReconcileInteractiveSignInAsync(
            Identity(AccessTestFixture.OwnerSub),
            CancellationToken.None);

        result.IsReady.Should().BeFalse();
        result.Candidates.Should().ContainSingle(candidate =>
            candidate.NormalizedEmail == AccessTestFixture.SecondOwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
        var user = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        user!.Status.Should().Be(UserStatus.Pending);
        user.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task ReconcileAsync_UnconfiguredOwner_RemovesTheRoleAndPersistsAccurateAuditSnapshots()
    {
        await fixture.ResetAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        const string removedSub = "logto-owner-removed";
        var removedOwnerId = await SeedOwnerAsync(removedSub, FakeExternalUserProfileSource.EmailFor(removedSub));

        using (var scope = fixture.ApiServices.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
                .ReconcileAsync("Remove a retired Owner.", CancellationToken.None);

            result.IsReady.Should().BeTrue();
        }

        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUsers.SingleAsync(user => user.Id == removedOwnerId)).RoleId.Should().BeNull();
        var audit = await db.AccessAuditEvents.SingleAsync(eventItem => eventItem.TargetUserId == removedOwnerId);
        using var target = JsonDocument.Parse(audit.TargetSnapshotJson);
        using var before = JsonDocument.Parse(audit.BeforeStateJson!);
        using var after = JsonDocument.Parse(audit.AfterStateJson!);
        target.RootElement.GetProperty("DisplayName").ValueKind.Should().NotBe(JsonValueKind.Undefined);
        target.RootElement.GetProperty("IsOwner").GetBoolean().Should().BeFalse();
        before.RootElement.GetProperty("IsOwner").GetBoolean().Should().BeTrue();
        after.RootElement.GetProperty("IsOwner").GetBoolean().Should().BeFalse();
        audit.Metadata.Provenance["configuredNormalizedEmail"]
            .Should().Be(FakeExternalUserProfileSource.EmailFor(removedSub).ToUpperInvariant());
        audit.Metadata.Provenance["evidenceSource"].Should().Be("m2m-safe-reconciliation");
    }

    [Fact]
    public async Task GetStatusAsync_ExistingOwnerWithDirectGrants_IsNotReadyAndOperatorCleanupRevokesThem()
    {
        await fixture.ResetAsync();
        var ownerId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var grantorId = await SeedUserAsync("logto-grantor", "grantor@example.test", UserStatus.Active);
        await SeedDirectGrantAsync(ownerId, grantorId);

        using (var scope = fixture.ApiServices.CreateScope())
        {
            var reconciliation = scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>();
            var status = await reconciliation.GetStatusAsync(CancellationToken.None);
            status.IsReady.Should().BeFalse();
            status.Candidates.Should().Contain(candidate => candidate.UserId == ownerId
                && candidate.State == OwnerReconciliationCandidateState.OwnerHasDirectGrants);

            (await reconciliation.ReconcileAsync("Remove direct Owner grant.", CancellationToken.None)).IsReady.Should().BeTrue();
        }

        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == ownerId)).Should().Be(0);
        (await db.AccessAuditEvents.Where(eventItem => eventItem.TargetUserId == ownerId)
            .Select(eventItem => eventItem.ActionType).ToListAsync())
            .Should().Contain(AccessAuditActionType.PermissionRevoked);
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_AuditInsertFailure_RollsBackPromotionAndGrantRevocation()
    {
        await fixture.ResetAsync();
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

    [Fact]
    public async Task ReconcileAsync_ConcurrentDirectGrantAdditionWaitsForTheLeaseAndIsDetectedByTheNextPreflight()
    {
        await fixture.ResetAsync();
        var ownerId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var grantorId = await SeedUserAsync("logto-concurrent-grantor", "concurrent-grantor@example.test", UserStatus.Active);
        await SeedDirectGrantAsync(ownerId, grantorId);
        var profileBlock = fixture.ProfileSource.BlockNextProfileFor(AccessTestFixture.OwnerSub);

        using var reconciliationScope = fixture.ApiServices.CreateScope();
        var reconciliation = reconciliationScope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>();
        var firstReconciliation = reconciliation.ReconcileAsync("Remove an existing direct Owner grant.", CancellationToken.None);
        await profileBlock.WaitUntilEnteredAsync();

        var saveAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var concurrentGrant = AddDirectGrantAsync(
            ownerId,
            grantorId,
            AbwabPermissions.Doors.Edit,
            saveAttempted);
        await saveAttempted.Task;
        (await Task.WhenAny(concurrentGrant, Task.Delay(250))).Should().NotBe(concurrentGrant);

        profileBlock.Release();
        (await firstReconciliation).IsReady.Should().BeTrue();
        await concurrentGrant;

        using var statusScope = fixture.ApiServices.CreateScope();
        var status = await statusScope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);
        status.IsReady.Should().BeFalse();
        status.Candidates.Should().Contain(candidate => candidate.UserId == ownerId
            && candidate.State == OwnerReconciliationCandidateState.OwnerHasDirectGrants);
    }

    [Fact]
    public async Task ReconcileAsync_ProviderUnavailableBeforeRemoval_FailsClosedAndLeavesTheOwnerIntact()
    {
        await fixture.ResetAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var removedOwnerId = await SeedOwnerAsync("logto-owner-unavailable", "owner-unavailable@example.test");
        fixture.ProfileSource.ReturnUnavailableFor("logto-owner-unavailable");

        using var scope = fixture.ApiServices.CreateScope();
        var reconcile = () => scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Attempt a safe removal.", CancellationToken.None);

        await reconcile.Should().ThrowAsync<HttpRequestException>();
        (await fixture.GetUserBySubAsync("logto-owner-unavailable"))!.Id.Should().Be(removedOwnerId);
        (await fixture.GetUserBySubAsync("logto-owner-unavailable"))!.RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileAsync_LastActiveOwnerRemoval_IsBlocked()
    {
        await fixture.ResetAsync();
        const string ownerSub = "logto-last-owner";
        var ownerId = await SeedOwnerAsync(ownerSub, FakeExternalUserProfileSource.EmailFor(ownerSub));

        using var scope = fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Attempt last Owner removal.", CancellationToken.None);

        result.CanApply.Should().BeFalse();
        result.Candidates.Should().Contain(candidate => candidate.UserId == ownerId
            && candidate.State == OwnerReconciliationCandidateState.RemovalBlockedByLastOwner);
        (await fixture.GetUserBySubAsync(ownerSub))!.RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_ConcurrentAdditionAndRemovalRejectTheSecondAttemptAndRecoverAfterLockRelease()
    {
        await fixture.ResetAsync();
        await SeedConfiguredOwnerAsync(AccessTestFixture.SecondOwnerSub, UserStatus.Pending, owner: false);
        const string removedSub = "logto-concurrent-removed-owner";
        var removedOwnerId = await SeedOwnerAsync(removedSub, FakeExternalUserProfileSource.EmailFor(removedSub));
        var profileBlock = fixture.ProfileSource.BlockNextProfileFor(AccessTestFixture.SecondOwnerSub);

        using var firstScope = fixture.ApiServices.CreateScope();
        using var secondScope = fixture.ApiServices.CreateScope();
        var firstReconciliation = firstScope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileInteractiveSignInAsync(Identity(AccessTestFixture.SecondOwnerSub), CancellationToken.None);
        await profileBlock.WaitUntilEnteredAsync();

        var secondReconciliation = () => secondScope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Concurrent safe removal.", CancellationToken.None);

        await secondReconciliation.Should().ThrowAsync<OwnerReconciliationLockUnavailableException>();
        profileBlock.Release();
        (await firstReconciliation).IsReady.Should().BeTrue();

        var promotedUser = await fixture.GetUserBySubAsync(AccessTestFixture.SecondOwnerSub);
        promotedUser!.Status.Should().Be(UserStatus.Active);
        promotedUser.RoleId.Should().NotBeNull();

        using var retryScope = fixture.ApiServices.CreateScope();
        (await retryScope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Retry safe removal after promotion.", CancellationToken.None)).IsReady.Should().BeTrue();
        (await fixture.GetUserBySubAsync(removedSub))!.Id.Should().Be(removedOwnerId);
        (await fixture.GetUserBySubAsync(removedSub))!.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task ReconcileAsync_ReasonExceedingTheStorageBound_IsRejectedBeforeAnyMutation()
    {
        await fixture.ResetAsync();
        var ownerId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var grantorId = await SeedUserAsync("logto-reason-grantor", "reason-grantor@example.test", UserStatus.Active);
        await SeedDirectGrantAsync(ownerId, grantorId);

        using var scope = fixture.ApiServices.CreateScope();
        var reconcile = () => scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync(new string('x', 1025), CancellationToken.None);

        await reconcile.Should().ThrowAsync<OwnerReconciliationReasonValidationException>();
        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == ownerId)).Should().Be(1);
        (await db.AccessAuditEvents.CountAsync()).Should().Be(0);
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

    private async Task<int> SeedOwnerAsync(string sub, string email)
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
        await using var apiScope = fixture.ApiServices.CreateAsyncScope();
        await apiScope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);

        await AddDirectGrantAsync(userId, grantorId, AbwabPermissions.Doors.Create);
    }

    private async Task AddDirectGrantAsync(
        int userId,
        int grantorId,
        string permissionCode,
        TaskCompletionSource? saveAttempted = null)
    {
        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionId = await db.AccessPermissions
            .Where(permission => permission.Code == permissionCode)
            .Select(permission => permission.Id)
            .SingleAsync();
        db.AccessUserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            GrantedByUserId = grantorId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
        });
        saveAttempted?.TrySetResult();
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

    private static OwnerBootstrapConfiguration Configuration(string fingerprint, string email)
        => new(
            new HashSet<string>([email.ToUpperInvariant()], StringComparer.Ordinal),
            fingerprint);

    private sealed class MutableOwnerBootstrapConfigurationSource(OwnerBootstrapConfiguration current)
        : IOwnerBootstrapConfigurationSource
    {
        private OwnerBootstrapConfiguration _current = current;

        public OwnerBootstrapConfiguration GetCurrent() => _current;

        public void SetCurrent(OwnerBootstrapConfiguration current)
        {
            _current = current;
        }
    }

    private sealed class ConfigurationChangingOwnerReconciliationStore(
        IOwnerReconciliationStore inner,
        Action beforeAcquire) : IOwnerReconciliationStore
    {
        public Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
            OwnerBootstrapConfiguration configuration,
            CancellationToken cancellationToken)
        {
            return inner.ReadSnapshotAsync(configuration, cancellationToken);
        }

        public Task<IOwnerReconciliationLease?> TryAcquireLeaseAsync(CancellationToken cancellationToken)
        {
            beforeAcquire();
            return inner.TryAcquireLeaseAsync(cancellationToken);
        }
    }
}
