namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

public sealed record GetAyahStudyQuery(
    string VerseKey,
    string? TafsirSource,
    string? TranslationSource,
    string? FullI3rabSource);
