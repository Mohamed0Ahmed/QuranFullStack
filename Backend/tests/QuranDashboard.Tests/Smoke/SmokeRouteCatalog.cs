namespace QuranDashboard.Tests.Smoke;

// What a route requires of its caller, not who the caller is — SmokePersona is the caller side, and
// naming both ends "Anonymous" would put two different meanings on one word in one namespace.
internal enum SmokeRouteAccess
{
    Open,
    RequiresAuthentication,
}

// Template is what EndpointDataSource reports, constraints included, so a route whose constraint is
// relaxed ({id:int} → {id}) surfaces as a parity mismatch instead of silently passing. Path is the bound
// request the sweep sends, always anonymously.
//
// DerivedStatus is derived by reading the action's own outcome switch against a migrated-but-empty
// schema, never recorded from a run: list reads have no NotFound branch and answer 200 with an empty
// page, id-scoped reads answer 404 because their reader returns null. For the one route that requires
// authentication it is the anonymous rejection; that route's authenticated behaviour is
// SmokeAuthPipelineTests' subject, not the sweep's.
internal sealed record SmokeRoute(
    string Template,
    string Path,
    HttpStatusCode DerivedStatus,
    SmokeRouteAccess Access = SmokeRouteAccess.Open);

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
    public static IReadOnlyList<SmokeRoute> Routes { get; } =
    [
        // api/words/roots — RootsController + RootsController.Details
        new("api/words/roots", "/api/words/roots", HttpStatusCode.OK),
        new("api/words/roots/{id:int}", "/api/words/roots/1", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/ayahs", "/api/words/roots/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/words/{wordKind}", "/api/words/roots/1/words/tashkeel", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/surahs", "/api/words/roots/1/surahs", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/missing-surahs", "/api/words/roots/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/lemmas", "/api/words/roots/1/lemmas", HttpStatusCode.NotFound),
        new("api/words/roots/{id:int}/stems", "/api/words/roots/1/stems", HttpStatusCode.NotFound),

        // api/words/lemmas — LemmasController
        new("api/words/lemmas", "/api/words/lemmas", HttpStatusCode.OK),
        new("api/words/lemmas/{id:int}", "/api/words/lemmas/1", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/words/{wordKind}", "/api/words/lemmas/1/words/tashkeel", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/ayahs", "/api/words/lemmas/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/surahs", "/api/words/lemmas/1/surahs", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/missing-surahs", "/api/words/lemmas/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/lemmas/{id:int}/stems", "/api/words/lemmas/1/stems", HttpStatusCode.NotFound),

        // api/words/stems — StemsController
        new("api/words/stems", "/api/words/stems", HttpStatusCode.OK),
        new("api/words/stems/{id:int}", "/api/words/stems/1", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/words/{wordKind}", "/api/words/stems/1/words/tashkeel", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/ayahs", "/api/words/stems/1/ayahs", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/surahs", "/api/words/stems/1/surahs", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/missing-surahs", "/api/words/stems/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/stems/{id:int}/lemmas", "/api/words/stems/1/lemmas", HttpStatusCode.NotFound),

        // api/words/unique — UniqueWordsController
        new("api/words/unique/{kind}", "/api/words/unique/tashkeel", HttpStatusCode.OK),
        new("api/words/unique/{kind}/{id:int}", "/api/words/unique/tashkeel/1", HttpStatusCode.NotFound),
        new("api/words/unique/{kind}/{id:int}/surahs", "/api/words/unique/tashkeel/1/surahs", HttpStatusCode.NotFound),
        new("api/words/unique/{kind}/{id:int}/missing-surahs", "/api/words/unique/tashkeel/1/missing-surahs", HttpStatusCode.NotFound),
        new("api/words/unique/{kind}/{id:int}/ayahs", "/api/words/unique/tashkeel/1/ayahs", HttpStatusCode.NotFound),

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
        new("api/mushaf/pages/{pageNumber}", "/api/mushaf/pages/1", HttpStatusCode.NotFound),
        new("api/mushaf/surahs", "/api/mushaf/surahs", HttpStatusCode.OK),
        new("api/mushaf/study-sources", "/api/mushaf/study-sources", HttpStatusCode.OK),
        new("api/mushaf/ayahs/{verseKey}/study", "/api/mushaf/ayahs/1:1/study", HttpStatusCode.NotFound),
        new("api/mushaf/ayahs/{verseKey}/mutashabihat", "/api/mushaf/ayahs/1:1/mutashabihat", HttpStatusCode.NotFound),
        new("api/mushaf/ayahs/{verseKey}/similar-ayahs", "/api/mushaf/ayahs/1:1/similar-ayahs", HttpStatusCode.NotFound),
        new("api/mushaf/words/{wordLocation}/analysis", "/api/mushaf/words/1:1:1/analysis", HttpStatusCode.NotFound),

        // System routes. Health answers 503 when the container-backed check fails, so 200 is real
        // evidence; access/me is the tree's only [Authorize] endpoint (AccessController class level).
        new("api/health", "/api/health", HttpStatusCode.OK),
        new("api/dashboard/info", "/api/dashboard/info", HttpStatusCode.OK),
        new("api/access/me", "/api/access/me", HttpStatusCode.Unauthorized, SmokeRouteAccess.RequiresAuthentication),
    ];

    // The sweep's theory data is the Path alone (a string is serializable, so every route is an
    // individually addressable test case); this resolves it back to its entry. Single rather than a
    // dictionary lookup so a duplicated Path fails loudly instead of silently shadowing an entry.
    public static SmokeRoute ByPath(string path) =>
        Routes.Single(route => route.Path == path);
}
