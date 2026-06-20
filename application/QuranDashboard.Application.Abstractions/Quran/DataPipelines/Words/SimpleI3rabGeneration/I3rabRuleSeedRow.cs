namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public sealed record I3rabRuleSeedRow(
    string SignatureKey,
    string RuleFamily,
    string I3rabArabic,
    string DefaultStatus,
    string? Description,
    short SortOrder);
