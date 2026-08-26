using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityGroups;

public sealed class GetPhraseSimilarityGroupsHandler(IPhraseSimilarityReader reader)
{
    public async Task<PhraseReadOutcome<PhraseSimilarityGroupsResponse>> HandleAsync(
        GetPhraseSimilarityGroupsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var modeValue = string.IsNullOrWhiteSpace(query.Mode)
            ? PhraseTextModeKeys.Simple
            : query.Mode;
        if (!PhraseTextModeContract.TryParse(modeValue, out var mode))
        {
            return new PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Invalid(
                PhraseRequestInvalidKind.Mode);
        }

        var wordCount = query.WordCount ?? PhraseSimilarityContract.DefaultGlobalLength;
        if (wordCount is < PhraseSimilarityContract.MinimumGlobalLength
            or > PhraseSearchPaging.MaximumSourceLength)
        {
            return new PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Invalid(
                PhraseRequestInvalidKind.Length);
        }

        var threshold = query.Threshold ?? PhraseSimilarityContract.DefaultThreshold;
        if (!PhraseSimilarityContract.IsPresetThreshold(threshold))
        {
            return new PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Invalid(
                PhraseRequestInvalidKind.Threshold);
        }

        if (!PhraseSimilarityRequestValidation.TryPaging(
                query.Page,
                query.PageSize,
                out var page,
                out var pageSize))
        {
            return new PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Invalid(
                PhraseRequestInvalidKind.Paging);
        }

        var result = await reader.GetGroupsAsync(
            mode,
            checked((short)wordCount),
            checked((short)threshold),
            page,
            pageSize,
            cancellationToken);
        return result switch
        {
            PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Success success =>
                new PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Success(success.Value),
            PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Unavailable =>
                new PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Unavailable(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseSearchReadResult<PhraseSimilarityGroupsResponse>)} variant."),
        };
    }
}
