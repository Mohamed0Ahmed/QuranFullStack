using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record ManualProtectionResolutionDto(
    ManualProtectionType ProtectionType,
    bool IsProtected,
    bool IsDirect,
    Guid? SourceCategoryId,
    ManualProtectionScope? Scope,
    ProtectionActionClassification ActionClassification,
    DateTimeOffset ServerTimeUtc);
