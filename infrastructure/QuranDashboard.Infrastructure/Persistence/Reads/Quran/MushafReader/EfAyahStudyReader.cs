using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

/// <summary>
/// EF read implementation for one ayah's study context: core identity plus the
/// three selected source kinds loaded together. HTML is returned unmodified;
/// sanitization happens on the frontend at render time.
/// </summary>
public sealed class EfAyahStudyReader(QuranDashboardDbContext db) : IAyahStudyReader
{
    private static readonly JsonSerializerOptions CoveredKeysJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AyahStudyResponse?> GetAyahStudyAsync(
        string verseKey,
        string? tafsirSourceKey,
        string? translationSourceKey,
        string? fullI3rabSourceKey,
        CancellationToken ct)
    {
        var ayah = await db.QuranAyahs
            .AsNoTracking()
            .Include(a => a.Surah)
            .FirstOrDefaultAsync(a => a.VerseKey == verseKey, ct);

        if (ayah is null)
        {
            return null;
        }

        var sajda = await db.QuranSajdas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.AyahId == ayah.Id, ct);

        var ayahCore = MapAyahCore(ayah, sajda);
        var effectiveTafsirKey = await ResolveTafsirSourceKeyAsync(tafsirSourceKey, ct);
        var effectiveTranslationKey = await ResolveTranslationSourceKeyAsync(translationSourceKey, ct);
        var effectiveFullI3rabKey = await ResolveFullI3rabSourceKeyAsync(fullI3rabSourceKey, ct);

        var tafsir = effectiveTafsirKey is not null
            ? await LoadTafsirAsync(ayah.Id, effectiveTafsirKey, ct)
            : null;
        var translation = effectiveTranslationKey is not null
            ? await LoadTranslationAsync(ayah.Id, effectiveTranslationKey, ct)
            : null;
        var fullI3rab = effectiveFullI3rabKey is not null
            ? await LoadFullI3rabAsync(ayah.Id, effectiveFullI3rabKey, ct)
            : null;
        var similaritySummary = await LoadSimilaritySummaryAsync(ayah.Id, ct);

        return new AyahStudyResponse(
            ayahCore,
            new SelectedSourcesDto(effectiveTafsirKey, effectiveTranslationKey, effectiveFullI3rabKey),
            tafsir,
            translation,
            fullI3rab,
            similaritySummary);
    }

    private async Task<SimilaritySummaryDto> LoadSimilaritySummaryAsync(int ayahId, CancellationToken ct)
    {
        var similarAyahCount = await CountDistinctSimilarAyahsAsync(ayahId, ct);
        var (groupCount, occurrenceCount) = await CountMutashabihatAsync(ayahId, ct);

        return new SimilaritySummaryDto(similarAyahCount, groupCount, occurrenceCount);
    }

    private async Task<int> CountDistinctSimilarAyahsAsync(int ayahId, CancellationToken ct)
    {
        var outgoing = db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.SourceAyahId == ayahId)
            .Select(link => link.TargetAyahId);

        var incoming = db.SimilarAyahLinks
            .AsNoTracking()
            .Where(link => link.TargetAyahId == ayahId)
            .Select(link => link.SourceAyahId);

        return await outgoing.Union(incoming).Distinct().CountAsync(ct);
    }

    private async Task<(int GroupCount, int OccurrenceCount)> CountMutashabihatAsync(
        int ayahId,
        CancellationToken ct)
    {
        var groupIds = db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => occurrence.AyahId == ayahId)
            .Select(occurrence => occurrence.GroupId)
            .Distinct();

        var groupCount = await groupIds.CountAsync(ct);
        if (groupCount == 0)
        {
            return (0, 0);
        }

        var occurrenceCount = await db.MutashabihatOccurrences
            .AsNoTracking()
            .Where(occurrence => groupIds.Contains(occurrence.GroupId))
            .CountAsync(ct);

        return (groupCount, occurrenceCount);
    }

    private async Task<string?> ResolveTafsirSourceKeyAsync(string? requestedKey, CancellationToken ct) =>
        await ResolveSourceKeyAsync(requestedKey, db.TafsirSources.AsNoTracking().Select(s => s.SourceKey), ct);

    private async Task<string?> ResolveTranslationSourceKeyAsync(string? requestedKey, CancellationToken ct) =>
        await ResolveSourceKeyAsync(requestedKey, db.TranslationSources.AsNoTracking().Select(s => s.SourceKey), ct);

    private async Task<string?> ResolveFullI3rabSourceKeyAsync(string? requestedKey, CancellationToken ct) =>
        await ResolveSourceKeyAsync(requestedKey, db.FullI3rabSources.AsNoTracking().Select(s => s.SourceKey), ct);

    private static async Task<string?> ResolveSourceKeyAsync(
        string? requestedKey,
        IQueryable<string> sourceKeys,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestedKey))
        {
            return null;
        }

        var exists = await sourceKeys.AnyAsync(key => key == requestedKey, ct);
        return exists ? requestedKey : null;
    }

    private static AyahCoreDto MapAyahCore(Ayah ayah, Sajda? sajda) => new(
        ayah.VerseKey,
        ayah.SurahNumber,
        ayah.Surah.NameArabic,
        ayah.AyahNumber,
        ayah.TextUthmani,
        ayah.WordsCountReal,
        ayah.PageFrom,
        ayah.PageTo,
        ayah.JuzNumber ?? 0,
        ayah.HizbNumber ?? 0,
        ayah.RubNumber ?? 0,
        sajda is null
            ? null
            : new SajdaDto(
                sajda.SajdahNumber,
                sajda.VerseKey,
                MapSajdahType(sajda.SajdahType)));

    private async Task<TafsirEntryDto?> LoadTafsirAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var source = await db.TafsirSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceKey == sourceKey, ct);

        if (source is null)
        {
            return null;
        }

        var ayahEntry = await db.TafsirAyahEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SourceId == source.Id && e.AyahId == ayahId, ct);

        if (ayahEntry is null)
        {
            return null;
        }

        var entry = await db.TafsirEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ayahEntry.TafsirEntryId, ct);

        if (entry is null)
        {
            return null;
        }

        return new TafsirEntryDto(
            source.SourceKey,
            source.DisplayNameAr,
            string.IsNullOrWhiteSpace(source.ShortNameAr) ? null : source.ShortNameAr,
            source.LanguageCode,
            source.Direction,
            source.TafsirKind,
            ayahEntry.SourceValueKind,
            ayahEntry.SourceLeaderVerseKey,
            ayahEntry.IsGroupLeader,
            entry.CoveredAyahCount,
            ParseCoveredAyahKeys(entry.CoveredAyahKeys),
            entry.TafsirText);
    }

    private async Task<TranslationEntryDto?> LoadTranslationAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var source = await db.TranslationSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceKey == sourceKey, ct);

        if (source is null)
        {
            return null;
        }

        var ayahEntry = await db.TranslationAyahEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SourceId == source.Id && e.AyahId == ayahId, ct);

        if (ayahEntry is null)
        {
            return null;
        }

        return new TranslationEntryDto(
            source.SourceKey,
            string.IsNullOrWhiteSpace(source.DisplayNameAr) ? null : source.DisplayNameAr,
            string.IsNullOrWhiteSpace(source.DisplayNameEn) ? null : source.DisplayNameEn,
            source.LanguageCode,
            source.Direction,
            source.TranslationType,
            source.ContainsHtmlMarkup,
            ayahEntry.Text);
    }

    private async Task<FullI3rabEntryDto?> LoadFullI3rabAsync(int ayahId, string sourceKey, CancellationToken ct)
    {
        var source = await db.FullI3rabSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SourceKey == sourceKey, ct);

        if (source is null)
        {
            return null;
        }

        var ayahEntry = await db.FullI3rabAyahEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SourceId == source.Id && e.AyahId == ayahId, ct);

        if (ayahEntry is null)
        {
            return null;
        }

        var entry = await db.FullI3rabEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ayahEntry.EntryId, ct);

        if (entry is null)
        {
            return null;
        }

        return new FullI3rabEntryDto(
            source.SourceKey,
            source.DisplayNameAr,
            string.IsNullOrWhiteSpace(source.ShortNameAr) ? null : source.ShortNameAr,
            source.MarkupFormat,
            ayahEntry.SourceValueKind,
            ayahEntry.SourceLeaderVerseKey,
            ayahEntry.IsGroupLeader,
            entry.CoveredAyahCount,
            ParseCoveredAyahKeys(entry.CoveredAyahKeys),
            entry.I3rabHtml);
    }

    private static IReadOnlyList<string> ParseCoveredAyahKeys(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, CoveredKeysJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string MapSajdahType(SajdahType sajdahType) => sajdahType switch
    {
        SajdahType.Required => "required",
        SajdahType.Optional => "optional",
        _ => "required",
    };
}
