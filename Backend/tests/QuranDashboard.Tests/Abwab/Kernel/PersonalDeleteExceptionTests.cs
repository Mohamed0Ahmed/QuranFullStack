using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class PersonalDeleteExceptionTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await AbwabKernelHarness.ResetKernelStateAsync(fixture);
        await AbwabKernelHarness.EnsureFixtureTableAsync(fixture);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void DefaultPolicy_IsDefaultDeny()
    {
        AbwabPersonalDeletePolicy.Default.AllowsPhysicalDelete(typeof(KernelFixtureWriteTarget)).Should().BeFalse();
    }

    [Fact]
    public async Task PhysicalDelete_IsRejected_UnderDefaultDeny()
    {
        var id = await SeedFixtureRowAsync();

        await using var context = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        context.Fixtures.Remove((await context.Fixtures.FindAsync(id))!);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<AbwabPhysicalDeleteRejectedException>();
    }

    [Fact]
    public async Task PhysicalDelete_IsPermitted_WhenTheSealedExceptionAllowsTheType()
    {
        var id = await SeedFixtureRowAsync();
        var policy = new AbwabPersonalDeletePolicy([typeof(KernelFixtureWriteTarget)]);

        await using (var context = AbwabKernelHarness.CreateGuardedContext(fixture, policy))
        {
            context.Fixtures.Remove((await context.Fixtures.FindAsync(id))!);
            await context.SaveChangesAsync();
        }

        await using var verify = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        (await verify.Fixtures.FindAsync(id)).Should().BeNull();
    }

    private async Task<Guid> SeedFixtureRowAsync()
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO {AbwabKernelHarness.FixtureTable} (id, label, is_deleted) VALUES (@id, 'seed', false)";
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
        return id;
    }
}
