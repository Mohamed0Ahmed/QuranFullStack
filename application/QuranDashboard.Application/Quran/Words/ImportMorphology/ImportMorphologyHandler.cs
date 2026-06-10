using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Application.Quran.Words.ImportMorphology;

public sealed class ImportMorphologyHandler
{
    private readonly IMorphologyImportSource importSource;
    private readonly IMorphologyImportWriter importWriter;

    public ImportMorphologyHandler(
        IMorphologyImportSource importSource,
        IMorphologyImportWriter importWriter)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
    }

    public async Task<ImportMorphologyResult> HandleAsync(
        ImportMorphologyCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        try
        {
            var source = await importSource.LoadAsync(command.SourcePath, ct);

            if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
            {
                return ImportMorphologyResult.Refused(MorphologyInvariants.TargetsNotEmpty);
            }

            var result = await importWriter.ImportAsync(
                source,
                command.Force,
                command.ExpectedReadableWords,
                ct);

            return string.Equals(result.Verdict, "pass", StringComparison.Ordinal)
                ? ImportMorphologyResult.Success(result.Totals)
                : ImportMorphologyResult.Failure(
                    result.Errors.Count > 0
                        ? result.Errors[0]
                        : "Morphology import validation failed.");
        }
        catch (InvalidOperationException ex) when (
            ex.Message == MorphologyInvariants.TargetsNotEmpty
            || ex.Message == MorphologyInvariants.FoundationNotLoaded)
        {
            return ImportMorphologyResult.Refused(ex.Message);
        }
        catch (InvalidDataException)
        {
            return ImportMorphologyResult.Refused(MorphologyInvariants.SourceMismatch);
        }
        catch (Exception ex) when (ex is FileNotFoundException or IOException)
        {
            return ImportMorphologyResult.Refused(MorphologyInvariants.SourceMismatch);
        }
    }
}
