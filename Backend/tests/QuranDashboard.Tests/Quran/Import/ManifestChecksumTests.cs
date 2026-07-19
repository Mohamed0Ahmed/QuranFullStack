using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Foundation;

namespace QuranDashboard.Tests.Quran.Import;

// ManifestChecksum is the shared SHA-256 helper extracted from the 8 manifest readers under
// DataPipelines/ (decision 5, DRY). Each reader keeps its own throw-vs-CheckResult shell; this
// only covers the shared computation + comparison it now delegates to.
public sealed class ManifestChecksumTests
{
    [Fact]
    public async Task ComputeSha256Hex_matches_known_hash_of_file_contents()
    {
        // SHA-256("hello world") — a widely published test vector.
        const string expectedHex = "B94D27B9934D3E08A52E52D7DA7DABFAC484EFE37A5380EE9088F7ACE2EFCDE9";
        var path = await WriteTempFileAsync("hello world");

        try
        {
            var actual = ManifestChecksum.ComputeSha256Hex(path);

            actual.Should().Be(expectedHex);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ComputeSha256HexAsync_matches_ComputeSha256Hex_for_the_same_file()
    {
        var path = await WriteTempFileAsync("the same content, hashed two ways");

        try
        {
            var sync = ManifestChecksum.ComputeSha256Hex(path);
            var async = await ManifestChecksum.ComputeSha256HexAsync(path, CancellationToken.None);

            async.Should().Be(sync);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("ABCDEF0123456789", "abcdef0123456789", true)]
    [InlineData("ABCDEF0123456789", "ABCDEF0123456789", true)]
    [InlineData("ABCDEF0123456789", "0000000000000000", false)]
    public void Matches_compares_case_insensitively(string actual, string expected, bool expectedResult)
    {
        ManifestChecksum.Matches(actual, expected).Should().Be(expectedResult);
    }

    private static async Task<string> WriteTempFileAsync(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manifest-checksum-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, content);
        return path;
    }
}
