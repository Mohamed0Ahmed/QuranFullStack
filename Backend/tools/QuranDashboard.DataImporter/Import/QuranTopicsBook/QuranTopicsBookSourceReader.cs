using System.Security.Cryptography;
using System.Text;
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
        var checksumBytes = await File.ReadAllBytesAsync(checksumPath, cancellationToken);
        ValidateChecksumSidecar(checksumBytes, sourcePath, sourceSha256);

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
            checksumPath,
            sourceSha256,
            Convert.ToHexStringLower(SHA256.HashData(checksumBytes)),
            document,
            metrics,
            checks,
            warnings);
    }

    internal async Task<bool> SourceUnchangedAsync(
        QuranTopicsBookSourcePackage package,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(package.SourcePath) || !File.Exists(package.ChecksumPath))
        {
            return false;
        }

        var bytes = await File.ReadAllBytesAsync(package.SourcePath, cancellationToken);
        var checksumBytes = await File.ReadAllBytesAsync(package.ChecksumPath, cancellationToken);
        return string.Equals(Convert.ToHexStringLower(SHA256.HashData(bytes)), package.Sha256, StringComparison.Ordinal)
            && string.Equals(
                Convert.ToHexStringLower(SHA256.HashData(checksumBytes)),
                package.ChecksumSha256,
                StringComparison.Ordinal);
    }

    private static void ValidateChecksumSidecar(
        byte[] checksumBytes,
        string sourcePath,
        string sourceSha256)
    {
        string checksumText;
        try
        {
            checksumText = new UTF8Encoding(false, true).GetString(checksumBytes);
        }
        catch (DecoderFallbackException)
        {
            throw new QuranTopicsBookImportException("The source checksum sidecar is not valid UTF-8.");
        }

        var match = ChecksumPattern().Match(checksumText);
        if (!match.Success
            || !string.Equals(match.Groups["file"].Value, Path.GetFileName(sourcePath), StringComparison.Ordinal))
        {
            throw new QuranTopicsBookImportException("The source checksum sidecar has an invalid contract.");
        }

        if (!string.Equals(sourceSha256, match.Groups["sha"].Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new QuranTopicsBookImportException("The source SHA-256 does not match its adjacent checksum.");
        }
    }

    [GeneratedRegex(@"\A(?<sha>[0-9a-fA-F]{64})  (?<file>[^\r\n]+)\r?\n?\z", RegexOptions.CultureInvariant)]
    private static partial Regex ChecksumPattern();
}
