using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private async Task<IReadOnlyList<LinkingResolvedAyahDto>> ResolveWordTypeAsync(
        LinkingSourceDescriptor.WordType source,
        CancellationToken cancellationToken)
    {
        var matches = source.Selection switch
        {
            LinkingWordTypeSelection.Word selection =>
                await ResolveWordTypeWordAsync(selection, cancellationToken),
            LinkingWordTypeSelection.Dimension selection =>
                await ResolveWordTypeDimensionAsync(selection, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source.Selection.Kind,
                "Unknown word type selection kind."),
        };

        return await HydrateMatchesAsync(matches, includeAyahMarkers: false, cancellationToken);
    }

    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveWordTypeWordAsync(
        LinkingWordTypeSelection.Word selection,
        CancellationToken cancellationToken)
    {
        var wordExists = await _dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .AnyAsync(word => word.Id == selection.TashkeelWordId, cancellationToken);
        if (!wordExists)
        {
            throw NotFound("tashkeelWordId", selection.TashkeelWordId);
        }

        var identity = new WordTypeRowIdentity(
            selection.TashkeelWordId,
            selection.ContextCode,
            selection.Case,
            selection.Tense,
            selection.Voice);

        return await EfWordTypesReader.MatchedMorphologyQuery(_dbContext, identity)
            .Select(morphology => new LinkingMatchedWordRow(
                morphology.QuranWord.AyahId,
                morphology.QuranWord.Id,
                morphology.QuranWord.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveWordTypeDimensionAsync(
        LinkingWordTypeSelection.Dimension selection,
        CancellationToken cancellationToken)
    {
        var kind = ToGroupedDimensionKind(selection.Kind);
        await GuardDimensionExistsAsync(kind, selection.DimensionId, cancellationToken);

        var scope = selection.Scope;
        var context = EfWordTypesReader.ToGroupedReadContext(
            new WordTypeFilter(scope.Type, scope.ChildCode, scope.Case, scope.Tense, scope.Voice));
        var parameters = EfWordTypesReader.BuildGroupedDetailParameters(context, selection.DimensionId);

        var sql = $"""
            WITH base AS (
                {EfWordTypesReader.BaseRowsSql(context, kind)}
            )
            SELECT DISTINCT
                base.ayah_id AS "{nameof(LinkingMatchedWordRow.AyahId)}",
                base.quran_word_id AS "{nameof(LinkingMatchedWordRow.QuranWordId)}",
                base.word_number AS "{nameof(LinkingMatchedWordRow.WordNumber)}"
            FROM base
            """;

        return await _dbContext.Database
            .SqlQueryRaw<LinkingMatchedWordRow>(sql, parameters)
            .ToListAsync(cancellationToken);
    }

    private async Task GuardDimensionExistsAsync(
        WordTypeGroupedDimensionKind kind,
        int dimensionId,
        CancellationToken cancellationToken)
    {
        var exists = kind switch
        {
            WordTypeGroupedDimensionKind.Root => await _dbContext.QuranRoots
                .AsNoTracking()
                .AnyAsync(root => root.Id == dimensionId, cancellationToken),
            WordTypeGroupedDimensionKind.Stem => await _dbContext.QuranStems
                .AsNoTracking()
                .AnyAsync(stem => stem.Id == dimensionId, cancellationToken),
            WordTypeGroupedDimensionKind.Lemma => await _dbContext.QuranLemmas
                .AsNoTracking()
                .AnyAsync(lemma => lemma.Id == dimensionId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown grouped dimension kind."),
        };

        if (!exists)
        {
            throw NotFound(kind.ToDtoKind() + "Id", dimensionId);
        }
    }

    private static WordTypeGroupedDimensionKind ToGroupedDimensionKind(
        LinkingWordTypeSelectionKind kind) => kind switch
    {
        LinkingWordTypeSelectionKind.Root => WordTypeGroupedDimensionKind.Root,
        LinkingWordTypeSelectionKind.Stem => WordTypeGroupedDimensionKind.Stem,
        LinkingWordTypeSelectionKind.Lemma => WordTypeGroupedDimensionKind.Lemma,
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind),
            kind,
            "A word type dimension selection must be root, stem, or lemma."),
    };
}
