using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

[Collection(nameof(MorphologyImportTestCollection))]
public sealed class MorphologyImportSourceNormalizationTests(MorphologyImportTestFixture fixture)
{
    [Fact]
    public async Task LoadAsync_applies_normalization_and_carries_summary_forward()
    {
        await fixture.SeedSyntheticWordsAsync();
        await AddReadableWordAsync("3:33:7", 6, "ت-٦");
        await AddReadableWordAsync("28:50:10", 7, "ت-٧");

        var sourcePath = await fixture.WriteSyntheticSourceFolderAsync();
        await RewriteCorpusAsync(sourcePath, fixture);

        var lemmas = MorphologySyntheticSeed.GetLemmaMap();
        lemmas["3:33:7"] = "ءَال";
        lemmas["28:50:10"] = ">aDal~";
        await fixture.PatchQulLemmaMapAsync(sourcePath, lemmas);

        await using var scope = fixture.CreateServiceProvider(services =>
        {
            services.AddSingleton<IWordLemmaNormalizationReader>(new FakeWordLemmaNormalizationReader());
        }).CreateAsyncScope();

        var importSource = scope.ServiceProvider.GetRequiredService<MorphologyImportSource>();
        var source = await importSource.LoadAsync(sourcePath, CancellationToken.None);

        source.CorrectionSummary.Should().NotBeNull();
        source.CorrectionSummary!.ArtifactSha256.Should().Be("f".PadRight(64, '0'));
        source.CorrectionSummary.RawLemmasSha256.Should().MatchRegex("^[0-9a-f]{64}$");
        source.CorrectionSummary.TotalEntries.Should().Be(2);
        source.CorrectionSummary.AppliedAdd.Should().Be(1);
        source.CorrectionSummary.AppliedReplace.Should().Be(1);
        source.CorrectionSummary.SpotChecks.Should().ContainSingle(check =>
            check.Location == "3:33:7" && check.AppliedLemmaArabic == "إِبْرَاهِيم");

        source.Lemmas["3:33:7"].Should().Be("إِبْرَاهِيم");
        source.Lemmas["28:50:10"].Should().Be("أَضَلّ");
        source.Lemmas["1:1:3"].Should().Be(MorphologySyntheticSeed.LemmaValue2);
    }

    private async Task AddReadableWordAsync(string location, int id, string textUthmani)
    {
        await using var scope = fixture.CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboard.Infrastructure.Persistence.QuranDashboardDbContext>();

        dbContext.QuranWords.Add(new QuranWord
        {
            Id = id,
            AyahId = 1,
            SurahNumber = 1,
            AyahNumber = 1,
            WordNumber = (short)id,
            PageNumber = 1,
            LineNumber = 1,
            LineWordOrder = (short)id,
            Location = location,
            QpcGlyph = textUthmani,
            TextUthmani = textUthmani,
            TextUthmaniSimple = textUthmani,
            TextImlaeiSimple = textUthmani,
            WordKeyImlaeiSimple = textUthmani,
            IsAyahMarker = false
        });

        await dbContext.SaveChangesAsync();
    }

    private async Task RewriteCorpusAsync(string sourcePath, MorphologyImportTestFixture fixture)
    {
        var corpusPath = Path.Combine(sourcePath, "corpus", "quranic-corpus-morphology-qpc-aligned.json");

        var corpus = new Dictionary<string, object>();
        foreach (var word in MorphologySyntheticSeed.ReadableWords)
        {
            corpus[word.Location] = new
            {
                qpcUthmani = word.TextUthmani,
                segments = MorphologySyntheticSeed.GetSegmentsForWord(word.Location)
            };
        }

        corpus["3:33:7"] = new
        {
            qpcUthmani = "ت-٦",
            segments = new[]
            {
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "faA",
                    features = "GEN",
                    root = (string?)null,
                    lemma = "faA"
                }
            }
        };

        corpus["28:50:10"] = new
        {
            qpcUthmani = "ت-٧",
            segments = new[]
            {
                new
                {
                    segmentNumber = (short)1,
                    kind = "STEM",
                    pos = "N",
                    form = "DaL",
                    features = "ACC",
                    root = (string?)null,
                    lemma = "DaL"
                }
            }
        };

        await WriteJsonAsync(corpusPath, corpus);
        await UpdateManifestRecordCountAsync(sourcePath, "corpus/quranic-corpus-morphology-qpc-aligned.json", corpus.Count);
        await fixture.RefreshManifestChecksumsAsync(sourcePath);
    }

    private static async Task UpdateManifestRecordCountAsync(string sourcePath, string relativePath, int recordCount)
    {
        var manifestPath = Path.Combine(sourcePath, "manifest.json");

        await using var stream = File.OpenRead(manifestPath);
        using var document = await JsonDocument.ParseAsync(stream);

        var files = new List<object>();
        foreach (var fileEntry in document.RootElement.GetProperty("files").EnumerateArray())
        {
            var localPath = fileEntry.TryGetProperty("path", out var pathProp)
                ? pathProp.GetString() ?? string.Empty
                : fileEntry.TryGetProperty("relativePath", out var relativeProp)
                    ? relativeProp.GetString() ?? string.Empty
                    : string.Empty;

            files.Add(new
            {
                role = fileEntry.GetProperty("role").GetString(),
                path = localPath,
                originPath = fileEntry.TryGetProperty("originPath", out var originPath) ? originPath.GetString() : null,
                expectedRecordCount = string.Equals(localPath, relativePath, StringComparison.Ordinal)
                    ? (int?)recordCount
                    : fileEntry.TryGetProperty("expectedRecordCount", out var expectedRecordCount) && expectedRecordCount.ValueKind != JsonValueKind.Null
                        ? expectedRecordCount.GetInt32()
                        : null,
                fileSizeBytes = fileEntry.TryGetProperty("fileSizeBytes", out var fileSizeBytes) ? (long?)fileSizeBytes.GetInt64() : null,
                sha256 = fileEntry.TryGetProperty("sha256", out var sha256) ? sha256.GetString() : null,
                notes = fileEntry.TryGetProperty("notes", out var notes) ? notes.GetString() : null
            });
        }

        await WriteJsonAsync(manifestPath, new { files });
    }

    private static async Task WriteJsonAsync(string path, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private sealed class FakeWordLemmaNormalizationReader : IWordLemmaNormalizationReader
    {
        public WordLemmaNormalizationLoaded Load() => new(
            new WordLemmaNormalizationArtifact
            {
                SchemaVersion = WordLemmaNormalizationArtifact.SupportedSchemaVersion,
                ArtifactId = "fake-normalization-artifact",
                Entries = []
            },
            [],
            "f".PadRight(64, '0'),
            new WordLemmaNormalizationCounts(0, 0, 0, 0, 0, 0, 0, 0, 0));

        public WordLemmaNormalizationResult Apply(
            IReadOnlyDictionary<string, string> rawLemmas,
            WordLemmaNormalizationLoaded loaded,
            IReadOnlySet<string>? readableWordLocations = null,
            string? rawLemmasSha256 = null)
        {
            var corrected = new Dictionary<string, string>(rawLemmas, StringComparer.Ordinal)
            {
                ["3:33:7"] = "إِبْرَاهِيم",
                ["28:50:10"] = "أَضَلّ",
            };

            var summary = new WordLemmaCorrectionSummary(
                ArtifactSha256: loaded.ArtifactSha256,
                RawLemmasSha256: rawLemmasSha256,
                TotalEntries: 2,
                AppliedAdd: 1,
                AppliedRemove: 0,
                AppliedReplace: 1,
                ReviewedKeep: 0,
                ReviewedException: 0,
                FailedOrSkipped: 0,
                SpotChecks: new[]
                {
                    new WordLemmaNormalizationSpotCheck("3:33:7", "replace", "إِبْرَاهِيم"),
                    new WordLemmaNormalizationSpotCheck("28:50:10", "add", "أَضَلّ"),
                });

            return new WordLemmaNormalizationResult(corrected, summary);
        }
    }
}
