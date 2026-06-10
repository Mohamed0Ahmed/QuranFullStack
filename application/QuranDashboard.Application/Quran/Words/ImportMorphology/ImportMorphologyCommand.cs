using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Application.Quran.Words.ImportMorphology;

public sealed record ImportMorphologyCommand(
    string SourcePath,
    bool Force,
    int ExpectedReadableWords = MorphologyInvariants.ExpectedReadableWords);
