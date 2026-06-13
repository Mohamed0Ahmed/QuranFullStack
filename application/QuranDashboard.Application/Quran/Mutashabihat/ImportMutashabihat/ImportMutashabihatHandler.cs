using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;

public sealed class ImportMutashabihatHandler
{
    private readonly IMutashabihatImportSource importSource;
    private readonly IMutashabihatImportWriter importWriter;

    public ImportMutashabihatHandler(
        IMutashabihatImportSource importSource,
        IMutashabihatImportWriter importWriter)
    {
        this.importSource = importSource;
        this.importWriter = importWriter;
    }

    public async Task<ImportMutashabihatResult> HandleAsync(
        ImportMutashabihatCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourcePath);

        try
        {
            var source = await importSource.LoadAsync(command.SourcePath, ct);

            if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
            {
                return ImportMutashabihatResult.Refused(MutashabihatInvariants.TargetsNotEmpty);
            }

            var result = await importWriter.ImportAsync(
                source,
                command.Force,
                command.ExpectedCounts ?? MutashabihatInvariants.Production,
                token => importSource.SourceUnchangedAsync(command.SourcePath, token),
                ct);

            return string.Equals(result.Verdict, "pass", StringComparison.Ordinal)
                ? ImportMutashabihatResult.Success(result.Totals)
                : ImportMutashabihatResult.Failure(
                    result.Errors.Count > 0
                        ? result.Errors[0]
                        : "Mutashabihat import validation failed.");
        }
        catch (MutashabihatSourceException)
        {
            return ImportMutashabihatResult.Refused(MutashabihatInvariants.SourceMismatch);
        }
        catch (IOException)
        {
            return ImportMutashabihatResult.Refused(MutashabihatInvariants.SourceMismatch);
        }
        catch (InvalidOperationException ex) when (
            ex.Message == MutashabihatInvariants.TargetsNotEmpty
            || ex.Message == MutashabihatInvariants.AyahsMissing)
        {
            return ImportMutashabihatResult.Refused(ex.Message);
        }
        catch (InvalidDataException ex)
        {
            return ImportMutashabihatResult.Failure(ex.Message);
        }
    }
}
