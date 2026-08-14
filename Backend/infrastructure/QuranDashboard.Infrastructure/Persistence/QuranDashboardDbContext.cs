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
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

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
    public DbSet<Permission> AccessPermissions => Set<Permission>();
    public DbSet<UserPermission> AccessUserPermissions => Set<UserPermission>();
    public DbSet<AccessAuditEvent> AccessAuditEvents => Set<AccessAuditEvent>();

    public DbSet<AbwabSection> AbwabSections => Set<AbwabSection>();
    public DbSet<AbwabDoor> AbwabDoors => Set<AbwabDoor>();
    public DbSet<AbwabDoorAlias> AbwabDoorAliases => Set<AbwabDoorAlias>();
    public DbSet<AbwabDoorRelation> AbwabDoorRelations => Set<AbwabDoorRelation>();
    public DbSet<AbwabTemplate> AbwabTemplates => Set<AbwabTemplate>();
    public DbSet<AbwabTemplateNode> AbwabTemplateNodes => Set<AbwabTemplateNode>();

    public DbSet<LinkingWorkspace> LinkingWorkspaces => Set<LinkingWorkspace>();
    public DbSet<LinkingWorkspaceSource> LinkingWorkspaceSources => Set<LinkingWorkspaceSource>();
    public DbSet<LinkingWorkspaceSourceManualAyah> LinkingWorkspaceSourceManualAyahs =>
        Set<LinkingWorkspaceSourceManualAyah>();
    public DbSet<LinkingWorkspaceSourceAyahOverride> LinkingWorkspaceSourceAyahOverrides =>
        Set<LinkingWorkspaceSourceAyahOverride>();
    public DbSet<LinkingWorkspaceSourceWord> LinkingWorkspaceSourceWords => Set<LinkingWorkspaceSourceWord>();
    public DbSet<LinkingWorkspaceSourceDescription> LinkingWorkspaceSourceDescriptions =>
        Set<LinkingWorkspaceSourceDescription>();

    public DbSet<LinkingOperation> LinkingOperations => Set<LinkingOperation>();
    public DbSet<LinkingConfirmationJob> LinkingConfirmationJobs => Set<LinkingConfirmationJob>();
    public DbSet<LinkingDoorAyah> LinkingDoorAyahs => Set<LinkingDoorAyah>();
    public DbSet<LinkingDoorAyahWord> LinkingDoorAyahWords => Set<LinkingDoorAyahWord>();
    public DbSet<LinkingSourceContribution> LinkingSourceContributions => Set<LinkingSourceContribution>();
    public DbSet<LinkingUnit> LinkingUnits => Set<LinkingUnit>();
    public DbSet<LinkingSourceContributionUnit> LinkingSourceContributionUnits => Set<LinkingSourceContributionUnit>();
    public DbSet<LinkingUnitAyah> LinkingUnitAyahs => Set<LinkingUnitAyah>();
    public DbSet<LinkingUnitAyahWord> LinkingUnitAyahWords => Set<LinkingUnitAyahWord>();
    public DbSet<LinkingUnitAyahDescription> LinkingUnitAyahDescriptions => Set<LinkingUnitAyahDescription>();
    public DbSet<LinkingDataState> LinkingDataStates => Set<LinkingDataState>();
    public DbSet<LinkingPreparedPreflight> LinkingPreparedPreflights => Set<LinkingPreparedPreflight>();
    public DbSet<LinkingPreparedSource> LinkingPreparedSources => Set<LinkingPreparedSource>();
    public DbSet<LinkingPreparedUnit> LinkingPreparedUnits => Set<LinkingPreparedUnit>();
    public DbSet<LinkingPreparedAyah> LinkingPreparedAyahs => Set<LinkingPreparedAyah>();
    public DbSet<LinkingPreparedAyahWord> LinkingPreparedAyahWords => Set<LinkingPreparedAyahWord>();
    public DbSet<LinkingPreparedAyahDescription> LinkingPreparedAyahDescriptions =>
        Set<LinkingPreparedAyahDescription>();
    public DbSet<LinkingPreparedAffectedContribution> LinkingPreparedAffectedContributions =>
        Set<LinkingPreparedAffectedContribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuranDashboardDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        return SaveChanges(true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditEventsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAuditEventsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureAuditEventsAreAppendOnly()
    {
        if (ChangeTracker.Entries<AccessAuditEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Access audit events are append-only.");
        }
    }
}
