using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record ActiveManualProtectionRecordDto(
    Guid ManualProtectionId,
    Guid CategoryId,
    ManualProtectionType ProtectionType,
    ManualProtectionScope ProtectionScope,
    uint Version);
