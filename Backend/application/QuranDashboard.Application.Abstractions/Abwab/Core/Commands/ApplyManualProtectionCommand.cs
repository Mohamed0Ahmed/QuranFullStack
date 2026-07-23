using QuranDashboard.Application.Abstractions.Abwab;

using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record ApplyManualProtectionCommand(
    Guid CategoryId,
    ManualProtectionType ProtectionType,
    ManualProtectionScope Scope,
    uint? ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
