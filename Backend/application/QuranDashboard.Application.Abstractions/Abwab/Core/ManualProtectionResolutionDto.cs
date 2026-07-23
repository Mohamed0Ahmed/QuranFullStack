using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

// ManualProtectionId/Version are the direct record's OWN identity + concurrency token (xmin) — only
// populated when IsDirect is true (the record lives AT this category); an inherited resolution names
// the source ancestor but not a record this category's caller can lift/re-scope directly. This DTO is
// protection metadata: both fields are gated by protection.view exactly like type/scope/actor
// (AbwabCompositeReadRedactor nulls the whole ManualProtections list without it).
public sealed record ManualProtectionResolutionDto(
    ManualProtectionType ProtectionType,
    bool IsProtected,
    bool IsDirect,
    Guid? SourceCategoryId,
    ManualProtectionScope? Scope,
    ProtectionActionClassification ActionClassification,
    DateTimeOffset ServerTimeUtc,
    Guid? ManualProtectionId,
    uint? Version);
