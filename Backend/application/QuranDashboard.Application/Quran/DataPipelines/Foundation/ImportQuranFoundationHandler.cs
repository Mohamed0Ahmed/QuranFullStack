using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;
using QuranDashboard.Application.Quran.DataPipelines.Foundation.Validation;

namespace QuranDashboard.Application.Quran.DataPipelines.Foundation;

public sealed class ImportQuranFoundationHandler
{
    private readonly IQuranImportSource importSource;
    private readonly QuranFoundationAssembler assembler;
    private readonly QuranImportValidator validator;
    private readonly IQuranImportWriter importWriter;
    private readonly IImportReportWriter reportWriter;

    public ImportQuranFoundationHandler(
        IQuranImportSource importSource,
        QuranFoundationAssembler assembler,
        QuranImportValidator validator,
        IQuranImportWriter importWriter,
        IImportReportWriter reportWriter)
    {
        this.importSource = importSource;
        this.assembler = assembler;
        this.validator = validator;
        this.importWriter = importWriter;
        this.reportWriter = reportWriter;
    }

    public async Task<ImportQuranFoundationResult> HandleAsync(
        ImportQuranFoundationCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourceRoot);

        var reportOutDir = ResolveReportOutDir(command);

        try
        {
            if (!command.Force && await importWriter.AnyTargetTableHasDataAsync(ct))
            {
                return ImportQuranFoundationResult.Refused(ImportRefusalMessages.TablesNotEmpty);
            }

            var sourceData = await importSource.LoadAsync(command.SourceRoot, ct);
            var assembled = assembler.Assemble(sourceData);

            var validation = validator.Validate(
                command.SourceRoot,
                sourceData.ManifestVersion,
                sourceData,
                assembled,
                forced: command.Force);

            if (string.Equals(validation.Verdict, ImportValidationVerdicts.Fail, StringComparison.Ordinal))
            {
                await reportWriter.WriteAsync(validation, reportOutDir, ct);
                return ImportQuranFoundationResult.Failure(
                    $"Import validation failed ({validation.Errors.Count} hard rule(s)): {validation.Errors[0]}");
            }

            await importWriter.WriteAsync(assembled, command.Force, ct);

            validation = validation with { Persisted = true };

            await reportWriter.WriteAsync(validation, reportOutDir, ct);

            return ImportQuranFoundationResult.Success(validation.Totals);
        }
        catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException or IOException)
        {
            return ImportQuranFoundationResult.Failure(ex.Message);
        }
    }

    private static string ResolveReportOutDir(ImportQuranFoundationCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.ReportOutDir))
        {
            return Path.GetFullPath(command.ReportOutDir);
        }

        var sourceParent = Path.GetDirectoryName(Path.GetFullPath(command.SourceRoot))
            ?? throw new InvalidOperationException("Could not resolve the source root parent directory.");

        return Path.GetFullPath(Path.Combine(sourceParent, "..", "report"));
    }
}
