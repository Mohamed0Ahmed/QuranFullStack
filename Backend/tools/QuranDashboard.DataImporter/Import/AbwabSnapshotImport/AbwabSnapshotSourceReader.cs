using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal sealed class AbwabSnapshotSourceReader
{
    private static readonly Regex ChecksumPattern = new(
        @"\A(?<sha>[0-9a-fA-F]{64})  (?<file>[^\r\n]+)\r?\n?\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal async Task<AbwabSnapshotSourcePackage> LoadAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var fullSourcePath = Path.GetFullPath(sourcePath);
        var checksumPath = $"{fullSourcePath}.sha256";
        if (!File.Exists(fullSourcePath))
        {
            throw new AbwabSnapshotImportException("The Abwab v4 snapshot source file was not found.");
        }

        if (!File.Exists(checksumPath))
        {
            throw new AbwabSnapshotImportException("The adjacent Abwab snapshot SHA-256 sidecar was not found.");
        }

        var sourceBytes = await File.ReadAllBytesAsync(fullSourcePath, cancellationToken);
        var checksumBytes = await File.ReadAllBytesAsync(checksumPath, cancellationToken);
        var sourceDigest = ComputeDigest(sourceBytes);
        var checksumDigest = ComputeDigest(checksumBytes);
        ValidateChecksumSidecar(checksumBytes, fullSourcePath, sourceDigest.Sha256);

        AbwabSnapshotDocument snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<AbwabSnapshotDocument>(sourceBytes, JsonOptions)
                ?? throw new AbwabSnapshotImportException("The Abwab snapshot JSON document is empty.");
        }
        catch (JsonException)
        {
            throw new AbwabSnapshotImportException("The Abwab snapshot JSON document is invalid.");
        }

        var validation = AbwabSnapshotImportSourceValidator.Validate(snapshot);
        if (!validation.Succeeded)
        {
            throw new AbwabSnapshotImportException(
                validation.Errors[0],
                validation.Checks,
                validation.Warnings);
        }

        return new AbwabSnapshotSourcePackage(
            fullSourcePath,
            checksumPath,
            sourceDigest.Sha256,
            sourceDigest,
            checksumDigest,
            snapshot,
            validation.Checks,
            validation.Warnings);
    }

    internal async Task<bool> SourceUnchangedAsync(
        AbwabSnapshotSourcePackage package,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (!File.Exists(package.SourcePath) || !File.Exists(package.ChecksumPath))
        {
            return false;
        }

        var sourceBytes = await File.ReadAllBytesAsync(package.SourcePath, cancellationToken);
        var checksumBytes = await File.ReadAllBytesAsync(package.ChecksumPath, cancellationToken);
        return ComputeDigest(sourceBytes) == package.SourceDigest
            && ComputeDigest(checksumBytes) == package.ChecksumDigest;
    }

    private static void ValidateChecksumSidecar(
        byte[] checksumBytes,
        string sourcePath,
        string actualSha256)
    {
        string checksumText;
        try
        {
            checksumText = new UTF8Encoding(false, true).GetString(checksumBytes);
        }
        catch (DecoderFallbackException)
        {
            throw new AbwabSnapshotImportException("The Abwab snapshot checksum sidecar is not valid UTF-8.");
        }

        var match = ChecksumPattern.Match(checksumText);
        if (!match.Success
            || !string.Equals(match.Groups["file"].Value, Path.GetFileName(sourcePath), StringComparison.Ordinal))
        {
            throw new AbwabSnapshotImportException("The Abwab snapshot checksum sidecar has an invalid contract.");
        }

        if (!string.Equals(match.Groups["sha"].Value, actualSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new AbwabSnapshotImportException("The Abwab snapshot bytes do not match the adjacent SHA-256 sidecar.");
        }
    }

    private static AbwabSnapshotFileDigest ComputeDigest(byte[] bytes) =>
        new(bytes.LongLength, Convert.ToHexStringLower(SHA256.HashData(bytes)));
}
