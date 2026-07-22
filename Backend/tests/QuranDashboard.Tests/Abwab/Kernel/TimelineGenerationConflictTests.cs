using QuranDashboard.Api.Abwab;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-017 / SC-003 (T028): a stale ExpectedTimelineGeneration fails with the exact 409
// (abwab.timeline_generation_stale) BEFORE any row mutation, mapped through the ApiResponse envelope.
[Collection(nameof(AbwabDbCollection))]
public sealed class TimelineGenerationConflictTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StaleGeneration_Fails_BeforeAnyRowMutation()
    {
        // Simulate a generation advance elsewhere on the timeline. The command's own target row/revision is
        // untouched, yet the advance still invalidates it.
        await AdvanceGenerationAsync();

        var act = () => AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0));

        var thrown = (await act.Should().ThrowAsync<AbwabTimelineGenerationStaleException>()).Which;
        thrown.Code.Should().Be(AbwabConflictCodes.TimelineGenerationStale);

        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        (await context.AbwabChangeSets.CountAsync()).Should().Be(0, "no row may be mutated before the 409");
        (await context.AbwabAuditEvents.CountAsync()).Should().Be(0);
        (await AbwabKernelHarness.ReadAuditHeadAsync(fixture)).Should().Be(0);
    }

    [Fact]
    public void StaleException_MapsTo_Exact409Envelope()
    {
        var exception = new AbwabTimelineGenerationStaleException(expectedGeneration: 0, currentGeneration: 1);

        var handled = AbwabConflictResponses.TryMap(exception, out var statusCode, out var response);

        handled.Should().BeTrue();
        statusCode.Should().Be(409);
        response.IsSuccess.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Should().Be(AbwabConflictCodes.TimelineGenerationStale);
    }

    private async Task AdvanceGenerationAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await AbwabKernelHarness.ExecuteAsync(
            connection,
            "UPDATE abwab_revision_state SET timeline_generation = 1 WHERE id = 1");
    }
}
