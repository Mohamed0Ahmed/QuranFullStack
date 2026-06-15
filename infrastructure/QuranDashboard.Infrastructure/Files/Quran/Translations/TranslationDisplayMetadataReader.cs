using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.Translations;

public sealed class TranslationDisplayMetadataReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] RequiredDisplayFields =
    [
        "displayNameEn",
        "displayNameAr",
        "languageCode",
        "languageNameEn",
        "languageNameAr",
        "nativeName",
        "direction",
        "translationType",
        "sourceKey",
        "packageFile"
    ];

    public async Task<TranslationDisplayMetadataDocument> ReadAsync(
        string packagePath,
        IReadOnlyCollection<string> approvedSourceKeys,
        TranslationExpectedCounts? expectedCounts,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(approvedSourceKeys);

        var metadataPath = Path.Combine(packagePath, "source-display-metadata.json");
        if (!File.Exists(metadataPath))
        {
            throw new TranslationSourceException(
                $"Display metadata file was not found: {metadataPath}");
        }

        await using var stream = File.OpenRead(metadataPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = document.RootElement;

        var metadataType = root.GetProperty("metadataType").GetString()
            ?? throw new TranslationSourceException("source-display-metadata.json is missing metadataType.");
        var status = root.GetProperty("status").GetString()
            ?? throw new TranslationSourceException("source-display-metadata.json is missing status.");
        var sourceCount = root.GetProperty("sourceCount").GetInt32();

        var checks = new List<TranslationCheckResult>
        {
            ValidateDisplayMetadataFinal(metadataType, status, sourceCount, expectedCounts)
        };
        TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

        var records = new List<TranslationDisplayMetadataRecord>();
        foreach (var recordElement in root.GetProperty("records").EnumerateArray())
        {
            var record = recordElement.Deserialize<TranslationDisplayMetadataRecord>(JsonOptions)
                ?? throw new TranslationSourceException("A display metadata record could not be parsed.");

            records.Add(record);
        }

        checks.Add(ValidateDisplayMetadataSet(approvedSourceKeys, records));
        checks.Add(ValidateRequiredFields(records));
        TranslationValidationChecks.EnsureAllHardChecksPassed(checks);

        return new TranslationDisplayMetadataDocument(
            metadataType,
            status,
            sourceCount,
            records,
            root.GetRawText());
    }

    private static TranslationCheckResult ValidateDisplayMetadataFinal(
        string metadataType,
        string status,
        int sourceCount,
        TranslationExpectedCounts? expectedCounts)
    {
        var expectedSourceCount = expectedCounts?.ApprovedSources ?? sourceCount;
        var passed = string.Equals(metadataType, TranslationImportConstants.DisplayMetadataType, StringComparison.Ordinal)
            && string.Equals(status, TranslationImportConstants.DisplayMetadataFinalStatus, StringComparison.Ordinal)
            && sourceCount == expectedSourceCount;

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckDisplayMetadataFinal,
            $"metadataType={TranslationImportConstants.DisplayMetadataType}, status={TranslationImportConstants.DisplayMetadataFinalStatus}, sourceCount={expectedSourceCount}",
            $"metadataType={metadataType}, status={status}, sourceCount={sourceCount}",
            passed);
    }

    private static TranslationCheckResult ValidateDisplayMetadataSet(
        IReadOnlyCollection<string> approvedSourceKeys,
        IReadOnlyList<TranslationDisplayMetadataRecord> records)
    {
        var approvedSet = approvedSourceKeys.ToHashSet(StringComparer.Ordinal);
        var metadataKeys = records.Select(record => record.SourceKey).ToHashSet(StringComparer.Ordinal);

        var missing = approvedSet.Except(metadataKeys).ToList();
        var extra = metadataKeys.Except(approvedSet).ToList();
        var passed = missing.Count == 0 && extra.Count == 0 && records.Count == approvedSet.Count;

        var observed = passed
            ? "exact match"
            : string.Join(
                "; ",
                new[]
                {
                    missing.Count > 0 ? $"missing: {string.Join(", ", missing)}" : null,
                    extra.Count > 0 ? $"extra: {string.Join(", ", extra)}" : null
                }.Where(part => part is not null));

        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckDisplayMetadataSet,
            "display metadata sourceKey set exactly matches manifest approved source set",
            observed,
            passed);
    }

    private static TranslationCheckResult ValidateRequiredFields(
        IReadOnlyList<TranslationDisplayMetadataRecord> records)
    {
        var failures = new List<string>();

        foreach (var record in records)
        {
            if (!string.Equals(
                    record.MetadataStatus,
                    TranslationImportConstants.DisplayRecordFinalStatus,
                    StringComparison.Ordinal))
            {
                failures.Add($"{record.SourceKey}:metadataStatus");
            }

            foreach (var field in RequiredDisplayFields)
            {
                if (!HasRequiredField(record, field))
                {
                    failures.Add($"{record.SourceKey}:{field}");
                }
            }
        }

        var passed = failures.Count == 0;
        return TranslationValidationChecks.Hard(
            TranslationInvariants.CheckDisplayMetadataRequiredFields,
            string.Join(", ", RequiredDisplayFields),
            passed ? "all present and non-empty" : string.Join(", ", failures),
            passed);
    }

    private static bool HasRequiredField(TranslationDisplayMetadataRecord record, string fieldName)
    {
        var value = fieldName switch
        {
            "displayNameEn" => record.DisplayNameEn,
            "displayNameAr" => record.DisplayNameAr,
            "languageCode" => record.LanguageCode,
            "languageNameEn" => record.LanguageNameEn,
            "languageNameAr" => record.LanguageNameAr,
            "nativeName" => record.NativeName,
            "direction" => record.Direction,
            "translationType" => record.TranslationType,
            "sourceKey" => record.SourceKey,
            "packageFile" => record.PackageFile,
            _ => null
        };

        return !string.IsNullOrWhiteSpace(value);
    }
}

public sealed record TranslationDisplayMetadataDocument(
    string MetadataType,
    string Status,
    int SourceCount,
    IReadOnlyList<TranslationDisplayMetadataRecord> Records,
    string MetadataJson);

public sealed record TranslationDisplayMetadataRecord(
    string SourceKey,
    string PackageFile,
    string LanguageCode,
    string LanguageNameEn,
    string LanguageNameAr,
    string NativeName,
    string Direction,
    string TranslationType,
    string DisplayNameEn,
    string DisplayNameAr,
    string? TranslatorKey,
    string? TranslatorNameEn,
    string? TranslatorNameAr,
    string MetadataStatus);
