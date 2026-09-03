using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Quran.MushafReader;

[Collection(nameof(MushafReaderCollection))]
public sealed class QuranFidelityOracleTests(MushafReaderTestFixture fixture)
{
    [Fact]
    public async Task Database_ayah_word_and_layout_relationships_match_the_source_reviewed_oracle()
    {
        var oracle = QuranFidelityOracleDocument.ReadOracle();
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var verseKeys = oracle.Ayahs.Select(ayah => ayah.VerseKey).ToArray();

        var ayahs = await db.QuranAyahs
            .AsNoTracking()
            .Where(ayah => verseKeys.Contains(ayah.VerseKey))
            .OrderBy(ayah => ayah.SurahNumber)
            .ThenBy(ayah => ayah.AyahNumber)
            .Select(ayah => new { ayah.VerseKey, ayah.TextUthmani })
            .ToListAsync();
        ayahs.Should().BeEquivalentTo(
            oracle.Ayahs.Select(ayah => new { ayah.VerseKey, ayah.TextUthmani }),
            options => options.WithStrictOrdering());

        var words = await db.QuranWords
            .AsNoTracking()
            .Where(word => word.PageNumber == oracle.PageNumber)
            .OrderBy(word => word.LineNumber)
            .ThenBy(word => word.LineWordOrder)
            .Select(word => new QuranFidelityWord(
                word.Location,
                word.Ayah.VerseKey,
                word.TextUthmani,
                word.IsAyahMarker))
            .ToListAsync();
        words.Should().Equal(oracle.Words);

        var locationsByVerse = words
            .GroupBy(word => word.VerseKey)
            .ToDictionary(group => group.Key, group => group.Select(word => word.Location).ToArray());
        foreach (var ayah in oracle.Ayahs)
        {
            locationsByVerse[ayah.VerseKey].Should().Equal(ayah.WordLocations);
        }

        var databaseLines = await db.QuranMushafLines
            .AsNoTracking()
            .Where(line => line.PageNumber == oracle.PageNumber)
            .OrderBy(line => line.LineNumber)
            .Select(line => new
            {
                line.LineNumber,
                LineType = line.LineType.ToString(),
                line.IsCentered,
                line.SurahNumber,
            })
            .ToListAsync();
        databaseLines.Select(line => new
        {
            line.LineNumber,
            LineType = ToApiLineType(line.LineType),
            line.IsCentered,
            line.SurahNumber,
        }).Should().BeEquivalentTo(
            oracle.Lines.Select(line => new
            {
                line.LineNumber,
                line.LineType,
                line.IsCentered,
                line.SurahNumber,
            }),
            options => options.WithStrictOrdering());

        var tafsir = await (
            from source in db.TafsirSources.AsNoTracking()
            where source.SourceKey == oracle.Study.Tafsir.SourceKey
            join ayahEntry in db.TafsirAyahEntries.AsNoTracking()
                on source.Id equals ayahEntry.SourceId
            join entry in db.TafsirEntries.AsNoTracking()
                on ayahEntry.TafsirEntryId equals entry.Id
            where ayahEntry.VerseKey == oracle.Study.VerseKey
            select new QuranFidelityTafsir(
                source.SourceKey,
                source.DisplayNameAr,
                source.ShortNameAr,
                source.LanguageCode,
                source.Direction,
                source.TafsirKind,
                ayahEntry.SourceValueKind,
                ayahEntry.SourceLeaderVerseKey,
                ayahEntry.IsGroupLeader,
                entry.CoveredAyahCount,
                JsonSerializer.Deserialize<string[]>(entry.CoveredAyahKeys)!,
                entry.TafsirText))
            .SingleAsync();
        tafsir.Should().BeEquivalentTo(oracle.Study.Tafsir, options => options.WithStrictOrdering());

        var translation = await (
            from source in db.TranslationSources.AsNoTracking()
            where source.SourceKey == oracle.Study.Translation.SourceKey
            join entry in db.TranslationAyahEntries.AsNoTracking()
                on source.Id equals entry.SourceId
            where entry.VerseKey == oracle.Study.VerseKey
            select new QuranFidelityTranslation(
                source.SourceKey,
                source.DisplayNameAr,
                source.DisplayNameEn,
                source.LanguageCode,
                source.Direction,
                source.TranslationType,
                source.ContainsHtmlMarkup,
                entry.Text))
            .SingleAsync();
        translation.Should().BeEquivalentTo(oracle.Study.Translation);
    }

    [Fact]
    public async Task Mushaf_page_API_maps_the_exact_oracle()
    {
        var oracle = QuranFidelityOracleDocument.ReadOracle();
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync($"/api/mushaf/pages/{oracle.PageNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        var page = envelope.GetProperty("data").Deserialize<MushafPageResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        page.Should().NotBeNull();
        page!.PageNumber.Should().Be(oracle.PageNumber);
        page.AyahRange.FirstVerseKey.Should().Be(oracle.Ayahs[0].VerseKey);
        page.AyahRange.LastVerseKey.Should().Be(oracle.Ayahs[^1].VerseKey);

        page.Lines.Select(line => new QuranFidelityLine(
            line.LineNumber,
            line.LineType,
            line.IsCentered,
            line.SurahNumber,
            line.Words.Select(word => word.WordLocation).ToArray()))
            .Should().BeEquivalentTo(
                oracle.Lines,
                options => options.WithStrictOrdering());
        page.Lines.SelectMany(line => line.Words)
            .Select(word => new QuranFidelityWord(
                word.WordLocation,
                word.VerseKey,
                word.TextUthmani,
                word.IsAyahMarker))
            .Should().Equal(oracle.Words);

        using var studyResponse = await client.GetAsync(
            $"/api/mushaf/ayahs/{oracle.Study.VerseKey}/study"
            + $"?tafsirSource={oracle.Study.Tafsir.SourceKey}"
            + $"&translationSource={oracle.Study.Translation.SourceKey}");
        studyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var studyEnvelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(studyResponse);
        var study = studyEnvelope.GetProperty("data").Deserialize<AyahStudyResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        study.Should().NotBeNull();
        study!.Ayah.VerseKey.Should().Be(oracle.Study.VerseKey);
        study.SelectedSources.TafsirSource.Should().Be(oracle.Study.Tafsir.SourceKey);
        study.SelectedSources.TranslationSource.Should().Be(oracle.Study.Translation.SourceKey);
        study.Tafsir.Should().BeEquivalentTo(oracle.Study.Tafsir, options => options.WithStrictOrdering());
        study.Translation.Should().BeEquivalentTo(oracle.Study.Translation);

    }

    private static string ToApiLineType(string lineType) => lineType switch
    {
        "Ayah" => "ayah",
        "SurahName" => "surah_name",
        "Basmallah" => "basmallah",
        _ => throw new InvalidOperationException($"Unsupported Mushaf line type '{lineType}'."),
    };
}
