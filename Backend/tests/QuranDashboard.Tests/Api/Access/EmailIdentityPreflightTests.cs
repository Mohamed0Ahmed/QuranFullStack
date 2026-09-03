using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class EmailIdentityPreflightTests(AccessTestFixture fixture) : AccessMutableWriterTest(fixture)
{
    [Fact]
    public async Task ScanAsync_ReportsInvalidAndMismatchedIdentityRowsWithoutChangingThem()
    {
        var invalidUserId = await Fixture.InsertPersonaAsync("ReadOnly");
        var mismatchedUserId = await Fixture.InsertUserAsync(new QuranDashboard.Domain.Access.User
        {
            LogtoSub = "preflight-mismatch",
            Email = "Teacher@Example.Test",
            Status = QuranDashboard.Domain.Access.UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await using (var scope = Fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET email = 'not-an-email' WHERE id = {0};",
                invalidUserId);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET normalized_email = 'WRONG' WHERE id = {0};",
                mismatchedUserId);
        }

        using var apiScope = Fixture.ApiServices.CreateScope();
        var preflight = apiScope.ServiceProvider.GetRequiredService<IEmailIdentityPreflight>();

        var result = await preflight.ScanAsync(CancellationToken.None);

        result.IsClean.Should().BeFalse();
        result.InvalidUserIds.Should().ContainSingle().Which.Should().Be(invalidUserId);
        result.MismatchedNormalizedEmailUserIds.Should().ContainSingle().Which.Should().Be(mismatchedUserId);
        (await Fixture.GetUserBySubAsync("smoke-read-only"))!.NormalizedEmail
            .Should().Be("SMOKE-READ-ONLY@EXAMPLE.TEST");
    }

    [Fact]
    public async Task BackfillAsync_UsesTheSharedNormalizerAndLeavesACleanScan()
    {
        const string displayEmail = " Teacher@Example.Test ";
        await Fixture.InsertUserAsync(new QuranDashboard.Domain.Access.User
        {
            LogtoSub = "preflight-backfill",
            Email = displayEmail,
            NormalizedEmail = "STALE@EXAMPLE.TEST",
            Status = QuranDashboard.Domain.Access.UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        using var apiScope = Fixture.ApiServices.CreateScope();
        var preflight = apiScope.ServiceProvider.GetRequiredService<IEmailIdentityPreflight>();

        var changed = await preflight.BackfillAsync(CancellationToken.None);

        changed.Should().Be(1);
        (await Fixture.GetUserBySubAsync("preflight-backfill"))!.NormalizedEmail
            .Should().Be("TEACHER@EXAMPLE.TEST");
        (await preflight.ScanAsync(CancellationToken.None)).IsClean.Should().BeTrue();
    }
}
