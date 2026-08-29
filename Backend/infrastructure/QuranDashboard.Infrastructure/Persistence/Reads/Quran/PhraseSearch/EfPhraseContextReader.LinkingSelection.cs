using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    public async Task<PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>> GetLinkingSelectionAsync(
        PhraseContextSelection selection,
        PhraseContextLinkingSelection ayahSelection,
        CancellationToken cancellationToken)
    {
        if (ayahSelection.AyahIds.Any(ayahId => ayahId <= 0)
            || ayahSelection.AyahIds.Distinct().Count() != ayahSelection.AyahIds.Count)
        {
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidSelection();
        }

        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.BuildChanged();
        }

        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            selection.Resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidReference();
        }

        var loaded = await ReadAllFilteredOccurrencesAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            cancellationToken);
        var rowsByAyah = loaded.Items
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());
        if (ayahSelection.AyahIds.Any(ayahId => !rowsByAyah.ContainsKey(ayahId)))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidSelection();
        }

        var selectedRowsByAyah = ayahSelection.Mode switch
        {
            PhraseContextAyahSelectionMode.Only => rowsByAyah
                .Where(pair => ayahSelection.AyahIds.Contains(pair.Key))
                .ToList(),
            PhraseContextAyahSelectionMode.AllExcept => rowsByAyah
                .Where(pair => !ayahSelection.AyahIds.Contains(pair.Key))
                .ToList(),
            _ => [],
        };
        if (selectedRowsByAyah.Count == 0)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.InvalidSelection();
        }

        var selectedRows = selectedRowsByAyah
            .SelectMany(pair => pair.Value)
            .ToList();
        var occurrences = await LoadContextOccurrencesAsync(
            selectedRows,
            selection.Resolution,
            cancellationToken);
        var ayahs = selectedRowsByAyah
            .OrderBy(pair => pair.Value[0].SurahNumber)
            .ThenBy(pair => pair.Value[0].AyahNumber)
            .Select(pair => CreateLinkingSelectionAyah(
                pair.Value.Select(row => occurrences[row.OccurrenceId]).ToList(),
                selection))
            .ToList();
        var response = new PhraseContextLinkingSelectionResponse(
            snapshot.ActiveBuildId,
            ayahs.Count,
            ayahs);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseContextLinkingSelectionResponse>.Success(response);
    }

    private static PhraseContextLinkingSelectionAyahDto CreateLinkingSelectionAyah(
        IReadOnlyList<ContextOccurrence> occurrences,
        PhraseContextSelection selection)
    {
        var ayah = CreateContextAyah(occurrences, selection);
        var first = occurrences[0];
        var selectedWordIds = ayah.Highlights.QueryQuranWordIds
            .Concat(ayah.Highlights.PreviousQuranWordIds)
            .Concat(ayah.Highlights.FollowingQuranWordIds)
            .ToHashSet();
        var canonicalWordIds = first.Words
            .Where(word => selectedWordIds.Contains(word.QuranWordId))
            .Select(word => word.QuranWordId)
            .ToList();
        if (canonicalWordIds.Count != selectedWordIds.Count
            || canonicalWordIds.Any(wordId => wordId <= 0))
        {
            throw new InvalidDataException("PhraseSearch linking selection contains an invalid Quran word.");
        }

        return new PhraseContextLinkingSelectionAyahDto(
            ayah.AyahId,
            ayah.VerseKey,
            first.Row.PageFrom,
            canonicalWordIds);
    }
}
