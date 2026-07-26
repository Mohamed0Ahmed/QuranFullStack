using QuranDashboard.Tests.Smoke._Support;

namespace QuranDashboard.Tests.Smoke.Pipeline;

internal enum SmokeAuthKind
{
    Anonymous,
    TreeRedacted,
    Authenticated,
    Permission,
    SystemOwner,
}

internal sealed record SmokeAuth(SmokeAuthKind Kind, string? PermissionCode = null)
{
    public static readonly SmokeAuth Anonymous = new(SmokeAuthKind.Anonymous);
    public static readonly SmokeAuth TreeRedacted = new(SmokeAuthKind.TreeRedacted);
    public static readonly SmokeAuth Authenticated = new(SmokeAuthKind.Authenticated);
    public static readonly SmokeAuth SystemOwner = new(SmokeAuthKind.SystemOwner);
}

internal sealed record SmokeRouteCase(
    HttpMethod Method,
    string PathTemplate,
    Func<SmokeSeedContext, string> ConcretePath,
    SmokeAuth Auth,
    Func<SmokeSeedContext, string>? ValidBody = null,
    Func<SmokeSeedContext, string>? InvalidBody = null)
{
    public string Key => $"{Method.Method.ToUpperInvariant()} /{PathTemplate}";
}

// Bodies are lambdas because seeded ids exist only at seed time; nothing here may touch
// SmokeSeed.Context during static initialization — the parity gate enumerates Keys with no DB.
// Enums are numeric in JSON (the API registers no JsonStringEnumConverter).
internal static class SmokeRouteCatalog
{
    private static readonly Guid UnseededGuid = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static IReadOnlyDictionary<string, SmokeRouteCase> Cases { get; } =
        BuildCases().ToDictionary(routeCase => routeCase.Key, StringComparer.Ordinal);

    private static List<SmokeRouteCase> BuildCases() =>
    [
        // ---- misc ----
        AnonymousGet("api/health"),
        AnonymousGet("api/dashboard/info"),

        // ---- mushaf reader (all anonymous GET) ----
        AnonymousGet("api/mushaf/pages/{pageNumber}", "api/mushaf/pages/1"),
        AnonymousGet("api/mushaf/surahs"),
        AnonymousGet("api/mushaf/study-sources"),
        AnonymousGet("api/mushaf/ayahs/{verseKey}/study", "api/mushaf/ayahs/1:1/study"),
        AnonymousGet("api/mushaf/ayahs/{verseKey}/similar-ayahs", "api/mushaf/ayahs/1:1/similar-ayahs"),
        AnonymousGet("api/mushaf/ayahs/{verseKey}/mutashabihat", "api/mushaf/ayahs/2:6/mutashabihat"),
        AnonymousGet("api/mushaf/words/{wordLocation}/analysis", "api/mushaf/words/1:1:1/analysis"),

        // ---- words: roots ----
        AnonymousGet("api/words/roots"),
        AnonymousGet("api/words/roots/{id}", "api/words/roots/1"),
        AnonymousGet("api/words/roots/{id}/ayahs", "api/words/roots/1/ayahs"),
        AnonymousGet("api/words/roots/{id}/words/{wordKind}", "api/words/roots/1/words/tashkeel"),
        AnonymousGet("api/words/roots/{id}/surahs", "api/words/roots/1/surahs"),
        AnonymousGet("api/words/roots/{id}/missing-surahs", "api/words/roots/1/missing-surahs"),
        AnonymousGet("api/words/roots/{id}/lemmas", "api/words/roots/1/lemmas"),
        AnonymousGet("api/words/roots/{id}/stems", "api/words/roots/1/stems"),

        // ---- words: lemmas ----
        AnonymousGet("api/words/lemmas"),
        AnonymousGet("api/words/lemmas/{id}", "api/words/lemmas/1"),
        AnonymousGet("api/words/lemmas/{id}/words/{wordKind}", "api/words/lemmas/1/words/tashkeel"),
        AnonymousGet("api/words/lemmas/{id}/ayahs", "api/words/lemmas/1/ayahs"),
        AnonymousGet("api/words/lemmas/{id}/surahs", "api/words/lemmas/1/surahs"),
        AnonymousGet("api/words/lemmas/{id}/missing-surahs", "api/words/lemmas/1/missing-surahs"),
        AnonymousGet("api/words/lemmas/{id}/stems", "api/words/lemmas/1/stems"),

        // ---- words: stems ----
        AnonymousGet("api/words/stems"),
        AnonymousGet("api/words/stems/{id}", "api/words/stems/1"),
        AnonymousGet("api/words/stems/{id}/words/{wordKind}", "api/words/stems/1/words/tashkeel"),
        AnonymousGet("api/words/stems/{id}/ayahs", "api/words/stems/1/ayahs"),
        AnonymousGet("api/words/stems/{id}/surahs", "api/words/stems/1/surahs"),
        AnonymousGet("api/words/stems/{id}/missing-surahs", "api/words/stems/1/missing-surahs"),
        AnonymousGet("api/words/stems/{id}/lemmas", "api/words/stems/1/lemmas"),

        // ---- words: unique ----
        AnonymousGet("api/words/unique/{kind}", "api/words/unique/tashkeel"),
        AnonymousGet("api/words/unique/{kind}/{id}", "api/words/unique/tashkeel/1"),
        AnonymousGet("api/words/unique/{kind}/{id}/surahs", "api/words/unique/tashkeel/1/surahs"),
        AnonymousGet("api/words/unique/{kind}/{id}/missing-surahs", "api/words/unique/tashkeel/1/missing-surahs"),
        AnonymousGet("api/words/unique/{kind}/{id}/ayahs", "api/words/unique/tashkeel/1/ayahs"),

        // ---- words: word types ----
        AnonymousGet("api/words/word-types/tree"),
        AnonymousGet("api/words/word-types/words"),
        AnonymousGet("api/words/word-types/table"),
        AnonymousGet("api/words/word-types/scope-counts"),
        AnonymousGet("api/words/word-types/words/{tashkeelWordId}", "api/words/word-types/words/1"),
        AnonymousGet("api/words/word-types/words/{tashkeelWordId}/ayahs", "api/words/word-types/words/1/ayahs"),
        AnonymousGet("api/words/word-types/words/{tashkeelWordId}/surahs", "api/words/word-types/words/1/surahs"),
        AnonymousGet("api/words/word-types/table/{kind}/{dimensionId}", "api/words/word-types/table/roots/1"),
        AnonymousGet("api/words/word-types/table/{kind}/{dimensionId}/words", "api/words/word-types/table/roots/1/words"),
        AnonymousGet("api/words/word-types/table/{kind}/{dimensionId}/ayahs", "api/words/word-types/table/roots/1/ayahs"),
        AnonymousGet("api/words/word-types/table/{kind}/{dimensionId}/surahs", "api/words/word-types/table/roots/1/surahs"),

        // ---- access / security ----
        new(HttpMethod.Get, "api/access/me", _ => "api/access/me", SmokeAuth.Authenticated),
        new(HttpMethod.Get, "api/security/permissions", _ => "api/security/permissions", SmokeAuth.SystemOwner),
        new(HttpMethod.Post, "api/security/permissions/grant", _ => "api/security/permissions/grant", SmokeAuth.SystemOwner,
            ValidBody: _ => """{"targetKind":"Subject","targetKey":"smoke-unused","permissionCode":"attribution.view","expectedTimelineGeneration":0,"expectedVersion":0}""",
            InvalidBody: _ => """{"targetKey":"smoke-unused","permissionCode":"attribution.view","expectedTimelineGeneration":0,"expectedVersion":0}"""),
        new(HttpMethod.Post, "api/security/permissions/revoke", _ => "api/security/permissions/revoke", SmokeAuth.SystemOwner,
            ValidBody: _ => """{"targetKind":"Subject","targetKey":"smoke-unused","permissionCode":"attribution.view","expectedTimelineGeneration":0,"expectedVersion":0}""",
            InvalidBody: _ => """{"targetKey":"smoke-unused","permissionCode":"attribution.view","expectedTimelineGeneration":0,"expectedVersion":0}"""),

        // ---- abwab: tree (anonymous/unpermissioned callers get an in-body 403) ----
        new(HttpMethod.Get, "api/abwab/tree", _ => "api/abwab/tree", SmokeAuth.TreeRedacted),
        new(HttpMethod.Get, "api/abwab/tree/search", _ => "api/abwab/tree/search?query=باب", SmokeAuth.TreeRedacted),

        // ---- abwab: sections ----
        new(HttpMethod.Post, "api/abwab/sections", _ => "api/abwab/sections", Permission("section.add"),
            ValidBody: _ => """{"name":"قسم جديد","expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedTreeRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Put, "api/abwab/sections/{sectionId}", ctx => $"api/abwab/sections/{ctx.SectionId}", Permission("section.edit"),
            ValidBody: _ => """{"name":"قسم معدل","expectedVersion":1,"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedVersion":1,"expectedTreeRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Post, "api/abwab/sections/reorder", _ => "api/abwab/sections/reorder", Permission("section.reorder"),
            ValidBody: ctx => $$"""{"orders":[{"sectionId":"{{ctx.SectionId}}","sortOrder":0,"expectedVersion":1}],"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedTreeRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Delete, "api/abwab/sections/{sectionId}", ctx => $"api/abwab/sections/{ctx.ExpendableSectionId}", Permission("section.delete"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),

        // ---- abwab: categories ----
        new(HttpMethod.Post, "api/abwab/categories", ctx => "api/abwab/categories", Permission("category.add"),
            ValidBody: ctx => $$"""{"name":"باب جديد","description":null,"representativeQuranExcerpt":null,"parentCategoryId":null,"sectionId":"{{ctx.SectionId}}","expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: ctx => $$"""{"description":null,"representativeQuranExcerpt":null,"parentCategoryId":null,"sectionId":"{{ctx.SectionId}}","expectedTreeRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Put, "api/abwab/categories/{categoryId}", ctx => $"api/abwab/categories/{ctx.RootCategoryId}", Permission("category.edit"),
            ValidBody: _ => """{"name":"باب معدل","description":null,"representativeQuranExcerpt":null,"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"description":null,"representativeQuranExcerpt":null,"expectedVersion":1,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Post, "api/abwab/categories/move", _ => "api/abwab/categories/move", Permission("category.move"),
            ValidBody: ctx => $$"""{"moves":[{"categoryId":"{{ctx.ChildCategoryId}}","newParentCategoryId":null,"newSectionId":"{{ctx.SectionId}}","expectedVersion":1}],"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedTreeRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Post, "api/abwab/categories/reorder", ctx => "api/abwab/categories/reorder", Permission("category.reorder"),
            ValidBody: ctx => $$"""{"scope":2,"parentCategoryId":null,"sectionId":null,"orders":[{"categoryId":"{{ctx.RootCategoryId}}","newOrder":0,"expectedVersion":1}],"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"scope":2,"parentCategoryId":null,"sectionId":null,"expectedTreeRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Post, "api/abwab/categories/{categoryId}/subtree-delete", ctx => $"api/abwab/categories/{ctx.ExpendableCategoryId}/subtree-delete", Permission("category.delete"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/categories/operation-restore/{deletionOperationId}", _ => $"api/abwab/categories/operation-restore/{UnseededGuid}", Permission("category.delete"),
            ValidBody: _ => """{"expectedTreeRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/categories/{categoryId}/aliases", ctx => $"api/abwab/categories/{ctx.RootCategoryId}/aliases", Permission("category.edit"),
            ValidBody: _ => """{"value":"مرادف جديد","expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Put, "api/abwab/categories/aliases/{aliasId}", ctx => $"api/abwab/categories/aliases/{ctx.AliasId}", Permission("category.edit"),
            ValidBody: _ => """{"value":"مرادف معدل","expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Delete, "api/abwab/categories/aliases/{aliasId}", ctx => $"api/abwab/categories/aliases/{ctx.AliasId}", Permission("category.edit"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),

        // ---- abwab: relationships ----
        new(HttpMethod.Get, "api/abwab/relationships/{categoryId}", ctx => $"api/abwab/relationships/{ctx.RootCategoryId}", Permission("relationship.view")),
        new(HttpMethod.Post, "api/abwab/relationships", _ => "api/abwab/relationships", Permission("relationship.add"),
            ValidBody: ctx => $$"""{"relationshipType":0,"firstCategoryId":"{{ctx.RootCategoryId}}","secondCategoryId":"{{ctx.ExpendableCategoryId}}","expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Put, "api/abwab/relationships/{relationshipId}", ctx => $"api/abwab/relationships/{ctx.RelationshipId}", Permission("relationship.edit"),
            ValidBody: ctx => $$"""{"relationshipType":1,"firstCategoryId":"{{ctx.RootCategoryId}}","secondCategoryId":"{{ctx.ChildCategoryId}}","expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Delete, "api/abwab/relationships/{relationshipId}", ctx => $"api/abwab/relationships/{ctx.RelationshipId}", Permission("relationship.delete"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/relationships/{relationshipId}/restore", ctx => $"api/abwab/relationships/{ctx.RelationshipId}/restore", Permission("relationship.restore"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),

        // ---- abwab: templates ----
        new(HttpMethod.Get, "api/abwab/templates", _ => "api/abwab/templates", Permission("template.view")),
        new(HttpMethod.Get, "api/abwab/templates/{doorTemplateId}", ctx => $"api/abwab/templates/{ctx.TemplateId}", Permission("template.view")),
        new(HttpMethod.Get, "api/abwab/templates/{doorTemplateId}/history", ctx => $"api/abwab/templates/{ctx.TemplateId}/history", Permission("template.view")),
        new(HttpMethod.Post, "api/abwab/templates", _ => "api/abwab/templates", Permission("template.add"),
            ValidBody: _ => """{"name":"قالب جديد","description":null,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"description":null,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Put, "api/abwab/templates/{doorTemplateId}", ctx => $"api/abwab/templates/{ctx.TemplateId}", Permission("template.edit"),
            ValidBody: _ => """{"name":"قالب معدل","description":null,"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"description":null,"expectedVersion":1,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Delete, "api/abwab/templates/{doorTemplateId}", ctx => $"api/abwab/templates/{ctx.ExpendableTemplateId}", Permission("template.delete"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/templates/{doorTemplateId}/restore", ctx => $"api/abwab/templates/{ctx.ExpendableTemplateId}/restore", Permission("template.restore"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/templates/{doorTemplateId}/nodes", ctx => $"api/abwab/templates/{ctx.TemplateId}/nodes", Permission("template.edit"),
            ValidBody: _ => """{"parentTemplateNodeId":null,"name":"عقدة جديدة","representativeQuranExcerpt":null,"description":null,"expectedTemplateRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"parentTemplateNodeId":null,"representativeQuranExcerpt":null,"description":null,"expectedTemplateRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Put, "api/abwab/templates/nodes/{templateNodeId}", ctx => $"api/abwab/templates/nodes/{ctx.NodeId}", Permission("template.edit"),
            ValidBody: _ => """{"name":"عقدة معدلة","representativeQuranExcerpt":null,"description":null,"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"representativeQuranExcerpt":null,"description":null,"expectedVersion":1,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Post, "api/abwab/templates/nodes/{templateNodeId}/reparent", ctx => $"api/abwab/templates/nodes/{ctx.NodeId}/reparent", Permission("template.edit"),
            ValidBody: _ => """{"newParentTemplateNodeId":null,"expectedVersion":1,"expectedTemplateRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/templates/{doorTemplateId}/nodes/reorder", ctx => $"api/abwab/templates/{ctx.TemplateId}/nodes/reorder", Permission("template.edit"),
            ValidBody: ctx => $$"""{"parentTemplateNodeId":null,"orderedTemplateNodeIds":["{{ctx.NodeId}}"],"expectedTemplateRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"parentTemplateNodeId":null,"expectedTemplateRevision":0,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Delete, "api/abwab/templates/nodes/{templateNodeId}", ctx => $"api/abwab/templates/nodes/{ctx.NodeId}", Permission("template.edit"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTemplateRevision":0,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/templates/nodes/{templateNodeId}/aliases", ctx => $"api/abwab/templates/nodes/{ctx.NodeId}/aliases", Permission("template.edit"),
            ValidBody: _ => """{"value":"مرادف جديد","expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Put, "api/abwab/templates/aliases/{templateNodeSearchAliasId}", ctx => $"api/abwab/templates/aliases/{ctx.NodeAliasId}", Permission("template.edit"),
            ValidBody: _ => """{"value":"مرادف معدل","expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}"""),
        new(HttpMethod.Delete, "api/abwab/templates/aliases/{templateNodeSearchAliasId}", ctx => $"api/abwab/templates/aliases/{ctx.NodeAliasId}", Permission("template.edit"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/templates/aliases/{templateNodeSearchAliasId}/restore", ctx => $"api/abwab/templates/aliases/{ctx.NodeAliasId}/restore", Permission("template.edit"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/templates/{doorTemplateId}/apply", ctx => $"api/abwab/templates/{ctx.TemplateId}/apply", Permission("template.apply"),
            ValidBody: ctx => $$"""{"targetCategoryId":"{{ctx.ExpendableCategoryId}}","expectedTemplateRevision":0,"expectedTreeRevision":0,"expectedTargetVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),

        // ---- abwab: protection ----
        new(HttpMethod.Get, "api/abwab/protection/{categoryId}", ctx => $"api/abwab/protection/{ctx.RootCategoryId}", Permission("protection.view")),
        new(HttpMethod.Post, "api/abwab/protection/{categoryId}/{protectionType}/apply", ctx => $"api/abwab/protection/{ctx.RootCategoryId}/QuranContent/apply", Permission("protection.apply"),
            ValidBody: _ => """{"scope":0,"expectedVersion":null,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/protection/{categoryId}/{protectionType}/lift", ctx => $"api/abwab/protection/{ctx.RootCategoryId}/QuranContent/lift", Permission("protection.lift"),
            ValidBody: _ => """{"expectedVersion":1,"expectedTimelineGeneration":0}""",
            InvalidBody: _ => "{"),
        new(HttpMethod.Post, "api/abwab/protection/{categoryId}/full-preset", ctx => $"api/abwab/protection/{ctx.RootCategoryId}/full-preset", Permission("protection.apply"),
            ValidBody: _ => """{"scope":0,"expectedVersions":{},"expectedTimelineGeneration":0}""",
            InvalidBody: _ => """{"scope":0,"expectedTimelineGeneration":0}"""),
    ];

    private static SmokeAuth Permission(string code) => new(SmokeAuthKind.Permission, code);

    private static SmokeRouteCase AnonymousGet(string template, string? concretePath = null) =>
        new(HttpMethod.Get, template, _ => concretePath ?? template, SmokeAuth.Anonymous);
}
