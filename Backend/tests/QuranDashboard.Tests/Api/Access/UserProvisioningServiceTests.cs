using QuranDashboard.Domain.Access;

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
        // decision 3: the configured Owner email with no linked social/SSO identity behind it must be
        // provisioned exactly like a normal user — Pending, no role — not Owner/Active.
        fixture.ProfileSource.ReturnUnverifiedFor(AccessTestFixture.OwnerSub);

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(AccessTestFixture.OwnerSub, CancellationToken.None);

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
        // The fake defaults every profile to IdP-verified, matching a real owner login backed by a
        // linked social/SSO identity.
        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var result = await provisioningService.GetOrCreateAsync(AccessTestFixture.OwnerSub, CancellationToken.None);

        result.Status.Should().Be(UserStatus.Active);
        result.RoleName.Should().Be(RoleNames.Owner);

        var persisted = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        persisted!.Status.Should().Be(UserStatus.Active);
        persisted.RoleId.Should().NotBeNull();
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

        // decision 3: login must never auto-revive or auto-promote a Disabled user, even for the
        // configured Owner email with a verified profile.
        var result = await provisioningService.GetOrCreateAsync(AccessTestFixture.OwnerSub, CancellationToken.None);

        result.Status.Should().Be(UserStatus.Disabled);
        result.RoleId.Should().BeNull();

        var persisted = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        persisted!.Status.Should().Be(UserStatus.Disabled);
        persisted.RoleId.Should().BeNull();
    }
}
