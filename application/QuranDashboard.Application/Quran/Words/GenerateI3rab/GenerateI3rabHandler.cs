namespace QuranDashboard.Application.Quran.Words.GenerateI3rab;

public sealed class GenerateI3rabHandler
{
    public Task<GenerateI3rabResult> HandleAsync(GenerateI3rabCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(GenerateI3rabResult.Failure(
            "generate-i3rab orchestration is implemented in Phase 3 (User Story 1).",
            command.ReportOutDir));
    }
}
