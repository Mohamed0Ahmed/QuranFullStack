using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

// Streams the Dashboard-ready enriched morphology artifact (one JSON array element per readable Quran
// word) and yields EnrichedMorphologyRecord values. Uses DeserializeAsyncEnumerable so the 96 MB file
// is consumed record-by-record instead of being buffered whole into memory. Audit-only fields present
// on the JSON (corpusPresent, provenance, *MappingStatus, *QulCanonical, stemBuckwalter,
// quranWordIdVerifiedAgainstDashboard, textUthmani/textImlaei/textUthmaniSimple, boundaryAyah,
// boundaryHandling) are read into the record model for validation but are deliberately NOT carried
// onto AlignedWordDto/AlignedSegmentDto — those have no members for them and the writer cannot persist
// them. The EnrichedDimensionBuilder / EnrichedMorphologyImportSource project only schema-bound fields.
public sealed class EnrichedMorphologyReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async IAsyncEnumerable<EnrichedMorphologyRecord> ReadAsync(
        string fullPath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Enriched morphology source file not found: {fullPath}", fullPath);
        }

        await using var stream = File.OpenRead(fullPath);
        await foreach (var record in JsonSerializer.DeserializeAsyncEnumerable<EnrichedMorphologyRecord>(
                           stream, JsonOptions, ct).WithCancellation(ct))
        {
            if (record is null)
            {
                throw new InvalidDataException(
                    $"Enriched morphology source '{fullPath}' contained a null record entry.");
            }

            yield return record;
        }
    }
}

public sealed record EnrichedMorphologyRecord
{
    public string? Location { get; init; }
    public int? Surah { get; init; }
    public int? Ayah { get; init; }
    public int? WordNumber { get; init; }
    public int? QuranWordId { get; init; }

    // textUthmani/textImlaei/textUthmaniSimple are foundation-owned; read here only for the
    // whole-word render agreement statistic. Never re-imported, never persisted by this pathway.
    public string? TextUthmani { get; init; }

    public bool CorpusPresent { get; init; }

    public bool QuranWordIdVerifiedAgainstDashboard { get; init; }

    public IReadOnlyList<EnrichedMorphologySegment> Segments { get; init; } = [];
}

public sealed record EnrichedMorphologySegment
{
    public short SegmentNumber { get; init; }
    public string? Kind { get; init; }
    public string? Pos { get; init; }
    public string? FormBuckwalter { get; init; }
    public string? FormArabic { get; init; }
    public string? FeaturesRaw { get; init; }
    public IReadOnlyList<string>? Features { get; init; }
    public string? LemmaBuckwalter { get; init; }
    public string? LemmaArabic { get; init; }
    public string? RootBuckwalter { get; init; }
    public string? RootArabic { get; init; }
    public string? StemBuckwalter { get; init; }
    public string? StemArabic { get; init; }
}
