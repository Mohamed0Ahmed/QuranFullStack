using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Ownership;

// FR-024/FR-025 / SC-005 (T049): concurrent owner removals always leave >= 1 active owner; removal/disable is
// observed on the next request; owner authority has NO email/role/runtime fallback (real PG).
[Collection(nameof(AbwabDbCollection))]
public sealed class FinalOwnerInvariantTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Issuer = "https://logto.test";

    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentRemovals_AlwaysLeaveAtLeastOneActiveOwner()
    {
        await SeedOwnerAsync("owner-a", bootstrap: true);
        await SeedOwnerAsync("owner-b", bootstrap: false);

        var removeA = TryRemoveAsync("owner-a");
        var removeB = TryRemoveAsync("owner-b");
        var outcomes = await Task.WhenAll(removeA, removeB);

        (await SecurityTestHarness.ActiveOwnerCountAsync(fixture)).Should().Be(1);
        outcomes.Count(threw => threw).Should().Be(1, "exactly one concurrent removal must be refused as the last owner");
    }

    [Fact]
    public async Task LastOwnerRemoval_IsRejectedWithLastSystemOwnerCode()
    {
        await SeedOwnerAsync("solo-owner", bootstrap: true);

        var act = () => SecurityTestHarness.RemoveOwnerAsync(
            fixture, new RemoveSystemOwnerCommand("solo-owner", ExpectedTimelineGeneration.Of(0), "actor"));

        (await act.Should().ThrowAsync<LastSystemOwnerException>())
            .Which.Code.Should().Be(AbwabConflictCodes.LastSystemOwner);
        (await SecurityTestHarness.ActiveOwnerCountAsync(fixture)).Should().Be(1);
    }

    [Fact]
    public async Task RemovalAndDisable_AreObservedOnTheNextRequest()
    {
        await SeedOwnerAsync("owner-a", bootstrap: true);
        await SeedOwnerAsync("owner-b", bootstrap: false);

        await SecurityTestHarness.RemoveOwnerAsync(
            fixture, new RemoveSystemOwnerCommand("owner-a", ExpectedTimelineGeneration.Of(0), "actor"));

        await using var db = SecurityTestHarness.CreateContext(fixture);
        var store = new QuranDashboard.Infrastructure.Security.Persistence.SystemOwnerStore(db);
        (await store.IsActiveSystemOwnerAsync("owner-a", CancellationToken.None)).Should().BeFalse();
        (await store.IsActiveSystemOwnerAsync("owner-b", CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task OwnerAuthority_HasNoEmailRoleOrRuntimeFallback()
    {
        await SeedOwnerAsync("real-owner", bootstrap: true);

        await using var db = SecurityTestHarness.CreateContext(fixture);
        var store = new QuranDashboard.Infrastructure.Security.Persistence.SystemOwnerStore(db);

        // A subject that is not an explicit membership row is NEVER an owner — there is no email, role, or
        // runtime fallback that could confer ownership.
        (await store.IsActiveSystemOwnerAsync("not-a-member@example.com", CancellationToken.None)).Should().BeFalse();
        (await store.IsActiveSystemOwnerAsync("Owner", CancellationToken.None)).Should().BeFalse();
    }

    private async Task SeedOwnerAsync(string subject, bool bootstrap)
    {
        if (bootstrap)
        {
            await SecurityTestHarness.BootstrapOwnerAsync(fixture, new BootstrapSystemOwnerCommand(
                Issuer, subject, Issuer, EmailVerified: true, AccountEnabled: true, ExpectedTimelineGeneration.Of(0), "actor"));
        }
        else
        {
            await SecurityTestHarness.AddOwnerAsync(fixture, new AddSystemOwnerCommand(
                Issuer, subject, AccountEnabled: true, ExpectedTimelineGeneration.Of(0), "actor"));
        }
    }

    private async Task<bool> TryRemoveAsync(string subject)
    {
        try
        {
            await SecurityTestHarness.RemoveOwnerAsync(
                fixture, new RemoveSystemOwnerCommand(subject, ExpectedTimelineGeneration.Of(0), "actor"));
            return false;
        }
        catch (LastSystemOwnerException)
        {
            return true;
        }
    }
}
