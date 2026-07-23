using QuranDashboard.Application.Abstractions.Abwab;

using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record LiftManualProtectionCommand(
    Guid CategoryId,
    ManualProtectionType ProtectionType,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
