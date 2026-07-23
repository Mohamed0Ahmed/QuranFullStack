using QuranDashboard.Application.Abstractions.Abwab;

using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record ApplyFullProtectionPresetCommand(
    Guid CategoryId,
    ManualProtectionScope Scope,
    IReadOnlyDictionary<ManualProtectionType, uint> ExpectedVersions,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
