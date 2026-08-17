using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.Queries.ResolveLinkingSourcePage;

public sealed class ResolveLinkingSourcePageHandler(
    ILinkingSourcePageReader reader,
    ILinkingDataRevisionReadScope revisionScope,
    ILinkingScalabilityPolicy policy)
{
    public async Task<ResolveLinkingSourcePageOutcome> HandleAsync(
        ResolveLinkingSourcePageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invalidField = InvalidField(query);
        if (invalidField is not null)
        {
            return new ResolveLinkingSourcePageOutcome.InvalidRequest(invalidField);
        }

        if (!LinkingSourceDescriptorValidation.TryValidate(query.Descriptor, out _))
        {
            return new ResolveLinkingSourcePageOutcome.InvalidDescriptor(new LinkingDescriptorViolation(
                LinkingDescriptorViolationCode.MalformedDescriptor,
                "descriptor",
                null));
        }

        var normalizedView = query.View with
        {
            AyahOverrideIds = [.. query.View.AyahOverrideIds.Distinct().Order()],
            TypeCodes =
            [
                .. query.View.TypeCodes
                    .Select(typeCode => typeCode.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
            ],
        };
        var resolutionIdentity = LinkingSourceIdentity.For(query.Descriptor);
        var sourceViewIdentity = LinkingSourceViewIdentity.Compute(resolutionIdentity, normalizedView);

        try
        {
            return await revisionScope.ExecuteAsync<ResolveLinkingSourcePageOutcome>(
                policy.MaximumAutomaticAttempts,
                async (revision, token) =>
                {
                    if (query.ExpectedLinkingDataRevision is long expectedRevision
                        && expectedRevision != revision)
                    {
                        return new ResolveLinkingSourcePageOutcome.LinkingDataStale();
                    }

                    if (query.ExpectedSourceViewIdentity is not null
                        && !string.Equals(
                            query.ExpectedSourceViewIdentity,
                            sourceViewIdentity,
                            StringComparison.Ordinal))
                    {
                        return new ResolveLinkingSourcePageOutcome.SourceViewStale();
                    }

                    var page = await reader.ResolvePageAsync(
                        query.Descriptor,
                        revision,
                        sourceViewIdentity,
                        normalizedView,
                        query.Page,
                        query.PageSize,
                        token);
                    return new ResolveLinkingSourcePageOutcome.Success(page);
                },
                cancellationToken);
        }
        catch (LinkingInvalidDescriptorException exception)
        {
            return new ResolveLinkingSourcePageOutcome.InvalidDescriptor(exception.Violation);
        }
        catch (LinkingSourceNotFoundException exception)
        {
            return new ResolveLinkingSourcePageOutcome.NotFound(exception.Reference);
        }
        catch (LinkingPageOutOfRangeException)
        {
            return new ResolveLinkingSourcePageOutcome.InvalidRequest("page");
        }
        catch (LinkingDataRevisionReadRetryExhaustedException)
        {
            return new ResolveLinkingSourcePageOutcome.TransientFailure();
        }
    }

    private string? InvalidField(ResolveLinkingSourcePageQuery query)
    {
        if (query.Page <= 0)
        {
            return "page";
        }

        if (query.PageSize <= 0 || query.PageSize > policy.PageSizeMaximum)
        {
            return "pageSize";
        }

        var hasExpectedRevision = query.ExpectedLinkingDataRevision is > 0;
        var hasExpectedIdentity = !string.IsNullOrWhiteSpace(query.ExpectedSourceViewIdentity);
        if (hasExpectedRevision != hasExpectedIdentity || (!hasExpectedRevision && query.Page != 1))
        {
            return "expectedLinkingDataRevision";
        }

        if (query.View.AyahOverrideIds.Any(id => id <= 0))
        {
            return "view.ayahOverrideIds";
        }

        if (LinkingSourceDescriptorValidation.TypeCodesError(query.View.TypeCodes) is not null
            || (query.View.TypeCodes.Count > 0 && !LinkingSourceTypeFilter.Supports(query.Descriptor)))
        {
            return "view.typeCodes";
        }

        var all = query.View.Segment == LinkingSourcePageSegment.All;
        if ((all && (query.View.InclusionMode is not null || query.View.AyahOverrideIds.Count != 0))
            || (!all && query.View.InclusionMode is null))
        {
            return "view";
        }

        return null;
    }
}
