using QuranDashboard.Domain.Access;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class UserProvisioningServiceTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task GetOrCreateAsync_EmailCollidesWithDifferentSub_ThrowsEmailConflictException()
    {
        await fixture.ResetAsync();
        const string existingSub = "logto-user-email-conflict-existing";
        const string newSub = "logto-user-email-conflict-new";
        var conflictingEmail = FakeExternalUserProfileSource.EmailFor(existingSub);

        await fixture.InsertUserAsync(new User
        {
            LogtoSub = existingSub,
            Email = conflictingEmail,
            Status = UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        // The new subject's server-verified email collides with the pre-existing user's email above,
        // simulating a subject deleted+recreated in Logto (new sub, same verified email).
        fixture.ProfileSource.ReturnEmailFor(newSub, conflictingEmail);

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var act = () => provisioningService.GetOrCreateAsync(newSub, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<UserProvisioningEmailConflictException>();
        thrown.Which.Email.Should().Be(conflictingEmail);

        // No partial row leaks from the failed insert.
        (await fixture.GetUsersAsync()).Should().ContainSingle(u => u.LogtoSub == existingSub);
    }

    [Fact]
    public async Task GetOrCreateAsync_PersistsTheSharedNormalizedIdentityWhilePreservingDisplayEmail()
    {
        await fixture.ResetAsync();
        const string sub = "logto-normalized-display";
        const string displayEmail = " Teacher@Example.Test ";
        fixture.ProfileSource.ReturnEmailFor(sub, displayEmail);

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        await provisioningService.GetOrCreateAsync(sub, CancellationToken.None);

        var persisted = await fixture.GetUserBySubAsync(sub);
        persisted!.Email.Should().Be(displayEmail);
        persisted.NormalizedEmail.Should().Be("TEACHER@EXAMPLE.TEST");
    }

    [Fact]
    public async Task GetOrCreateAsync_NormalizedEmailCollidesAcrossDisplayFormatting_ThrowsWithoutMerge()
    {
        await fixture.ResetAsync();
        const string existingSub = "logto-normalized-existing";
        const string newSub = "logto-normalized-new";
        await fixture.InsertUserAsync(new User
        {
            LogtoSub = existingSub,
            Email = "owner@example.test",
            Status = UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.ProfileSource.ReturnEmailFor(newSub, " Owner@Example.Test ");

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var act = () => provisioningService.GetOrCreateAsync(newSub, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<UserProvisioningEmailConflictException>();
        thrown.Which.Email.Should().Be(" Owner@Example.Test ");
        (await fixture.GetUsersAsync()).Should().ContainSingle(user => user.LogtoSub == existingSub);
    }

    [Fact]
    public async Task GetOrCreateAsync_OwnerEmailFirstLogin_EmailUnverified_IsNotPromoted()
    {
        await fixture.ResetAsync();
        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail, verified: false),
            CancellationToken.None);

        result.Status.Should().Be(UserStatus.Pending);
        result.RoleId.Should().BeNull();
        result.RoleName.Should().BeNull();

        var persisted = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        persisted!.Status.Should().Be(UserStatus.Pending);
        persisted.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_OwnerEmailFirstLogin_EmailVerified_IsProvisionedOwnerActive()
    {
        await fixture.ResetAsync();
        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail, verified: true),
            CancellationToken.None);

        result.Status.Should().Be(UserStatus.Active);
        result.RoleName.Should().Be(RoleNames.Owner);

        var persisted = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        persisted!.Status.Should().Be(UserStatus.Active);
        persisted.RoleId.Should().NotBeNull();

        await using var queryScope = fixture.QueryServices.CreateAsyncScope();
        var db = queryScope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var audit = await db.AccessAuditEvents.SingleAsync(eventItem => eventItem.TargetUserId == persisted.Id);
        audit.ActionType.Should().Be(AccessAuditActionType.OwnerGrantedByReconciliation);
        AssertAuditSnapshots(audit, expectedTargetOwner: true, expectedBeforeOwner: false, expectedAfterOwner: true);
        audit.Metadata.Provenance["configuredNormalizedEmail"]
            .Should().Be(AccessTestFixture.OwnerEmail.ToUpperInvariant());
        audit.Metadata.Provenance["evidenceSource"].Should().Be("interactive-oidc");
    }

    [Fact]
    public async Task GetOrCreateAsync_SecondConfiguredVerifiedOwner_IsProvisionedOwnerActive()
    {
        await fixture.ResetAsync();

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.SecondOwnerSub, AccessTestFixture.SecondOwnerEmail, verified: true),
            CancellationToken.None);

        result.Status.Should().Be(UserStatus.Active);
        result.RoleName.Should().Be(RoleNames.Owner);
    }

    [Fact]
    public async Task GetOrCreateAsync_FirstVerifiedConfiguredOwner_BootstrapsWhileAnotherAwaitsItsOwnLogin()
    {
        await fixture.ResetAsync();

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();
        var reconciliation = scope.ServiceProvider.GetRequiredService<IOwnerReconciliationService>();

        var promoted = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail, verified: true),
            CancellationToken.None);
        var status = await reconciliation.GetStatusAsync(CancellationToken.None);

        promoted.RoleName.Should().Be(RoleNames.Owner);
        status.IsReady.Should().BeTrue();
        status.Candidates.Should().Contain(candidate => candidate.NormalizedEmail == AccessTestFixture.SecondOwnerEmail.ToUpperInvariant()
            && candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetOrCreateAsync_ConfiguredUserWithOneOrManyDirectGrants_PromotesAtomicallyAndAuditsEveryRevocation(
        int directGrantCount)
    {
        await fixture.ResetAsync();
        var now = DateTimeOffset.UtcNow;
        var ownerRoleId = (await fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id;
        await fixture.InsertUserAsync(new User
        {
            LogtoSub = AccessTestFixture.OwnerSub,
            Email = AccessTestFixture.OwnerEmail,
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        var grantorId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = "logto-owner-reconciliation-grantor",
            Email = "grantor@example.test",
            Status = UserStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        var targetId = await fixture.InsertUserAsync(new User
        {
            LogtoSub = AccessTestFixture.SecondOwnerSub,
            Email = AccessTestFixture.SecondOwnerEmail,
            Status = UserStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        var permissionCodes = new[] { AbwabPermissions.Doors.Create, AbwabPermissions.Doors.Edit }
            .Take(directGrantCount)
            .ToArray();

        await using (var scope = fixture.ApiServices.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var permissionIds = await db.AccessPermissions
                .Where(permission => new[] { AbwabPermissions.Doors.Create, AbwabPermissions.Doors.Edit }
                    .Contains(permission.Code))
                .ToDictionaryAsync(permission => permission.Code, permission => permission.Id);
            db.AccessUserPermissions.AddRange(permissionCodes.Select(permissionCode => new UserPermission
            {
                UserId = targetId,
                PermissionId = permissionIds[permissionCode],
                GrantedByUserId = grantorId,
                GrantedAtUtc = now,
            }));
            await db.SaveChangesAsync();
        }

        using (var scope = fixture.ApiServices.CreateScope())
        {
            var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

            await provisioningService.GetOrCreateAsync(
                Identity(AccessTestFixture.SecondOwnerSub, AccessTestFixture.SecondOwnerEmail, verified: true),
                CancellationToken.None);
        }

        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            var target = await db.AccessUsers.Include(user => user.Role).SingleAsync(user => user.Id == targetId);
            var auditEvents = await db.AccessAuditEvents
                .Where(eventItem => eventItem.TargetUserId == targetId)
                .OrderBy(eventItem => eventItem.Id)
                .ToListAsync();

            target.Status.Should().Be(UserStatus.Active);
            target.Role!.Name.Should().Be(RoleNames.Owner);
            (await db.AccessUserPermissions.CountAsync(grant => grant.UserId == targetId)).Should().Be(0);
            auditEvents.Select(eventItem => eventItem.ActionType).Should().Equal(
                Enumerable.Repeat(AccessAuditActionType.PermissionRevoked, directGrantCount)
                    .Append(AccessAuditActionType.OwnerGrantedByReconciliation));
            auditEvents.Take(directGrantCount).Select(eventItem => eventItem.PermissionCode).Should().Equal(
                permissionCodes.Order(StringComparer.Ordinal));
            foreach (var revokedGrant in auditEvents.Take(directGrantCount))
            {
                AssertAuditSnapshots(revokedGrant, expectedTargetOwner: false, expectedBeforeOwner: false, expectedAfterOwner: false);
            }

            AssertAuditSnapshots(
                auditEvents.Single(eventItem => eventItem.ActionType == AccessAuditActionType.OwnerGrantedByReconciliation),
                expectedTargetOwner: true,
                expectedBeforeOwner: false,
                expectedAfterOwner: true);
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_DisabledOwnerEmailUser_IsNeverRevivedOrPromoted()
    {
        await fixture.ResetAsync();
        var now = DateTimeOffset.UtcNow;
        await fixture.InsertUserAsync(new User
        {
            LogtoSub = AccessTestFixture.OwnerSub,
            Email = AccessTestFixture.OwnerEmail,
            Status = UserStatus.Disabled,
            RoleId = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail, verified: true),
            CancellationToken.None);

        result.Status.Should().Be(UserStatus.Disabled);
        result.RoleId.Should().BeNull();

        var persisted = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        persisted!.Status.Should().Be(UserStatus.Disabled);
        persisted.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_OwnerEmailFirstLogin_WithoutAnEmailClaim_IsNotPromoted()
    {
        await fixture.ResetAsync();

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.OwnerSub, null, verified: true),
            CancellationToken.None);

        result.Status.Should().Be(UserStatus.Pending);
        result.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_OwnerEmailFirstLogin_WithAMismatchedVerifiedEmailClaim_IsNotPromoted()
    {
        await fixture.ResetAsync();

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(
            Identity(AccessTestFixture.OwnerSub, "different@example.test", verified: true),
            CancellationToken.None);

        result.Status.Should().Be(UserStatus.Pending);
        result.RoleId.Should().BeNull();
    }

    private static AuthenticatedInteractiveIdentity Identity(string sub, string? email, bool verified)
        => new(sub, email, verified);

    private static void AssertAuditSnapshots(
        AccessAuditEvent audit,
        bool expectedTargetOwner,
        bool expectedBeforeOwner,
        bool expectedAfterOwner)
    {
        using var target = JsonDocument.Parse(audit.TargetSnapshotJson);
        using var before = JsonDocument.Parse(audit.BeforeStateJson!);
        using var after = JsonDocument.Parse(audit.AfterStateJson!);
        target.RootElement.GetProperty("DisplayName").ValueKind.Should().NotBe(JsonValueKind.Undefined);
        target.RootElement.GetProperty("IsOwner").GetBoolean().Should().Be(expectedTargetOwner);
        before.RootElement.GetProperty("IsOwner").GetBoolean().Should().Be(expectedBeforeOwner);
        after.RootElement.GetProperty("IsOwner").GetBoolean().Should().Be(expectedAfterOwner);
    }
}
