using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class TimelineBoundarySeedTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExactlyOneImmutableGenerationZeroRoot_IsSeeded()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);

        var boundaries = await context.AbwabTimelineGenerationBoundaries.AsNoTracking().ToListAsync();

        boundaries.Should().ContainSingle();
        boundaries[0].Generation.Should().Be(0);
        boundaries[0].IsRoot.Should().BeTrue();
    }

    [Fact]
    public async Task RootEdit_IsRejected()
    {
        await AssertRawSqlThrowsAsync(
            "UPDATE abwab_timeline_generation_boundaries SET reason = 'tamper' WHERE generation = 0");
    }

    [Fact]
    public async Task RootDelete_IsRejected()
    {
        await AssertRawSqlThrowsAsync("DELETE FROM abwab_timeline_generation_boundaries WHERE generation = 0");
    }

    [Fact]
    public async Task DuplicateRoot_IsRejected()
    {
        await AssertRawSqlThrowsAsync(
            "INSERT INTO abwab_timeline_generation_boundaries (generation, is_root, created_at, reason) "
            + "VALUES (1, true, now(), 'duplicate-root')");
    }

    private async Task AssertRawSqlThrowsAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        var act = () => AbwabKernelHarness.ExecuteAsync(connection, sql);

        await act.Should().ThrowAsync<PostgresException>();
    }
}
