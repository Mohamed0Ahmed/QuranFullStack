using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingConfirmationResultDto(
    int DoorId,
    bool IsNoOp,
    LinkingPreflightCountsDto Totals,
    IReadOnlyList<LinkingConfirmationSourceResultDto> Sources);

public sealed record LinkingConfirmationSourceResultDto(
    string SourceIdentity,
    string Classification,
    long? ContributionId,
    LinkingPreflightCountsDto Counts);
