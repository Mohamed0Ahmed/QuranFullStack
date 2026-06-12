using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Domain.Quran.Words.Display;
using QuranDashboard.Domain.Quran.Words.Morphology;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Persistence;

public sealed class QuranDashboardDbContext(DbContextOptions<QuranDashboardDbContext> options) : DbContext(options)
{
    public DbSet<Surah> QuranSurahs => Set<Surah>();
    public DbSet<Ayah> QuranAyahs => Set<Ayah>();
    public DbSet<MushafPage> QuranMushafPages => Set<MushafPage>();
    public DbSet<MushafLine> QuranMushafLines => Set<MushafLine>();
    public DbSet<QuranWord> QuranWords => Set<QuranWord>();
    public DbSet<OrderedTashkeelWord> QuranWordsOrderedTashkeel => Set<OrderedTashkeelWord>();
    public DbSet<OrderedSimpleWord> QuranWordsOrderedSimple => Set<OrderedSimpleWord>();
    public DbSet<UniqueTashkeelWord> QuranWordsUniqueTashkeel => Set<UniqueTashkeelWord>();
    public DbSet<UniqueSimpleWord> QuranWordsUniqueSimple => Set<UniqueSimpleWord>();
    public DbSet<WordMorphology> WordMorphologies => Set<WordMorphology>();
    public DbSet<WordMorphologySegment> WordMorphologySegments => Set<WordMorphologySegment>();
    public DbSet<QuranRoot> QuranRoots => Set<QuranRoot>();
    public DbSet<QuranLemma> QuranLemmas => Set<QuranLemma>();
    public DbSet<QuranStem> QuranStems => Set<QuranStem>();
    public DbSet<PosTag> PosTags => Set<PosTag>();
    public DbSet<QuranI3rabRule> QuranI3rabRules => Set<QuranI3rabRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuranDashboardDbContext).Assembly);
    }
}
