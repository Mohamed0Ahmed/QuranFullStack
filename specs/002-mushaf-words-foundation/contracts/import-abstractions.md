# Contract — Import Abstractions (Application.Abstractions/Quran/Import)

Stable interfaces the Application orchestrator depends on; Infrastructure implements them. **No EF types** appear in these signatures — they exchange plain import DTOs. Signatures are illustrative (final names may be tidied during implementation) but the **boundaries are fixed**.

```csharp
namespace QuranDashboard.Application.Abstractions.Quran.Import;

// Reads + count/checksum-validates the manifested source set, returning raw import DTOs.
public interface IQuranImportSource
{
    Task<QuranImportSourceData> LoadAsync(string sourceRoot, CancellationToken ct);
}

// Plain DTO bundle (not EF entities). Each list mirrors a source file 1:1.
public sealed record QuranImportSourceData(
    IReadOnlyList<SurahMetaDto> Surahs,           // 114
    IReadOnlyList<AyahMetaDto> Ayahs,             // 6,236
    IReadOnlyList<WordRecordDto> Glyph,           // 83,668 (qpc-v4)
    IReadOnlyList<WordRecordDto> Uthmani,         // 83,668
    IReadOnlyList<WordRecordDto> UthmaniSimple,   // 83,668
    IReadOnlyList<WordRecordDto> ImlaeiSimple,    // 83,668
    LayoutDto Layout);                            // 604 pages / 9,046 lines

// Persists the assembled, validated rows (bulk COPY) in a single transaction.
// Implementation enforces refuse-unless-empty unless force = true (atomic truncate+reload).
public interface IQuranImportWriter
{
    Task WriteAsync(AssembledQuranData data, bool force, CancellationToken ct);
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);
}

// Writes the md + json validation report (see validation-report.schema.md).
public interface IImportReportWriter
{
    Task WriteAsync(QuranImportValidationResult result, string outputDir, CancellationToken ct);
}
```

- `AssembledQuranData` and `QuranImportValidationResult` are **Application** types (DTOs/results), built by the orchestrator/validator.
- Source DTO shapes (`WordRecordDto`, `LayoutDto`, etc.) are defined in `source-files.md`.
- The read-side interface `IMushafPageReadRepository` is **declared only** (for follow-up 001b) and is **not implemented** in this feature.
