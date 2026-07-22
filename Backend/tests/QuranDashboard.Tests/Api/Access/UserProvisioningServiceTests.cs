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

        fixture.ProfileSource.ReturnEmailFor(newSub, conflictingEmail);

        using var scope = fixture.ApiServices.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        var act = () => provisioningService.GetOrCreateAsync(newSub, CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<UserProvisioningEmailConflictException>();
        thrown.Which.Email.Should().Be(conflictingEmail);

        (await fixture.GetUsersAsync()).Should().ContainSingle(u => u.LogtoSub == existingSub);
    }

    [Fact]
    public async Task GetOrCreateAsync_OwnerEmailFirstLogin_EmailUnverified_IsNotPromoted()
    {
        await fixture.ResetAsync();
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

        var result = await provisioningService.GetOrCreateAsync(AccessTestFixture.OwnerSub, CancellationToken.None);

        result.Status.Should().Be(UserStatus.Disabled);
        result.RoleId.Should().BeNull();

        var persisted = await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub);
        persisted!.Status.Should().Be(UserStatus.Disabled);
        persisted.RoleId.Should().BeNull();
    }
}
