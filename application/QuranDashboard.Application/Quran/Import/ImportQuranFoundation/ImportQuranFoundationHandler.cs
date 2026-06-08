using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Application.Quran.Import.ImportQuranFoundation;

public sealed class ImportQuranFoundationHandler
{
    private readonly IQuranImportSource importSource;
    private readonly QuranFoundationAssembler assembler;
    private readonly IQuranImportWriter importWriter;

    public ImportQuranFoundationHandler(
        IQuranImportSource importSource,
        QuranFoundationAssembler assembler,
        IQuranImportWriter importWriter)
    {
        this.importSource = importSource;
        this.assembler = assembler;
        this.importWriter = importWriter;
    }

    public async Task<ImportQuranFoundationResult> HandleAsync(
        ImportQuranFoundationCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourceRoot);

        try
        {
            var sourceData = await importSource.LoadAsync(command.SourceRoot, ct);
            var assembled = assembler.Assemble(sourceData);

            await importWriter.WriteAsync(assembled, force: false, ct);

            var totals = new ImportTotals(
                assembled.Surahs.Count,
                assembled.Ayahs.Count,
                assembled.Pages.Count,
                assembled.Lines.Count,
                assembled.Words.Count,
                assembled.Words.Count(word => word.IsAyahMarker),
                assembled.Words.Count(word => !word.IsAyahMarker));

            return ImportQuranFoundationResult.Success(totals);
        }
        catch (Exception ex) when (ex is InvalidDataException or FileNotFoundException or IOException)
        {
            return ImportQuranFoundationResult.Failure(ex.Message);
        }
    }
}
