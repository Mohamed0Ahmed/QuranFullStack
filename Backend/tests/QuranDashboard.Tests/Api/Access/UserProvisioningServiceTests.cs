using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// Service-level Testcontainers tests for the email-unique-collision provisioning path: a subject
/// deleted and recreated in Logto presents a brand-new <c>sub</c> whose server-verified email already
/// belongs to a different, existing local user. Runs against the real Postgres container + migrations
/// shared with the <c>/api/access/me</c> suite, resolving <see cref="IUserProvisioningService"/> straight
/// from the pipeline's DI container so the real <c>UserProvisioningService</c> and its unique-index
/// handling run for real.
/// </summary>
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
}
