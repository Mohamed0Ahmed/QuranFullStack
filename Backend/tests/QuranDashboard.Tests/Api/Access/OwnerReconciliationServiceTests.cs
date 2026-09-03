using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Application.Access.OwnerReconciliation;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class OwnerReconciliationServiceTests(AccessTestFixture fixture) : AccessMutableWriterTest(fixture)
{
    [Fact]
    public async Task GetStatusAsync_UnprovisionedConfiguredOwner_AwaitsVerifiedSignInWithoutAddingIt()
    {

        using var scope = Fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);

        result.Candidates.Should().Contain(candidate => candidate.NormalizedEmail == AccessTestFixture.OwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
        result.IsReady.Should().BeFalse();
        (await Fixture.GetUsersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusAsync_OneConfiguredOwnerActiveWhileAnotherAwaitsLogin_IsReady()
    {
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);

        using var scope = Fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);

        result.IsReady.Should().BeTrue();
        result.Candidates.Should().Contain(candidate => candidate.NormalizedEmail == AccessTestFixture.SecondOwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
    }

    [Fact]
    public async Task GetStatusAsync_UnconfiguredLastOwnerWhileConfiguredCandidatesAwaitSignIn_IsNotReady()
    {
        const string unconfiguredSub = "logto-status-unconfigured-owner";
        var unconfiguredOwnerId = await SeedOwnerAsync(
            unconfiguredSub,
            FakeExternalUserProfileSource.EmailFor(unconfiguredSub));

        using var scope = Fixture.ApiServices.CreateScope();
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
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);

        using var scope = Fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Confirm a ready Owner state.", CancellationToken.None);

        result.CanApply.Should().BeTrue();
        result.IsReady.Should().BeTrue();
        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_MismatchedSubjectDoesNotPromoteTheConfiguredUser()
    {
        var userId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Pending, owner: false);

        using var scope = Fixture.ApiServices.CreateScope();
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
        (await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_ConfigurationDrift_RepreparesProviderEvidenceBeforeCommit()
    {
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Pending, owner: false);
        var configurationSource = new MutableOwnerBootstrapConfigurationSource(
            Configuration("initial", AccessTestFixture.OwnerEmail));

        using var scope = Fixture.ApiServices.CreateScope();
        var store = new CommitBarrierOwnerReconciliationStore(
            new OwnerReconciliationStore(
                scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>(),
                configurationSource));
        var reconciliation = new OwnerReconciliationService(
            store,
            configurationSource,
            Fixture.ProfileSource,
            scope.ServiceProvider.GetRequiredService<IEmailIdentityNormalizer>());

        var reconciliationTask = reconciliation.ReconcileInteractiveSignInAsync(
            Identity(AccessTestFixture.OwnerSub),
            CancellationToken.None);
        await store.WaitUntilFirstCommitAsync();
        configurationSource.SetCurrent(Configuration("changed", AccessTestFixture.OwnerEmail));
        store.ReleaseFirstCommit();

        var result = await reconciliationTask;

        result.IsReady.Should().BeTrue();
        result.ConfigurationFingerprint.Should().Be("changed");
        result.Candidates.Should().ContainSingle(candidate =>
            candidate.NormalizedEmail == AccessTestFixture.OwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.Added);
        Fixture.ProfileSource.CallsFor(AccessTestFixture.OwnerSub).Should().Be(2);
        store.CommitAttempts.Should().Be(2);
        var user = await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        user!.Status.Should().Be(UserStatus.Active);
        user.RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileAsync_UnconfiguredOwner_RemovesTheRoleAndPersistsAccurateAuditSnapshots()
    {
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        const string removedSub = "logto-owner-removed";
        var removedOwnerId = await SeedOwnerAsync(removedSub, FakeExternalUserProfileSource.EmailFor(removedSub));

        using (var scope = Fixture.ApiServices.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
                .ReconcileAsync("Remove a retired Owner.", CancellationToken.None);

            result.IsReady.Should().BeTrue();
        }

        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
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
        var ownerId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var grantorId = await SeedUserAsync("logto-grantor", "grantor@example.test", UserStatus.Active);
        await SeedDirectGrantAsync(ownerId, grantorId);

        using (var scope = Fixture.ApiServices.CreateScope())
        {
            var reconciliation = scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>();
            var status = await reconciliation.GetStatusAsync(CancellationToken.None);
            status.IsReady.Should().BeFalse();
            status.Candidates.Should().Contain(candidate => candidate.UserId == ownerId
                && candidate.State == OwnerReconciliationCandidateState.OwnerHasDirectGrants);

            (await reconciliation.ReconcileAsync("Remove direct Owner grant.", CancellationToken.None)).IsReady.Should().BeTrue();
        }

        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == ownerId)).Should().Be(0);
        (await db.AccessAuditEvents.Where(eventItem => eventItem.TargetUserId == ownerId)
            .Select(eventItem => eventItem.ActionType).ToListAsync())
            .Should().Contain(AccessAuditActionType.PermissionRevoked);
    }

    [Fact]
    public async Task GetStatusAsync_DisabledConfiguredOwnerWithDirectGrants_RequiresOperatorCleanupWithoutReactivation()
    {
        var activeOwnerId = await SeedConfiguredOwnerAsync(
            AccessTestFixture.SecondOwnerSub,
            UserStatus.Active,
            owner: true);
        var disabledOwnerId = await SeedConfiguredOwnerAsync(
            AccessTestFixture.OwnerSub,
            UserStatus.Disabled,
            owner: true);
        await SeedDirectGrantAsync(disabledOwnerId, activeOwnerId);

        using (var scope = Fixture.ApiServices.CreateScope())
        {
            var reconciliation = scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>();
            var status = await reconciliation.GetStatusAsync(CancellationToken.None);

            status.IsReady.Should().BeFalse();
            status.Candidates.Should().Contain(candidate => candidate.UserId == disabledOwnerId
                && candidate.State == OwnerReconciliationCandidateState.OwnerHasDirectGrants);
            (await reconciliation.ReconcileAsync(
                "Remove the disabled Owner's direct grant.",
                CancellationToken.None)).IsReady.Should().BeTrue();
        }

        var disabledOwner = await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        disabledOwner!.Status.Should().Be(UserStatus.Disabled);
        disabledOwner.RoleId.Should().NotBeNull();
        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == disabledOwnerId)).Should().Be(0);
        (await db.AccessAuditEvents.Where(eventItem => eventItem.TargetUserId == disabledOwnerId)
            .Select(eventItem => eventItem.ActionType).ToListAsync())
            .Should().Equal(AccessAuditActionType.PermissionRevoked);
    }

    [Fact]
    public async Task ReconcileAsync_ProviderBlocked_AllowsConcurrentGrantAndRepreparesBeforeCommit()
    {
        var ownerId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var grantorId = await SeedUserAsync("logto-concurrent-grantor", "concurrent-grantor@example.test", UserStatus.Active);
        await SeedDirectGrantAsync(ownerId, grantorId);
        var profileBlock = Fixture.ProfileSource.BlockNextProfileFor(AccessTestFixture.OwnerSub);

        using var reconciliationScope = Fixture.ApiServices.CreateScope();
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
        await concurrentGrant.WaitAsync(TimeSpan.FromSeconds(10));

        profileBlock.Release();
        (await firstReconciliation).IsReady.Should().BeTrue();
        Fixture.ProfileSource.CallsFor(AccessTestFixture.OwnerSub).Should().Be(2);

        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == ownerId)).Should().Be(0);
    }

    [Fact]
    public async Task ReconcileAsync_ProviderUnavailableBeforeRemoval_FailsClosedAndLeavesTheOwnerIntact()
    {
        await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var removedOwnerId = await SeedOwnerAsync("logto-owner-unavailable", "owner-unavailable@example.test");
        Fixture.ProfileSource.ReturnUnavailableFor("logto-owner-unavailable");

        using var scope = Fixture.ApiServices.CreateScope();
        var reconcile = () => scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Attempt a safe removal.", CancellationToken.None);

        await reconcile.Should().ThrowAsync<HttpRequestException>();
        (await Fixture.GetUserBySubAsync("logto-owner-unavailable"))!.Id.Should().Be(removedOwnerId);
        (await Fixture.GetUserBySubAsync("logto-owner-unavailable"))!.RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileAsync_LastActiveOwnerRemoval_IsBlocked()
    {
        const string ownerSub = "logto-last-owner";
        var ownerId = await SeedOwnerAsync(ownerSub, FakeExternalUserProfileSource.EmailFor(ownerSub));

        using var scope = Fixture.ApiServices.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Attempt last Owner removal.", CancellationToken.None);

        result.CanApply.Should().BeFalse();
        result.Candidates.Should().Contain(candidate => candidate.UserId == ownerId
            && candidate.State == OwnerReconciliationCandidateState.RemovalBlockedByLastOwner);
        (await Fixture.GetUserBySubAsync(ownerSub))!.RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileInteractiveSignInAsync_ConcurrentPromotionAndRemoval_ConvergeWithoutProviderTimeLockFailure()
    {
        var existingOwnerId = await SeedConfiguredOwnerAsync(
            AccessTestFixture.OwnerSub,
            UserStatus.Active,
            owner: true);
        await SeedConfiguredOwnerAsync(AccessTestFixture.SecondOwnerSub, UserStatus.Pending, owner: false);
        const string removedSub = "logto-concurrent-removed-owner";
        var removedOwnerId = await SeedOwnerAsync(removedSub, FakeExternalUserProfileSource.EmailFor(removedSub));
        var promotionProfileSource = new FakeExternalUserProfileSource();
        var profileBlock = promotionProfileSource.BlockNextProfileFor(AccessTestFixture.SecondOwnerSub);

        using var firstScope = Fixture.ApiServices.CreateScope();
        using var secondScope = Fixture.ApiServices.CreateScope();
        var promotion = new OwnerReconciliationService(
                firstScope.ServiceProvider.GetRequiredService<IOwnerReconciliationStore>(),
                firstScope.ServiceProvider.GetRequiredService<IOwnerBootstrapConfigurationSource>(),
                promotionProfileSource,
                firstScope.ServiceProvider.GetRequiredService<IEmailIdentityNormalizer>())
            .ReconcileInteractiveSignInAsync(Identity(AccessTestFixture.SecondOwnerSub), CancellationToken.None);
        await profileBlock.WaitUntilEnteredAsync();

        var removal = secondScope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync("Concurrent safe removal.", CancellationToken.None);

        (await removal).IsReady.Should().BeTrue();
        profileBlock.Release();
        (await promotion).IsReady.Should().BeTrue();
        promotionProfileSource.CallsFor(AccessTestFixture.SecondOwnerSub).Should().Be(2);

        var promotedUser = await Fixture.GetUserBySubAsync(AccessTestFixture.SecondOwnerSub);
        promotedUser!.Status.Should().Be(UserStatus.Active);
        promotedUser.RoleId.Should().NotBeNull();
        (await Fixture.GetUserBySubAsync(removedSub))!.Id.Should().Be(removedOwnerId);
        (await Fixture.GetUserBySubAsync(removedSub))!.RoleId.Should().BeNull();
        (await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!.Id.Should().Be(existingOwnerId);
        (await Fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!.RoleId.Should().NotBeNull();
    }

    [Fact]
    public async Task ReconcileAsync_ReasonExceedingTheStorageBound_IsRejectedBeforeAnyMutation()
    {
        var ownerId = await SeedConfiguredOwnerAsync(AccessTestFixture.OwnerSub, UserStatus.Active, owner: true);
        var grantorId = await SeedUserAsync("logto-reason-grantor", "reason-grantor@example.test", UserStatus.Active);
        await SeedDirectGrantAsync(ownerId, grantorId);

        using var scope = Fixture.ApiServices.CreateScope();
        var reconcile = () => scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync(new string('x', 1025), CancellationToken.None);

        await reconcile.Should().ThrowAsync<OwnerReconciliationReasonValidationException>();
        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
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
            ? (await Fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id
            : (int?)null;
        return await Fixture.InsertUserAsync(new User
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

    private async Task<int> SeedUserAsync(string sub, string email, UserStatus status)
    {
        return await Fixture.InsertUserAsync(new User
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
        await Fixture.VerifyPermissionCatalogueAsync();

        await AddDirectGrantAsync(userId, grantorId, AbwabPermissions.Doors.Create);
    }

    private async Task AddDirectGrantAsync(
        int userId,
        int grantorId,
        string permissionCode,
        TaskCompletionSource? saveAttempted = null)
    {
        await using var queryScope = Fixture.QueryServices.CreateAsyncScope();
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

    private sealed class CommitBarrierOwnerReconciliationStore(IOwnerReconciliationStore inner)
        : IOwnerReconciliationStore
    {
        private readonly TaskCompletionSource firstCommitEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource firstCommitReleased =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int commitAttempts;

        public int CommitAttempts => Volatile.Read(ref commitAttempts);

        public Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
            OwnerBootstrapConfiguration configuration,
            CancellationToken cancellationToken)
        {
            return inner.ReadSnapshotAsync(configuration, cancellationToken);
        }

        public async Task<OwnerReconciliationCommitResult> TryCommitAsync(
            OwnerReconciliationCommitIntent intent,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref commitAttempts) == 1)
            {
                firstCommitEntered.TrySetResult();
                await firstCommitReleased.Task.WaitAsync(cancellationToken);
            }

            return await inner.TryCommitAsync(intent, cancellationToken);
        }

        public Task WaitUntilFirstCommitAsync() => firstCommitEntered.Task;

        public void ReleaseFirstCommit() => firstCommitReleased.TrySetResult();
    }
}
