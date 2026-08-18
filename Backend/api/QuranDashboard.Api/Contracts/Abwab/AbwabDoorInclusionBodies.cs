namespace QuranDashboard.Api.Contracts.Abwab;

public sealed record AddAbwabDoorInclusionsBody(
    uint ExpectedTargetDoorVersion,
    IReadOnlyList<int> SourceDoorIds);

public sealed record DeleteAbwabDoorInclusionBody(uint ExpectedTargetDoorVersion);
