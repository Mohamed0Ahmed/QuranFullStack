namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabRuleSeedRow(
    string SignatureKey,
    string RuleFamily,
    string I3rabArabic,
    string DefaultStatus,
    string? Description,
    short SortOrder);
