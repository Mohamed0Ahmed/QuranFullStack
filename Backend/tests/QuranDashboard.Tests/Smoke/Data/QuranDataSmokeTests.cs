using QuranDashboard.Tests.Smoke._Fixtures;
using QuranDashboard.Tests.Smoke._Support;

namespace QuranDashboard.Tests.Smoke.Data;

// One real-data read per legacy Quran controller against the restored canonical dump.
// Thresholds are deliberately ">" floors from the documented inventory, not exact counts —
// exact-count verification lives in the fixture's manifest check.
[Collection(nameof(QuranDataSmokeCollection))]
public sealed class QuranDataSmokeTests(QuranDataSmokeFixture fixture)
{
    [QuranDumpFact]
    public void Gate_reports_dump_present() => QuranDumpGate.IsMissing.Should().BeFalse();

    [QuranDumpFact]
    public async Task Mushaf_page_1_has_surah_content()
    {
        var data = await ReadDataAsync("api/mushaf/pages/1");
        data.GetProperty("pageNumber").GetInt32().Should().Be(1);
        data.GetProperty("surahs").GetArrayLength().Should().BeGreaterThan(0);
    }

    [QuranDumpFact]
    public async Task Mushaf_surah_catalog_lists_all_114_surahs()
    {
        var data = await ReadDataAsync("api/mushaf/surahs");
        data.GetProperty("surahs").GetArrayLength().Should().Be(114);
    }

    [QuranDumpFact]
    public async Task Mushaf_study_sources_load()
        => await ReadDataAsync("api/mushaf/study-sources");

    [QuranDumpFact]
    public async Task Ayah_study_loads_for_first_ayah()
        => await ReadDataAsync("api/mushaf/ayahs/1:1/study");

    [QuranDumpFact]
    public async Task Ayah_similarities_load_for_first_ayah()
        => await ReadDataAsync("api/mushaf/ayahs/1:1/similar-ayahs");

    [QuranDumpFact]
    public async Task Ayah_mutashabihat_load()
        => await ReadDataAsync("api/mushaf/ayahs/2:6/mutashabihat");

    [QuranDumpFact]
    public async Task Word_analysis_loads_for_first_word()
        => await ReadDataAsync("api/mushaf/words/1:1:1/analysis");

    [QuranDumpFact]
    public async Task Roots_explorer_pages_the_full_corpus()
    {
        var data = await ReadDataAsync("api/words/roots");
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(1600);
    }

    [QuranDumpFact]
    public async Task Lemmas_explorer_pages_the_full_corpus()
    {
        var data = await ReadDataAsync("api/words/lemmas");
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(4700);
    }

    [QuranDumpFact]
    public async Task Stems_explorer_pages_the_full_corpus()
    {
        var data = await ReadDataAsync("api/words/stems");
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(11000);
    }

    [QuranDumpFact]
    public async Task Unique_words_explorer_pages_the_full_corpus()
    {
        var data = await ReadDataAsync("api/words/unique/tashkeel");
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(21000);
    }

    [QuranDumpFact]
    public async Task Word_types_tree_loads()
        => await ReadDataAsync("api/words/word-types/tree");

    [QuranDumpFact]
    public async Task Word_types_grouped_summary_loads_for_first_root()
        => await ReadDataAsync("api/words/word-types/table/roots/1");

    private async Task<JsonElement> ReadDataAsync(string path)
    {
        using var response = await fixture.Client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK, path);

        using var envelope = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        envelope.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeTrue(path);
        var data = envelope.RootElement.GetProperty("data");
        data.ValueKind.Should().NotBe(JsonValueKind.Null, path);
        return data.Clone();
    }
}
