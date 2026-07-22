using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

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

public sealed class EnrichedMorphologyRecord
{
    public string? Location { get; init; }
    public int? Surah { get; init; }
    public int? Ayah { get; init; }
    public int? WordNumber { get; init; }
    public int? QuranWordId { get; init; }

    public string? TextUthmani { get; init; }

    public bool CorpusPresent { get; init; }

    public bool QuranWordIdVerifiedAgainstDashboard { get; init; }

    public IReadOnlyList<EnrichedMorphologySegment> Segments { get; init; } = [];
}

public sealed class EnrichedMorphologySegment
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
