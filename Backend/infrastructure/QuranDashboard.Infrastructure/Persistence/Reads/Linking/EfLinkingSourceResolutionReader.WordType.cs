using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveWordTypeAsync(
        LinkingSourceDescriptor.WordType source,
        CancellationToken cancellationToken)
    {
        return source.Selection switch
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
    }

    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveWordTypeWordAsync(
        LinkingWordTypeSelection.Word selection,
        CancellationToken cancellationToken)
    {
        var identity = WordTypeRowIdentity.Create(
            selection.TashkeelWordId,
            selection.ContextCode,
            selection.Case,
            selection.Tense,
            selection.Voice)
            ?? throw InvalidPersistedWordTypeData("word identity");

        var wordExists = await _dbContext.QuranWordsUniqueTashkeel
            .AsNoTracking()
            .AnyAsync(word => word.Id == identity.TashkeelWordId, cancellationToken);
        if (!wordExists)
        {
            throw NotFound("tashkeelWordId", identity.TashkeelWordId);
        }

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
        var persistedScope = selection.Scope;
        var scope = WordTypeScope.Create(
            persistedScope.Type,
            persistedScope.ChildCode,
            persistedScope.Case,
            persistedScope.Tense,
            persistedScope.Voice)
            ?? throw InvalidPersistedWordTypeData("dimension scope");
        var groupedSelection = WordTypeGroupedSelection.Create(kind, selection.DimensionId, scope)
            ?? throw InvalidPersistedWordTypeData("dimension selection");

        await GuardDimensionExistsAsync(groupedSelection.Kind, groupedSelection.DimensionId, cancellationToken);

        var context = EfWordTypesReader.ToGroupedReadContext(groupedSelection.Scope);
        var parameters = EfWordTypesReader.BuildGroupedDetailParameters(context, groupedSelection.DimensionId);

        var sql = $"""
            WITH base AS (
                {EfWordTypesReader.BaseRowsSql(context, groupedSelection.Kind)}
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
        var exists = kind.RouteKey switch
        {
            "roots" => await _dbContext.QuranRoots
                .AsNoTracking()
                .AnyAsync(root => root.Id == dimensionId, cancellationToken),
            "stems" => await _dbContext.QuranStems
                .AsNoTracking()
                .AnyAsync(stem => stem.Id == dimensionId, cancellationToken),
            "lemmas" => await _dbContext.QuranLemmas
                .AsNoTracking()
                .AnyAsync(lemma => lemma.Id == dimensionId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown grouped dimension kind."),
        };

        if (!exists)
        {
            throw NotFound(kind.DtoKind + "Id", dimensionId);
        }
    }

    private static WordTypeGroupedDimensionKind ToGroupedDimensionKind(
        LinkingWordTypeSelectionKind kind)
    {
        var routeKey = kind switch
        {
            LinkingWordTypeSelectionKind.Root => "roots",
            LinkingWordTypeSelectionKind.Stem => "stems",
            LinkingWordTypeSelectionKind.Lemma => "lemmas",
            _ => throw InvalidPersistedWordTypeData("dimension kind"),
        };

        return WordTypeGroupedDimensionKind.Create(routeKey)
            ?? throw InvalidPersistedWordTypeData("dimension kind");
    }

    private static InvalidOperationException InvalidPersistedWordTypeData(string component) =>
        new($"Persisted linking word type {component} is invalid.");
}
