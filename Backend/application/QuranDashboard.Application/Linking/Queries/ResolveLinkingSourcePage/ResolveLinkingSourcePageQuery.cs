using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.Queries.ResolveLinkingSourcePage;

public sealed record ResolveLinkingSourcePageQuery(
    LinkingSourceDescriptor Descriptor,
    long? ExpectedLinkingDataRevision,
    string? ExpectedSourceViewIdentity,
    LinkingSourcePageView View,
    int Page,
    int PageSize);
