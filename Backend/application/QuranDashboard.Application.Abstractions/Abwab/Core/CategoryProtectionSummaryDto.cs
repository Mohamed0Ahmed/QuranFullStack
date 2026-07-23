namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// Full manual-protection detail requires protection.view; without it only the two generic flags are
// exposed and ManualProtections is null — enforced by AbwabCompositeReadRedactor, never by the reader.
public sealed record CategoryProtectionSummaryDto(
    bool IsActionBlocked,
    bool HasEffectiveManualProtection,
    IReadOnlyList<ManualProtectionResolutionDto>? ManualProtections);
