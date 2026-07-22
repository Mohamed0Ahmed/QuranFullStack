using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;
using QuranDashboard.Domain.Quran.Words.Display;
using QuranDashboard.Domain.Quran.Words.Morphology;
using QuranDashboard.Domain.Quran.Mutashabihat;
using QuranDashboard.Domain.Quran.Navigation;
using QuranDashboard.Domain.Quran.FullI3rab;
using QuranDashboard.Domain.Quran.Tafsirs;
using QuranDashboard.Domain.Quran.Translations;
using QuranDashboard.Domain.Quran.Words.Morphology.Irab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Domain.Abwab.Concurrency;
using QuranDashboard.Domain.Abwab.Timeline;

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
    public DbSet<QuranLemmaAnalysis> QuranLemmaAnalyses => Set<QuranLemmaAnalysis>();
    public DbSet<QuranStem> QuranStems => Set<QuranStem>();
    public DbSet<PosTag> PosTags => Set<PosTag>();
    public DbSet<QuranI3rabRule> QuranI3rabRules => Set<QuranI3rabRule>();
    public DbSet<MutashabihatGroup> MutashabihatGroups => Set<MutashabihatGroup>();
    public DbSet<MutashabihatOccurrence> MutashabihatOccurrences => Set<MutashabihatOccurrence>();
    public DbSet<SimilarAyahLink> SimilarAyahLinks => Set<SimilarAyahLink>();
    public DbSet<TafsirSource> TafsirSources => Set<TafsirSource>();
    public DbSet<TafsirEntry> TafsirEntries => Set<TafsirEntry>();
    public DbSet<TafsirAyahEntry> TafsirAyahEntries => Set<TafsirAyahEntry>();
    public DbSet<TranslationSource> TranslationSources => Set<TranslationSource>();
    public DbSet<TranslationAyahEntry> TranslationAyahEntries => Set<TranslationAyahEntry>();
    public DbSet<Juz> QuranJuzs => Set<Juz>();
    public DbSet<Hizb> QuranHizbs => Set<Hizb>();
    public DbSet<Rub> QuranRubs => Set<Rub>();
    public DbSet<Sajda> QuranSajdas => Set<Sajda>();
    public DbSet<FullI3rabSource> FullI3rabSources => Set<FullI3rabSource>();
    public DbSet<FullI3rabEntry> FullI3rabEntries => Set<FullI3rabEntry>();
    public DbSet<FullI3rabAyahEntry> FullI3rabAyahEntries => Set<FullI3rabAyahEntry>();

    public DbSet<User> AccessUsers => Set<User>();
    public DbSet<Role> AccessRoles => Set<Role>();

    // Abwab safety-foundation kernel substrate (028). No Quran foreign key exists on any of these until
    // this feature's exit is accepted (FR-009).
    public DbSet<ChangeSet> AbwabChangeSets => Set<ChangeSet>();
    public DbSet<AuditEvent> AbwabAuditEvents => Set<AuditEvent>();
    public DbSet<AbwabRevisionState> AbwabRevisionStates => Set<AbwabRevisionState>();
    public DbSet<TimelineGenerationBoundary> AbwabTimelineGenerationBoundaries => Set<TimelineGenerationBoundary>();
    public DbSet<AbwabWriteBarrier> AbwabWriteBarriers => Set<AbwabWriteBarrier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuranDashboardDbContext).Assembly);
    }
}
