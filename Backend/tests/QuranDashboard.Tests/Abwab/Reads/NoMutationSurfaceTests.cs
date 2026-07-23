using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Abwab.Tree;

namespace QuranDashboard.Tests.Abwab.Reads;

public sealed class NoMutationSurfaceTests
{
    private static readonly Type[] MutationVerbAttributes =
    [
        typeof(HttpPostAttribute),
        typeof(HttpPutAttribute),
        typeof(HttpPatchAttribute),
        typeof(HttpDeleteAttribute),
    ];

    [Fact]
    public void NoAbwabController_ExposesAMutationVerbAction()
    {
        var abwabControllers = typeof(AbwabTreeController).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.Namespace is not null && t.Namespace.StartsWith("QuranDashboard.Api.Abwab", StringComparison.Ordinal))
            .ToList();

        abwabControllers.Should().NotBeEmpty("the tree read endpoints must exist at this stage");

        var mutationActions = abwabControllers
            .SelectMany(controller => controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes().Any(attr => MutationVerbAttributes.Contains(attr.GetType())))
            .ToList();

        mutationActions.Should().BeEmpty(
            "no section/category mutation endpoint may exist at the Story 1 checkpoint; found: "
            + string.Join(", ", mutationActions.Select(m => $"{m.DeclaringType!.Name}.{m.Name}")));
    }

    [Fact]
    public void AbwabTreeController_OnlyExposesReadActions()
    {
        var actions = typeof(AbwabTreeController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        actions.Should().NotBeEmpty();
        actions.Should().OnlyContain(
            method => method.GetCustomAttribute<HttpGetAttribute>() != null,
            "the Story 1 tree controller exposes read/search/snapshot only");
    }

    [Fact]
    public void NoAbwabCategoryOrSectionCommandHandler_ExistsYet()
    {
        var applicationAssembly = typeof(AbwabTreeController).Assembly
            .GetReferencedAssemblies()
            .Single(a => a.Name == "QuranDashboard.Application");

        var loaded = Assembly.Load(applicationAssembly);

        var commandHandlers = loaded.GetTypes()
            .Where(t => t.Namespace is not null &&
                (t.Namespace.StartsWith("QuranDashboard.Application.Abwab.Sections", StringComparison.Ordinal) ||
                 t.Namespace.StartsWith("QuranDashboard.Application.Abwab.Categories", StringComparison.Ordinal)))
            .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal))
            .ToList();

        commandHandlers.Should().BeEmpty(
            "Story 1 is read/search/snapshot only; no section/category writer handler may exist yet");
    }
}
