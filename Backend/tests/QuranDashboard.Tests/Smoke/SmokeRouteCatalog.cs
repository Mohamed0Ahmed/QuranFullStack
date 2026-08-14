using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Smoke;

// What a route requires of its caller, not who the caller is — SmokePersona is the caller side, and
// naming both ends "Anonymous" would put two different meanings on one word in one namespace.
internal enum SmokeRouteAccessKind
{
    Public,
    AuthenticatedOnly,
    Permission,
    OwnerOnly,
}

internal sealed record SmokeRouteAccess(SmokeRouteAccessKind Kind, string? PermissionCode = null)
{
    public static SmokeRouteAccess Public { get; } = new(SmokeRouteAccessKind.Public);
    public static SmokeRouteAccess AuthenticatedOnly { get; } = new(SmokeRouteAccessKind.AuthenticatedOnly);
    public static SmokeRouteAccess OwnerOnly { get; } = new(SmokeRouteAccessKind.OwnerOnly);

    public static SmokeRouteAccess Permission(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new(SmokeRouteAccessKind.Permission, code);
    }
}

// What "answered with real data" means for one route. Four payload shapes sit behind the shared envelope:
// a paged read, an object wrapping a named collection, an object wrapping a named object, and a flat
// summary that is itself the payload. Each variant below states the one thing it reads, so an assertion
// is never a generic hunt for a non-empty anything.
//
// Every pinned count names a manifest table, never a literal: the dump and the numbers asserted against
// it then move together, and a count typed from memory cannot drift away from the artifact. Where the
// answer is one word's own occurrence count, which no manifest table records, the variant asserts a
// property real data produces rather than inventing a literal to pin.
internal abstract record SmokeSeededPayload
{
    private SmokeSeededPayload() { }

    // data.totalCount equals the table's manifest row count, and data.items carries at least one row.
    internal sealed record PagedTable(string ManifestTable) : SmokeSeededPayload;

    // data.items carries at least one row, with the total left unpinned: on the id-scoped reads the total
    // is one word's occurrence count, which no manifest table records and which only a literal could fix.
    internal sealed record NonEmptyPage : SmokeSeededPayload;

    // data.<Property> is an array whose length equals the table's manifest row count.
    internal sealed record CountedCollection(string Property, string ManifestTable) : SmokeSeededPayload;

    // data.<Property> is a non-empty array. Used where the row count of no single table is the answer.
    internal sealed record NonEmptyCollection(string Property) : SmokeSeededPayload;

    // data.<Property>.<NestedProperty> equals ExpectedValue — the key the request asked for, echoed back.
    // "The object is non-empty" would not do: every DTO behind these routes leads with a non-nullable
    // value type, so it is non-empty for any 200 regardless of the data, and the assertion could not fail.
    // Reading the key back proves the handler resolved the ayah or word that was asked for.
    internal sealed record EchoedKey(string Property, string NestedProperty, string ExpectedValue)
        : SmokeSeededPayload;

    // data.<Property> is a number greater than zero, on a payload that is a flat summary rather than a
    // wrapper. The count itself stays unpinned for the reason given above the variants.
    internal sealed record PositiveCount(string Property) : SmokeSeededPayload;
}

// A route's seeded answer is a second, independent expectation — not a payload bolted onto the first.
// DerivedStatus is what the route answers against an EMPTY schema; Status here is what the same route
// answers once the canonical dump is restored, and for the id-scoped reads the two genuinely differ
// (api/mushaf/pages/1 derives 404 empty and 200 seeded). Collapsing them into one status with an optional
// body would make one of the two tiers unable to state its own expectation.
internal sealed record SmokeSeededExpectation(HttpStatusCode Status, SmokeSeededPayload Payload);

// Template is what EndpointDataSource reports, constraints included, so a route whose constraint is
// relaxed ({id:int} → {id}) surfaces as a parity mismatch instead of silently passing. Path is the bound
// request the sweep sends, always anonymously.
//
// DerivedStatus is derived by reading the action's own outcome switch against a migrated-but-empty
// schema, never recorded from a run: list reads have no NotFound branch and answer 200 with an empty
// page, id-scoped reads answer 404 because their reader returns null. For the one route that requires
// authentication it is the anonymous rejection; that route's authenticated behaviour is
// SmokeAuthPipelineTests' subject, not the sweep's.
//
// Seeded is null for most entries and is not a gap: see the catalog for which routes carry one and why.
internal sealed record SmokeRoute(
    string Template,
    string Path,
    HttpStatusCode DerivedStatus,
    SmokeRouteAccess? Access = null)
{
    public SmokeRouteAccess Access { get; init; } = Access ?? SmokeRouteAccess.Public;

    public SmokeSeededExpectation? Seeded { get; init; }

    // Defaults to GET so all pre-existing entries stay untouched. A write route sets this explicitly.
    public HttpMethod Method { get; init; } = HttpMethod.Get;

    // The route is catalogued so the parity gate sees it, and is deliberately not dispatched by the
    // generic sweep, because the sweep's premise is that it never writes. Do not "fix" this by
    // dispatching it — a write route run twice by the sweep is the bug, not the missing dispatch.
    public bool ParityOnly { get; init; }
}

internal static class SmokeRouteCatalog
{
    // Bound values come from the formats the handlers themselves enforce: ids 1, word kind `tashkeel`
    // (UniqueWordKindKeys/RootWordKindKeys), grouped dimension kind `roots`
    // (WordTypeGroupedDimensionKindKeys), verse key `1:1` and word location `1:1:1` (the regexes in
    // GetAyahStudyHandler/GetWordAnalysisHandler), page 1.
    //
    // The three word-types detail paths carry `?contextCode=unspecified` because
    // WordTypeRowIdentity.IsValid rejects a blank context code: without it those routes derive 400
    // (InvalidIdentity) and never reach the reader, which is both a shallower sweep and the opposite of
    // how the frontend calls them (word-types.api.ts always sets contextCode).
    //
    // Within each section the rows follow the controller's own declaration order, so a row and its
    // action sit at the same ordinal — which is why roots lists ayahs before words/{wordKind} while
    // lemmas and stems list them the other way round.
    //
    // A Seeded expectation is given to the routes whose seeded answer follows from the artifact rather
    // than from an ordinal. Three groups, twelve routes: the unfiltered reads whose size is a whole table
    // (the four paged list reads, plus api/mushaf/surahs, which takes no key and whose collection length
    // is quran_surahs); the three Mushaf reads addressed by a Quran-stable natural key (page 1, verse 1:1,
    // word 1:1:1); and the four unique-word reads at id 1, whose id is deterministic by construction —
    // migration DeterministicUniqueWordIds drops the
    // identity column, the configuration declares ValueGeneratedNever, and the id IS the quran_words.id of
    // the word's first mushaf occurrence, an equality DisplayWordsSql.CheckUnqIdDeterministicViolations
    // fails the whole import over. Unique tashkeel id 1 is بِسْمِ.
    //
    // The roots/lemmas/stems detail rows are deliberately left without one. Their ids come from a single
    // nextDimId counter shared across all three dimensions (MorphologyAssembler), in first-encounter order
    // across the artifact: reproducible for a given artifact, but an ordinal that shifts when that
    // ordering does. Note that id 1 is absent from quran_lemmas and quran_stems entirely — the shared
    // counter gave it to a root, so those tables start at 2 and 3 — which means /api/words/lemmas/1 and
    // /api/words/stems/1 answer 404 fully seeded, and answer it correctly. That 404 is not recorded as a
    // seeded expectation either: pinning it would pin the very ordinal this paragraph declines to trust.
    public static IReadOnlyList<SmokeRoute> Routes { get; } =
    [
        // api/words/roots — RootsController + RootsController.Details
        new("api/words/roots", "/api/words/roots", HttpStatusCode.OK)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.PagedTable("quran_roots")),
        },
        new("api/words/roots/{id:int}", "/api/words/roots/1", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/ayahs", "/api/words/roots/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/words/{wordKind}", "/api/words/roots/1/words/tashkeel", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/surahs", "/api/words/roots/1/surahs", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/missing-surahs", "/api/words/roots/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/lemmas", "/api/words/roots/1/lemmas", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/stems", "/api/words/roots/1/stems", HttpStatusCode.NotFound),

        // api/words/lemmas — LemmasController
        new("api/words/lemmas", "/api/words/lemmas", HttpStatusCode.OK)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.PagedTable("quran_lemmas")),
        },
        new("api/words/lemmas/{id:int}", "/api/words/lemmas/1", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/words/{wordKind}", "/api/words/lemmas/1/words/tashkeel", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/ayahs", "/api/words/lemmas/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/surahs", "/api/words/lemmas/1/surahs", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/missing-surahs", "/api/words/lemmas/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/stems", "/api/words/lemmas/1/stems", HttpStatusCode.NotFound),

        // api/words/stems — StemsController
        new("api/words/stems", "/api/words/stems", HttpStatusCode.OK)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.PagedTable("quran_stems")),
        },
        new("api/words/stems/{id:int}", "/api/words/stems/1", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/words/{wordKind}", "/api/words/stems/1/words/tashkeel", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/ayahs", "/api/words/stems/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/surahs", "/api/words/stems/1/surahs", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/missing-surahs", "/api/words/stems/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/lemmas", "/api/words/stems/1/lemmas", HttpStatusCode.NotFound),

        // api/words/unique — UniqueWordsController
        new("api/words/unique/{kind}", "/api/words/unique/tashkeel", HttpStatusCode.OK)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.PagedTable("quran_words_unique_tashkeel")),
        },
        new("api/words/unique/{kind}/{id:int}", "/api/words/unique/tashkeel/1", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.PositiveCount("occurrencesCount")),
        },
        new("api/words/unique/{kind}/{id:int}/surahs", "/api/words/unique/tashkeel/1/surahs", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.NonEmptyCollection("surahs")),
        },
        new("api/words/unique/{kind}/{id:int}/missing-surahs", "/api/words/unique/tashkeel/1/missing-surahs", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.NonEmptyCollection("surahs")),
        },
        new("api/words/unique/{kind}/{id:int}/ayahs", "/api/words/unique/tashkeel/1/ayahs", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.NonEmptyPage()),
        },

        // api/words/word-types — WordTypesController + WordTypesController.Details.
        // `.../word-types/words` (list) and `.../word-types/words/{tashkeelWordId:int}` (detail) are
        // distinct templates on the same controller; `.../word-types/table` (list) collides by prefix
        // with the grouped-detail controller's `.../word-types/table/{kind}/{dimensionId:int}` below.
        new("api/words/word-types/tree", "/api/words/word-types/tree", HttpStatusCode.OK),
        new("api/words/word-types/words", "/api/words/word-types/words", HttpStatusCode.OK),
        new("api/words/word-types/table", "/api/words/word-types/table", HttpStatusCode.OK),
        new("api/words/word-types/scope-counts", "/api/words/word-types/scope-counts", HttpStatusCode.OK),
        new("api/words/word-types/words/{tashkeelWordId:int}", "/api/words/word-types/words/1?contextCode=unspecified", HttpStatusCode.NotFound),
        new("api/words/word-types/words/{tashkeelWordId:int}/ayahs", "/api/words/word-types/words/1/ayahs?contextCode=unspecified", HttpStatusCode.NotFound),
        new("api/words/word-types/words/{tashkeelWordId:int}/surahs", "/api/words/word-types/words/1/surahs?contextCode=unspecified", HttpStatusCode.NotFound),

        // api/words/word-types/table — WordTypeGroupedDetailsController
        new("api/words/word-types/table/{kind}/{dimensionId:int}", "/api/words/word-types/table/roots/1", HttpStatusCode.NotFound),
        new("api/words/word-types/table/{kind}/{dimensionId:int}/words", "/api/words/word-types/table/roots/1/words", HttpStatusCode.NotFound),
        new("api/words/word-types/table/{kind}/{dimensionId:int}/ayahs", "/api/words/word-types/table/roots/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/word-types/table/{kind}/{dimensionId:int}/surahs", "/api/words/word-types/table/roots/1/surahs", HttpStatusCode.NotFound),

        // api/mushaf — the reader controllers. The two catalogs answer Ok unconditionally (no outcome
        // switch at all), so they stay 200 even on an empty schema.
        new("api/mushaf/pages/{pageNumber}", "/api/mushaf/pages/1", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.NonEmptyCollection("lines")),
        },
        new("api/mushaf/surahs", "/api/mushaf/surahs", HttpStatusCode.OK)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.CountedCollection("surahs", "quran_surahs")),
        },
        new("api/mushaf/study-sources", "/api/mushaf/study-sources", HttpStatusCode.OK),
        new("api/mushaf/ayahs/{verseKey}/study", "/api/mushaf/ayahs/1:1/study", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.EchoedKey("ayah", "verseKey", "1:1")),
        },
        new("api/mushaf/ayahs/{verseKey}/mutashabihat", "/api/mushaf/ayahs/1:1/mutashabihat", HttpStatusCode.NotFound),
        new("api/mushaf/ayahs/{verseKey}/similar-ayahs", "/api/mushaf/ayahs/1:1/similar-ayahs", HttpStatusCode.NotFound),
        new("api/mushaf/words/{wordLocation}/analysis", "/api/mushaf/words/1:1:1/analysis", HttpStatusCode.NotFound)
        {
            Seeded = new(HttpStatusCode.OK, new SmokeSeededPayload.EchoedKey("word", "wordLocation", "1:1:1")),
        },

        new("api/linking/workspace", "/api/linking/workspace", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/linking/workspace/sources", "/api/linking/workspace/sources", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/linking/workspace/sources/{id:long}", "/api/linking/workspace/sources/1", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Delete,
            ParityOnly = true,
        },
        new("api/linking/workspace/sources/order", "/api/linking/workspace/sources/order", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Put,
            ParityOnly = true,
        },
        new("api/linking/workspace/sources", "/api/linking/workspace/sources", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Delete,
            ParityOnly = true,
        },
        new("api/linking/sources/resolve-page", "/api/linking/sources/resolve-page", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/linking/workspace/sources/{id:long}/configuration", "/api/linking/workspace/sources/1/configuration", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Patch,
            ParityOnly = true,
        },
        new("api/linking/preflights", "/api/linking/preflights", HttpStatusCode.Accepted, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/linking/preflights/{preflightId:guid}", "/api/linking/preflights/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/linking/preflights/{preflightId:guid}", "/api/linking/preflights/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Delete,
            ParityOnly = true,
        },
        new("api/linking/preflights/{preflightId:guid}/confirmation-jobs", "/api/linking/preflights/00000000-0000-0000-0000-000000000001/confirmation-jobs", HttpStatusCode.Accepted, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/linking/confirmation-jobs/{jobId:guid}", "/api/linking/confirmation-jobs/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/linking/confirmation-jobs/{jobId:guid}", "/api/linking/confirmation-jobs/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Delete,
            ParityOnly = true,
        },
        new("api/linking/confirmation-outcomes/{idempotencyKey:guid}", "/api/linking/confirmation-outcomes/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/linking/preflights/{preflightId:guid}/sources/{preparedSourceId:long}/ayahs", "/api/linking/preflights/00000000-0000-0000-0000-000000000001/sources/1/ayahs", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/linking/preflights/{preflightId:guid}/merged-ayahs", "/api/linking/preflights/00000000-0000-0000-0000-000000000001/merged-ayahs", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },

        // System routes. Health answers 503 when the container-backed check fails, so 200 is real
        // evidence; access/me is the tree's only [Authorize] endpoint (AccessController class level).
        new("api/health", "/api/health", HttpStatusCode.OK),
        new("api/dashboard/info", "/api/dashboard/info", HttpStatusCode.OK),
        new("api/access/me", "/api/access/me", HttpStatusCode.Unauthorized, SmokeRouteAccess.AuthenticatedOnly),
        new("api/access/users", "/api/access/users", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}", "/api/access/users/1", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/accept", "/api/access/users/1/accept", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/disable", "/api/access/users/1/disable", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/reactivate", "/api/access/users/1/reactivate", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/access/permissions", "/api/access/permissions", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/permissions", "/api/access/users/1/permissions", HttpStatusCode.NotFound, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/permissions", "/api/access/users/1/permissions", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Put,
            ParityOnly = true,
        },
        new("api/access/audit-events", "/api/access/audit-events", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/logto-sub/relink/preview", "/api/access/users/1/logto-sub/relink/preview", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/access/users/{userId:int}/logto-sub/relink/confirm", "/api/access/users/1/logto-sub/relink/confirm", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            Method = HttpMethod.Post,
            ParityOnly = true,
        },
        new("api/access/owner-reconciliation/status", "/api/access/owner-reconciliation/status", HttpStatusCode.OK, SmokeRouteAccess.OwnerOnly)
        {
            ParityOnly = true,
        },

        // api/abwab/sections — AbwabSectionsController. ParityOnly: these write, so the generic sweep
        // (which sends no body and shares the migrated-but-empty schema across every other case) must
        // not dispatch them — see SmokeAbwabWriteTests for their dedicated coverage. DerivedStatus
        // documents what a well-formed call answers against that empty schema, for reference only.
        new("api/abwab/sections", "/api/abwab/sections", HttpStatusCode.Created)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Sections.Create),
            ParityOnly = true,
        },
        new("api/abwab/sections/{id:int}", "/api/abwab/sections/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Put,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Sections.Edit),
            ParityOnly = true,
        },
        new("api/abwab/sections/{id:int}", "/api/abwab/sections/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Delete,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Sections.Delete),
            ParityOnly = true,
        },
        new("api/abwab/sections/{id:int}/order", "/api/abwab/sections/1/order", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Sections.Reorder),
            ParityOnly = true,
        },

        // api/abwab/doors — AbwabDoorsController. Same ParityOnly rationale as sections above.
        new("api/abwab/doors", "/api/abwab/doors", HttpStatusCode.Created)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Create),
            ParityOnly = true,
        },
        new("api/abwab/doors/{id:int}", "/api/abwab/doors/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Put,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Edit),
            ParityOnly = true,
        },
        new("api/abwab/doors/{id:int}/move", "/api/abwab/doors/1/move", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Move),
            ParityOnly = true,
        },
        // abwab-global-order: the body gained a required Scope (Section | Global). DerivedStatus is
        // unchanged at 404 — ParityOnly routes are never dispatched by the sweep (SmokeRoutePipelineTests
        // excludes them), so this value is documentation, not an assertion the sweep runs. "Well-formed"
        // now means "carries a valid scope"; door 1 still doesn't exist against the empty schema either
        // way, so the derived status stays 404, not 400 — confirmed, not assumed (plan §7 T208).
        new("api/abwab/doors/{id:int}/order", "/api/abwab/doors/1/order", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Reorder),
            ParityOnly = true,
        },
        new("api/abwab/doors/bulk-move", "/api/abwab/doors/bulk-move", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Move),
            ParityOnly = true,
        },
        new("api/abwab/doors/bulk-archive", "/api/abwab/doors/bulk-archive", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Archive),
            ParityOnly = true,
        },
        new("api/abwab/doors/{id:int}", "/api/abwab/doors/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Delete,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Archive),
            ParityOnly = true,
        },
        new("api/abwab/doors/{id:int}/restore", "/api/abwab/doors/1/restore", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Doors.Restore),
            ParityOnly = true,
        },

        // api/abwab/doors/{doorId:int}/relations and api/abwab/relations/{relationId:int} —
        // AbwabDoorRelationsController. The GET is ParityOnly despite being a safe read, unlike the
        // mushaf {verseKey} reads: SmokeAbwabWriteTests creates doors in this same shared schema, so a
        // dispatched /api/abwab/doors/1/relations would answer 200 rather than the derived 404 whenever
        // a created door holds id 1. The two writes are ParityOnly for the sibling routes' own reason.
        new("api/abwab/doors/{doorId:int}/relations", "/api/abwab/doors/1/relations", HttpStatusCode.NotFound)
        {
            ParityOnly = true,
        },
        new("api/abwab/doors/{doorId:int}/relations", "/api/abwab/doors/1/relations", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Relations.Create),
            ParityOnly = true,
        },
        new("api/abwab/relations/{relationId:int}", "/api/abwab/relations/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Delete,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Relations.Delete),
            ParityOnly = true,
        },

        // abwab-relations: the response contract gained AbwabTreeDoorDto.RelationCount. DerivedStatus is
        // unchanged at 200 — re-checked, not assumed: the handler still has no NotFound branch, so an
        // empty schema answers 200 with an empty snapshot exactly as before, and the new field is a
        // live-only count that is simply 0 there.
        //
        // api/abwab/tree — AbwabTreeController. Unlike its sibling write routes, this one IS dispatched
        // by the generic sweep: GetAbwabTreeHandler has no NotFound branch at all, so it derives 200 with
        // an empty snapshot against the migrated-but-empty schema regardless of what any other test left
        // behind — order-independent by construction, not by convention like the write routes above.
        new("api/abwab/tree", "/api/abwab/tree", HttpStatusCode.OK),

        // api/abwab/templates — AbwabTemplatesController. BOTH reads are ParityOnly, unlike
        // api/abwab/tree above. The tree read is order-independent by construction; these are not.
        // The templates workshop is the first feature whose own future smoke tests would create
        // templates in this shared schema, at which point a dispatched list would answer 200 with a
        // non-empty body and /templates/1 could flip from 404 to 200 — the same order-dependence
        // that keeps the relations GET undispatched. DerivedStatus documents what a well-formed call
        // answers against the empty schema; it is not an assertion that the sweep runs.
        new("api/abwab/templates", "/api/abwab/templates", HttpStatusCode.OK)
        {
            ParityOnly = true,
        },
        new("api/abwab/templates/{templateId:int}", "/api/abwab/templates/1", HttpStatusCode.NotFound)
        {
            ParityOnly = true,
        },
        new("api/abwab/templates", "/api/abwab/templates", HttpStatusCode.Created)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Templates.Create),
            ParityOnly = true,
        },
        new("api/abwab/templates/{templateId:int}", "/api/abwab/templates/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Delete,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Templates.Delete),
            ParityOnly = true,
        },

        // api/abwab/templates/{templateId:int}/nodes and api/abwab/template-nodes/{nodeId:int} —
        // AbwabTemplateNodesController. ParityOnly for the sibling write routes' own reason: these
        // write, so the generic sweep must not dispatch them.
        new("api/abwab/templates/{templateId:int}/nodes", "/api/abwab/templates/1/nodes", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.TemplateNodes.Create),
            ParityOnly = true,
        },
        new("api/abwab/template-nodes/{nodeId:int}", "/api/abwab/template-nodes/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Put,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.TemplateNodes.Edit),
            ParityOnly = true,
        },
        new("api/abwab/template-nodes/{nodeId:int}/order", "/api/abwab/template-nodes/1/order", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.TemplateNodes.Reorder),
            ParityOnly = true,
        },
        new("api/abwab/template-nodes/{nodeId:int}", "/api/abwab/template-nodes/1", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Delete,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.TemplateNodes.Delete),
            ParityOnly = true,
        },
        new("api/abwab/templates/{templateId:int}/apply", "/api/abwab/templates/1/apply", HttpStatusCode.NotFound)
        {
            Method = HttpMethod.Post,
            Access = SmokeRouteAccess.Permission(AbwabPermissions.Templates.Apply),
            ParityOnly = true,
        },
    ];

    // The sweep's theory data is the Path alone (a string is serializable, so every route is an
    // individually addressable test case); this resolves it back to its entry. Scoped to non-ParityOnly
    // entries because write routes legitimately share a Path across methods (PUT/DELETE on the same
    // {id} route) — the loud-duplicate guarantee still holds within the set the sweep actually dispatches.
    public static SmokeRoute ByPath(string path) =>
        Routes.Single(route => !route.ParityOnly && route.Path == path);
}
