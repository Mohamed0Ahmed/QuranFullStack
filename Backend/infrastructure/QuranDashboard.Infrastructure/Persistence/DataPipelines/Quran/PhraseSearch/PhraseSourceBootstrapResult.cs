namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSourceBootstrapResult(
    PhraseSourceState State,
    PhraseSourceReadResult Source,
    string ComputedFingerprint,
    string? CleanupWarning);
