using System.Text;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Enriched;

namespace QuranDashboard.Tests.Quran.WordsMorphologyEnriched;

public sealed class EnrichedMorphologyManifestReaderTests : IDisposable
{
    private readonly string tempRoot;
    private readonly EnrichedMorphologyManifestReader reader = new();

    public EnrichedMorphologyManifestReaderTests()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "enriched-morph-manifest-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* test cleanup best-effort */ }
    }

    [Fact]
    public async Task Reads_manifest_and_validates_sha256_size_record_and_segment_counts()
    {
        var (manifest, _) = await WriteValidArtifactAsync(recordCount: 2, segmentCount: 3);

        var result = await reader.ReadAsync(tempRoot, CancellationToken.None);

        result.FullPath.Should().Be(manifest.ArtifactPath);
        result.Sha256.Should().Be(manifest.Sha256);
        result.SizeBytes.Should().Be(manifest.SizeBytes);
        result.RecordCount.Should().Be(2);
        result.SegmentCount.Should().Be(3);
        result.QuranWordIdVerifiedAgainstDashboard.Should().BeTrue();
    }

    [Fact]
    public async Task Throws_on_checksum_mismatch()
    {
        await WriteArtifactWithManifestAsync(
            recordCount: 1, segmentCount: 1, sha256Override: "0000000000000000000000000000000000000000000000000000000000000000");

        var act = async () => await reader.ReadAsync(tempRoot, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*Checksum mismatch*");
    }

    [Fact]
    public async Task Throws_on_size_mismatch()
    {
        await WriteArtifactWithManifestAsync(recordCount: 1, segmentCount: 1, sizeOverride: long.MaxValue);

        var act = async () => await reader.ReadAsync(tempRoot, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*File size mismatch*");
    }

    [Fact]
    public async Task Throws_on_record_count_mismatch()
    {
        var (manifest, _) = await WriteValidArtifactAsync(recordCount: 2, segmentCount: 3);

        var act = async () => await reader.ValidateRecordAndSegmentCountsAsync(
            manifest.ArtifactPath, expectedRecordCount: 99, expectedSegmentCount: 3, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*record count mismatch*");
    }

    [Fact]
    public async Task Throws_on_segment_count_mismatch()
    {
        var (manifest, _) = await WriteValidArtifactAsync(recordCount: 2, segmentCount: 3);

        var act = async () => await reader.ValidateRecordAndSegmentCountsAsync(
            manifest.ArtifactPath, expectedRecordCount: 2, expectedSegmentCount: 999, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*segment count mismatch*");
    }

    [Fact]
    public async Task Capture_and_verify_digest_detects_changes()
    {
        var (manifest, _) = await WriteValidArtifactAsync(recordCount: 1, segmentCount: 1);
        var before = await reader.CaptureDigestAsync(manifest.ArtifactPath, CancellationToken.None);

        var unchanged = await reader.VerifyDigestUnchangedAsync(manifest.ArtifactPath, before, CancellationToken.None);
        unchanged.Should().BeTrue();

        await File.WriteAllTextAsync(manifest.ArtifactPath, "[{}]", CancellationToken.None);
        var changed = await reader.VerifyDigestUnchangedAsync(manifest.ArtifactPath, before, CancellationToken.None);
        changed.Should().BeFalse("mutating the file breaks the digest");
    }

    [Fact]
    public async Task Throws_when_manifest_missing()
    {
        // Empty temp root, no manifest.json.
        var act = async () => await reader.ReadAsync(tempRoot, CancellationToken.None);

        (await act.Should().ThrowAsync<FileNotFoundException>())
            .WithMessage("*manifest*");
    }

    [Fact]
    public async Task Throws_when_data_file_entry_missing_sha256()
    {
        var artifact = await WriteArtifactAsync(recordCount: 1, segmentCount: 1);
        await File.WriteAllTextAsync(Path.Combine(tempRoot, "manifest.json"), $$"""
            {
              "files": [
                {
                  "role": "enriched-morphology",
                  "relativePath": "corpus-based-enriched-morphology.dashboard-ready.json",
                  "sizeBytes": {{artifact.SizeBytes}},
                  "recordCount": 1,
                  "segmentCount": 1
                }
              ]
            }
            """);

        var act = async () => await reader.ReadAsync(tempRoot, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidDataException>())
            .WithMessage("*missing sha256*");
    }

    private async Task<(ManifestFixture Manifest, string RawJson)> WriteValidArtifactAsync(
        int recordCount, int segmentCount) =>
        await WriteArtifactWithManifestAsync(recordCount, segmentCount);

    private async Task<(ManifestFixture Manifest, string RawJson)> WriteArtifactWithManifestAsync(
        int recordCount,
        int segmentCount,
        string? sha256Override = null,
        long? sizeOverride = null)
    {
        var fixture = await WriteArtifactAsync(recordCount, segmentCount);
        var sha = sha256Override ?? fixture.Sha256;
        var size = sizeOverride ?? fixture.SizeBytes;
        var manifestJson = $$"""
            {
              "schema": "quran-enriched-morphology-v1",
              "provenance": "corpus-bridge-enriched",
              "quranWordIdVerifiedAgainstDashboard": true,
              "files": [
                {
                  "role": "enriched-morphology",
                  "relativePath": "corpus-based-enriched-morphology.dashboard-ready.json",
                  "sha256": "{{sha}}",
                  "sizeBytes": {{size}},
                  "recordCount": {{recordCount}},
                  "segmentCount": {{segmentCount}}
                }
              ]
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(tempRoot, "manifest.json"), manifestJson);
        return (fixture with { Sha256 = sha, SizeBytes = size }, fixture.RawJson);
    }

    private async Task<ManifestFixture> WriteArtifactAsync(int recordCount, int segmentCount)
    {
        var artifactPath = Path.Combine(tempRoot, "corpus-based-enriched-morphology.dashboard-ready.json");

        // Build a minimal valid artifact: a top-level array of records, each with the requested segment
        // count. The exact record/segment JSON shape is not asserted here (the reader tests do that); this
        // builder only needs to produce a valid array whose record/segment counts the manifest gate checks.
        var sb = new StringBuilder("[");
        for (var r = 0; r < recordCount; r++)
        {
            if (r > 0) sb.Append(',');
            sb.Append("{\"location\":\"1:1:").Append(r + 1).Append("\",\"quranWordId\":")
              .Append(r + 1).Append(",\"corpusPresent\":true,\"segments\":[");
            for (var s = 0; s < segmentCount; s++)
            {
                if (s > 0) sb.Append(',');
                sb.Append("{\"segmentNumber\":").Append(s + 1).Append(",\"kind\":\"STEM\",\"pos\":\"N\"}");
            }
            sb.Append("]}");
        }
        sb.Append(']');
        var rawJson = sb.ToString();
        await File.WriteAllTextAsync(artifactPath, rawJson);

        var bytes = await File.ReadAllBytesAsync(artifactPath);
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        return new ManifestFixture(artifactPath, sha256, bytes.Length, rawJson);
    }

    private sealed record ManifestFixture(string ArtifactPath, string Sha256, long SizeBytes, string RawJson);
}
