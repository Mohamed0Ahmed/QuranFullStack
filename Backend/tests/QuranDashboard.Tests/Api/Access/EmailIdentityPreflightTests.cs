using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class EmailIdentityPreflightTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task ScanAsync_HandlesNullNormalizedEmailDuringStagedMigration()
    {
        await fixture.ResetAsync();
        var userId = await fixture.InsertUserAsync(new QuranDashboard.Domain.Access.User
        {
            LogtoSub = "preflight-staged-null",
            Email = "Staged@Example.Test",
            Status = QuranDashboard.Domain.Access.UserStatus.Pending,
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

    [Fact]
    public async Task ScanAsync_ReportsInvalidAndMismatchedIdentityRowsWithoutChangingThem()
    {
        await fixture.ResetAsync();
        var invalidUserId = await fixture.InsertPersonaAsync("ReadOnly");
        var mismatchedUserId = await fixture.InsertUserAsync(new QuranDashboard.Domain.Access.User
        {
            LogtoSub = "preflight-mismatch",
            Email = "Teacher@Example.Test",
            Status = QuranDashboard.Domain.Access.UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await using (var scope = fixture.QueryServices.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET email = 'not-an-email' WHERE id = {0};",
                invalidUserId);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET normalized_email = 'WRONG' WHERE id = {0};",
                mismatchedUserId);
        }

        using var apiScope = fixture.ApiServices.CreateScope();
        var preflight = apiScope.ServiceProvider.GetRequiredService<IEmailIdentityPreflight>();

        var result = await preflight.ScanAsync(CancellationToken.None);

        result.IsClean.Should().BeFalse();
        result.InvalidUserIds.Should().ContainSingle().Which.Should().Be(invalidUserId);
        result.MismatchedNormalizedEmailUserIds.Should().ContainSingle().Which.Should().Be(mismatchedUserId);
        (await fixture.GetUserBySubAsync("smoke-read-only"))!.NormalizedEmail
            .Should().Be("SMOKE-READ-ONLY@EXAMPLE.TEST");
    }

    [Fact]
    public async Task BackfillAsync_UsesTheSharedNormalizerAndLeavesACleanScan()
    {
        await fixture.ResetAsync();
        const string displayEmail = " Teacher@Example.Test ";
        await fixture.InsertUserAsync(new QuranDashboard.Domain.Access.User
        {
            LogtoSub = "preflight-backfill",
            Email = displayEmail,
            NormalizedEmail = "STALE@EXAMPLE.TEST",
            Status = QuranDashboard.Domain.Access.UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        using var apiScope = fixture.ApiServices.CreateScope();
        var preflight = apiScope.ServiceProvider.GetRequiredService<IEmailIdentityPreflight>();

        var changed = await preflight.BackfillAsync(CancellationToken.None);

        changed.Should().Be(1);
        (await fixture.GetUserBySubAsync("preflight-backfill"))!.NormalizedEmail
            .Should().Be("TEACHER@EXAMPLE.TEST");
        (await preflight.ScanAsync(CancellationToken.None)).IsClean.Should().BeTrue();
    }
}
