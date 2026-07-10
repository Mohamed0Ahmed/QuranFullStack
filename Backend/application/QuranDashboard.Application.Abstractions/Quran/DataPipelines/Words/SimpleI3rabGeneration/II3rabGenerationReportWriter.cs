namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public interface II3rabGenerationReportWriter
{
    string Write(I3rabGenerationResult result, string outputDirectory);

    string WriteRefusal(I3rabRefusalReport refusal, string outputDirectory);
}
