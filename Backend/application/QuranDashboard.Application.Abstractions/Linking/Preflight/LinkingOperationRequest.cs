using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingOperationRequest(
    int DoorId,
    string? PreflightToken,
    Guid? IdempotencyKey,
    IReadOnlyList<LinkingOperationSourceRequest> Sources);

public sealed record LinkingOperationSourceRequest(
    LinkingSourceDescriptor Descriptor,
    LinkingContributionMode ContributionMode,
    bool? AutomaticWordMatchesEnabled,
    int OrderValue,
    long? ExistingContributionId,
    uint? ExistingContributionVersion,
    IReadOnlyList<LinkingOperationUnitRequest> Units);

public sealed record LinkingOperationUnitRequest(IReadOnlyList<LinkingOperationAyahRequest> Ayahs);

public sealed record LinkingOperationAyahRequest(
    int AyahId,
    IReadOnlyList<int> SelectedWordIds,
    IReadOnlyList<string> Descriptions);
