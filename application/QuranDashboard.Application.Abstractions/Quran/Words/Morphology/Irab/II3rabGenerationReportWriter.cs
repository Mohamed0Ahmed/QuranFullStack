namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public interface II3rabGenerationReportWriter
{
    string Write(I3rabGenerationResult result, string outputDirectory);

    string WriteRefusal(I3rabRefusalReport refusal, string outputDirectory);
}
