using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Api.Abwab.Protection;

public sealed record ApplyManualProtectionRequest(ManualProtectionScope Scope, uint? ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record LiftManualProtectionRequest(uint ExpectedVersion, long ExpectedTimelineGeneration);

public sealed record ApplyFullProtectionPresetRequest(
    ManualProtectionScope Scope,
    IReadOnlyDictionary<ManualProtectionType, uint> ExpectedVersions,
    long ExpectedTimelineGeneration);
