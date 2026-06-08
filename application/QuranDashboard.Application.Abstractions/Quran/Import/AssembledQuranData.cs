using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record AssembledQuranData(
    IReadOnlyList<Surah> Surahs,
    IReadOnlyList<Ayah> Ayahs,
    IReadOnlyList<MushafPage> Pages,
    IReadOnlyList<QuranWord> Words,
    IReadOnlyList<MushafLine> Lines);
