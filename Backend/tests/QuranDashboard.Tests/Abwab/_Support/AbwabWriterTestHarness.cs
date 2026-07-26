using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Application.Abwab.Categories;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Application.Abwab.Relationships;
using QuranDashboard.Application.Abwab.Sections;
using QuranDashboard.Application.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Application.Abwab.Tree;
using QuranDashboard.Infrastructure.Abwab.Caching;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;
using QuranDashboard.Infrastructure.Security.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab._Support;

// Constructs the real US3 write port (and its collaborators) directly against a fresh
// QuranDashboardDbContext, mirroring SecurityTestHarness's minimal-construction convention rather
// than spinning up the full ASP.NET DI container.
internal static class AbwabWriterTestHarness
{
    public static (IAbwabCoreWritePort WritePort, QuranDashboardDbContext Db) CreateWritePort(
        PostgresFixture fixture,
        IServerClock? clock = null,
        IDeletionReservationChecker? reservationChecker = null)
    {
        var db = AbwabKernelHarness.CreateProductionContext(fixture, new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));
        var effectiveClock = clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch);

        var executor = new AbwabAuditedCommitExecutor(db, effectiveClock, new NullAbwabCachePublisher());
        var sections = new EfSectionWriteStore(db);
        var categories = new EfCategoryTreeStore(db);
        var aliases = new EfCategorySearchAliasWriteStore(db);
        var protections = new EfManualProtectionWriteStore(db);
        var protectionReadPort = new EfManualProtectionReadPort(db);
        var resolver = new ProtectionResolver(protectionReadPort, effectiveClock);
        var owners = new SystemOwnerStore(db);
        var reservation = reservationChecker ?? new InertDeletionReservationChecker();

        var sectionHandler = new SectionWriterHandler(executor, sections);
        var contentHandler = new CategoryContentHandler(executor, categories, sections, resolver, owners, effectiveClock);
        var orderingHandler = new CategoryOrderingHandler(executor, categories, sections, resolver, owners, effectiveClock);
        var subtreeHandler = new CategorySubtreeHandler(executor, categories, sections, resolver, reservation, effectiveClock);
        var aliasHandler = new CategoryAliasHandler(executor, categories, aliases, resolver, owners, effectiveClock);
        var manualProtectionHandler = new ManualProtectionWriterHandler(executor, categories, protections, effectiveClock);
        var presetHandler = new FullProtectionPresetHandler(executor, categories, protections, effectiveClock);

        var writePort = new AbwabCoreWriteHandler(
            sectionHandler, contentHandler, orderingHandler, subtreeHandler, aliasHandler, manualProtectionHandler, presetHandler);

        return (writePort, db);
    }

    public static (IAbwabRelationshipWritePort WritePort, QuranDashboardDbContext Db) CreateRelationshipWritePort(
        PostgresFixture fixture,
        IServerClock? clock = null)
    {
        var db = AbwabKernelHarness.CreateProductionContext(fixture, new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));
        var effectiveClock = clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch);

        var handler = new RelationshipWriterHandler(
            new AbwabAuditedCommitExecutor(db, effectiveClock, new NullAbwabCachePublisher()),
            new EfCategoryRelationshipStore(db),
            new EfCategoryTreeStore(db),
            new EfSectionWriteStore(db),
            new ProtectionResolver(new EfManualProtectionReadPort(db), effectiveClock),
            effectiveClock);

        return (handler, db);
    }

    public static (IAbwabTemplateWritePort WritePort, QuranDashboardDbContext Db) CreateTemplateWritePort(
        PostgresFixture fixture,
        IServerClock? clock = null)
    {
        var db = AbwabKernelHarness.CreateProductionContext(fixture, new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));
        return (CreateTemplateWritePort(db, clock), db);
    }

    // The context is supplied so a budget test can attach its own command-counting interceptor and
    // still exercise the SAME wiring the other template tests run against.
    public static IAbwabTemplateWritePort CreateTemplateWritePort(QuranDashboardDbContext db, IServerClock? clock = null)
    {
        var effectiveClock = clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch);

        var executor = new AbwabAuditedCommitExecutor(db, effectiveClock, new NullAbwabCachePublisher());
        var templates = new EfDoorTemplateStore(db);
        var nodes = new EfTemplateNodeStore(db);
        var templateAliases = new EfTemplateNodeAliasStore(db);
        var categories = new EfCategoryTreeStore(db);
        var sections = new EfSectionWriteStore(db);
        var categoryAliases = new EfCategorySearchAliasWriteStore(db);
        var resolver = new ProtectionResolver(new EfManualProtectionReadPort(db), effectiveClock);

        return new AbwabTemplateWriteHandler(
            new TemplateAggregateHandler(executor, templates, nodes, templateAliases, effectiveClock),
            new TemplateNodeHandler(executor, templates, nodes, templateAliases, effectiveClock),
            new TemplateAliasHandler(executor, templates, nodes, templateAliases, effectiveClock),
            new TemplateApplicationHandler(executor, templates, nodes, templateAliases, categories, sections, categoryAliases, resolver));
    }

    // Reads every expectation the apply command carries fresh from the database, so a test states the
    // scenario it is proving instead of hand-counting revisions.
    public static async Task ApplyTemplateAsync(
        IAbwabTemplateWritePort writePort,
        QuranDashboardDbContext db,
        Guid doorTemplateId,
        Guid targetCategoryId,
        long? expectedTreeRevision = null,
        uint? expectedTargetVersion = null,
        long? expectedTemplateRevision = null)
    {
        var target = await db.AbwabCategories.AsNoTracking().SingleOrDefaultAsync(c => c.CategoryId == targetCategoryId);
        var template = await db.Set<DoorTemplate>().AsNoTracking().SingleOrDefaultAsync(t => t.DoorTemplateId == doorTemplateId);

        await writePort.ApplyDoorTemplateAsync(
            new ApplyDoorTemplateCommand(
                doorTemplateId,
                targetCategoryId,
                expectedTemplateRevision ?? template?.TemplateRevision ?? 0,
                expectedTreeRevision ?? await CurrentTreeRevisionAsync(db),
                expectedTargetVersion ?? target?.Version ?? 0,
                ExpectedTimelineGeneration.Of(0),
                "tester"),
            CancellationToken.None);
    }

    public static IAbwabTemplateReadPort CreateTemplateReadPort(QuranDashboardDbContext db, IServerClock? clock = null) =>
        new EfAbwabTemplateReadPort(db, clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch));

    public static IAbwabRelationshipReadPort CreateRelationshipReadPort(QuranDashboardDbContext db, IServerClock? clock = null) =>
        new EfAbwabRelationshipReadPort(db, clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch));

    public static ProtectionResolver CreateProtectionResolver(QuranDashboardDbContext db, IServerClock? clock = null) =>
        new(new EfManualProtectionReadPort(db), clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch));

    public static IAbwabCoreReadPort CreateReadPort(QuranDashboardDbContext db, IServerClock? clock = null) =>
        new EfAbwabCoreReadPort(db, clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch));

    // Sections now carry ExpectedTreeRevision too (Master Plan §8: sections' concurrency is xmin +
    // TreeRevision, not xmin alone) — tests read the current value fresh rather than hand-counting it.
    public static async Task<long> CurrentTreeRevisionAsync(QuranDashboardDbContext db) =>
        (await db.AbwabRevisionStates.AsNoTracking().SingleAsync()).TreeRevision;
}
