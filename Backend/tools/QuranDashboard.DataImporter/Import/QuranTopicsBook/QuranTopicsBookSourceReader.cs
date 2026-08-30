using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.DataImporter.Import.QuranTopicsBook;

internal sealed partial class QuranTopicsBookSourceReader
{
    internal async Task<QuranTopicsBookSourcePackage> LoadAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            throw new QuranTopicsBookImportException($"Source file was not found: {sourcePath}");
        }

        var checksumPath = sourcePath + ".sha256";
        if (!File.Exists(checksumPath))
        {
            throw new QuranTopicsBookImportException($"Adjacent checksum file was not found: {checksumPath}");
        }

        var sourceBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        var sourceSha256 = Convert.ToHexStringLower(SHA256.HashData(sourceBytes));
        var checksumText = (await File.ReadAllTextAsync(checksumPath, cancellationToken)).Trim();
        var expectedSha256 = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (expectedSha256 is null
            || !Sha256Regex().IsMatch(expectedSha256)
            || !string.Equals(sourceSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new QuranTopicsBookImportException("The source SHA-256 does not match its adjacent checksum.");
        }

        QuranTopicsBookDocument document;
        try
        {
            document = JsonSerializer.Deserialize(sourceBytes, QuranTopicsBookJsonContext.Default.QuranTopicsBookDocument)
                ?? throw new JsonException("The source document is empty.");
        }
        catch (JsonException exception)
        {
            throw new QuranTopicsBookImportException($"The source JSON is invalid ({exception.Message}).");
        }

        QuranTopicsBookMetrics metrics;
        List<string> checks;
        List<string> warnings;
        List<string> errors;
        try
        {
            (metrics, checks, warnings, errors) = QuranTopicsBookSourceValidator.Validate(document);
        }
        catch (NullReferenceException)
        {
            throw new QuranTopicsBookImportException("The source JSON is missing a required object or collection.");
        }
        if (errors.Count > 0)
        {
            throw new QuranTopicsBookImportException(
                $"The source failed validation: {string.Join("; ", errors)}",
                checks,
                warnings);
        }

        return new QuranTopicsBookSourcePackage(
            sourcePath,
            sourceSha256,
            document,
            metrics,
            checks,
            warnings);
    }

    internal async Task<bool> SourceUnchangedAsync(
        QuranTopicsBookSourcePackage package,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(package.SourcePath) || !File.Exists(package.SourcePath + ".sha256"))
        {
            return false;
        }

        var bytes = await File.ReadAllBytesAsync(package.SourcePath, cancellationToken);
        return string.Equals(
            Convert.ToHexStringLower(SHA256.HashData(bytes)),
            package.Sha256,
            StringComparison.Ordinal);
    }

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
