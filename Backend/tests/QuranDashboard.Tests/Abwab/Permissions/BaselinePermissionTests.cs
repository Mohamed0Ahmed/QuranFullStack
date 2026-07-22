using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Permissions;

// FR-031 / SC-007 (T053): the attribution.view DashboardAdminBaseline is identical across layers and cannot
// be removed. Baseline metadata (catalogue + seed), the always-on effective projection for Admin, and the
// removal-rejection are all asserted (real PG).
[Collection(nameof(AbwabDbCollection))]
public sealed class BaselinePermissionTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const string Baseline = PermissionCatalogue.AttributionView;

    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void Baseline_MetadataIsConsistent_InTheCatalogue()
    {
        PermissionCatalogue.IsBaseline(Baseline).Should().BeTrue();
        PermissionCatalogue.BaselineCodes.Should().ContainSingle().Which.Should().Be(Baseline);
        PermissionCatalogue.IsSystemOwnerOnly(Baseline).Should().BeFalse();
    }

    [Fact]
    public async Task Baseline_IsSeededAsDashboardAdminBaseline()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT dashboard_admin_baseline FROM permission_codes WHERE code = @c";
        command.Parameters.AddWithValue("c", Baseline);

        (await command.ExecuteScalarAsync()).Should().Be(true);
    }

    [Fact]
    public async Task Baseline_IsAlwaysEffectiveForAdmin_WithoutAnAssignment()
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);
        var resolver = SecurityTestHarness.BuildResolver(db, new SecurityTestHarness.RecordingCache());

        var admin = await resolver.ResolveAsync("some-admin", "Admin", CancellationToken.None);
        admin.Should().Contain(Baseline);

        var editor = await resolver.ResolveAsync("some-editor", "Editor", CancellationToken.None);
        editor.Should().NotContain(Baseline);
    }

    [Fact]
    public async Task Baseline_RemovalIsRejected()
    {
        var act = () => SecurityTestHarness.RevokeAsync(fixture, new RevokePermissionCommand(
            PermissionTargetKind.Role, "Admin", Baseline, ExpectedTimelineGeneration.Of(0), 0, "actor"));

        (await act.Should().ThrowAsync<PermissionBaselineLockedException>())
            .Which.Code.Should().Be(AbwabConflictCodes.PermissionBaselineLocked);
    }
}
