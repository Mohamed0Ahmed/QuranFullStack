using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Abwab.Templates;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Application.Abwab.Templates;

namespace QuranDashboard.Tests.Abwab.Templates;

public sealed class NoCreateFromRealDoorTests
{
    private static readonly Regex CreateFromRealDoorNamePattern = new(
        "(?i)(createfrom|fromcategory|fromdoor|fromexisting|importcategory|importdoor|clone|duplicate|copytemplate|copynode)",
        RegexOptions.Compiled);

    private static readonly string[] TemplateSourceRoots =
    [
        Path.Combine("application", "QuranDashboard.Application.Abstractions", "Abwab", "Templates"),
        Path.Combine("application", "QuranDashboard.Application", "Abwab", "Templates"),
        Path.Combine("api", "QuranDashboard.Api", "Abwab", "Templates"),
    ];

    // The one legitimate direction is template -> real category (application). Anything reading real
    // categories INTO a template would have to reach one of these from an editor type.
    private const string ApplicationHandlerFileName = "TemplateApplicationHandler.cs";

    private static readonly string[] ForbiddenCategorySurfaces =
    [
        "ICategoryTreeStore",
        "ICategorySearchAliasWriteStore",
        "ISectionWriteStore",
        "IAbwabCoreReadPort",
    ];

    [Fact]
    public void NoTemplateCommandOrRequestOrEndpoint_IsNamedForACreateFromRealDoorOrCopyPath()
    {
        var offenders = TemplateSurfaceTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(member => !member.Name.StartsWith('<'))
                .Select(member => $"{type.Name}.{member.Name}")
                .Prepend(type.Name))
            .Where(name => CreateFromRealDoorNamePattern.IsMatch(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "§7.4 forbids any create-from-real-door and any cross-door copy path; offenders: " + string.Join(", ", offenders));
    }

    [Fact]
    public void NoTemplateEditorHandler_DependsOnTheCategoryStores()
    {
        var editorHandlers = TemplateEditorHandlers();
        editorHandlers.Should().NotBeEmpty("the editor-handler set is derived, so it can never silently become empty");

        var offenders = editorHandlers
            .SelectMany(handler => handler.GetConstructors().SelectMany(c => c.GetParameters()))
            .Where(parameter => ForbiddenCategorySurfaces.Any(surface =>
                (parameter.ParameterType.FullName ?? string.Empty).Contains(surface, StringComparison.Ordinal)))
            .Select(parameter => $"{parameter.Member.DeclaringType!.Name}({parameter.ParameterType.Name})")
            .ToList();

        offenders.Should().BeEmpty(
            "template editor CRUD may never read real categories; only the application handler touches them, "
            + "and only to WRITE through the 029 writer; offenders: " + string.Join(", ", offenders));
    }

    [Fact]
    public void OnlyTheApplicationHandler_ReferencesTheCategoryWriteSurface()
    {
        var sourceFiles = TemplateSourceFiles().ToList();
        sourceFiles.Should().NotBeEmpty("the source scan must actually read the template sources, not pass vacuously");

        var offenders = sourceFiles
            .Where(file => !string.Equals(Path.GetFileName(file), ApplicationHandlerFileName, StringComparison.Ordinal))
            .Where(file => ForbiddenCategorySurfaces.Any(surface =>
                File.ReadAllText(file).Contains(surface, StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty(
            "no template source file other than the application handler may reach a real-category surface; offenders: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void NoTemplateCommand_CarriesASecondTemplateOrACategorySourceForNodeCreation()
    {
        var nodeCreationCommands = new[] { typeof(AddTemplateNodeCommand), typeof(ReparentTemplateNodeCommand), typeof(ReorderTemplateNodesCommand) };

        foreach (var command in nodeCreationCommands)
        {
            var guidCarryingNames = command.GetProperties()
                .Where(p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?))
                .Select(p => p.Name)
                .ToList();

            guidCarryingNames.Should().NotContain(
                name => name.Contains("Category", StringComparison.Ordinal),
                $"{command.Name} must not accept a real category as node content (§7.4)");
            guidCarryingNames.Count(name => name.Contains("DoorTemplate", StringComparison.Ordinal)).Should().BeLessThanOrEqualTo(1,
                $"{command.Name} must reference at most ONE template, so no node can be copied across doors");
        }
    }

    [Fact]
    public void TheOnlyTemplateEndpointAcceptingACategory_IsTheApplyEndpoint()
    {
        var actionsAcceptingACategory = typeof(TemplatesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(action => action.GetParameters()
                .Any(parameter => ReferencesACategory(parameter.ParameterType) ||
                    parameter.Name!.Contains("categoryId", StringComparison.OrdinalIgnoreCase)))
            .Select(action => action.Name)
            .ToList();

        actionsAcceptingACategory.Should().BeEquivalentTo([nameof(TemplatesController.Apply)],
            "application to one target category is the ONLY template endpoint that may name a real category");
    }

    [Fact]
    public void TheApplyEndpoint_RequiresTemplateApplyAlone()
    {
        var apply = typeof(TemplatesController).GetMethod(nameof(TemplatesController.Apply))!;

        apply.GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .Should().BeEquivalentTo([QuranDashboard.Domain.Security.Permissions.PermissionCatalogue.TemplateApply]);
    }

    private static bool ReferencesACategory(Type type) =>
        type.GetProperties().Any(property => property.Name.Contains("Category", StringComparison.Ordinal));

    private static IEnumerable<Type> TemplateSurfaceTypes() =>
        new[]
        {
            typeof(AddDoorTemplateCommand).Assembly,
            typeof(TemplateAggregateHandler).Assembly,
            typeof(TemplatesController).Assembly,
        }
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => (type.Namespace ?? string.Empty).EndsWith(".Abwab.Templates", StringComparison.Ordinal));

    // Derived, never listed: a FOURTH editor handler added later is covered automatically, because
    // the only exemption is the one handler §7.4 allows to touch real categories.
    private static IReadOnlyList<Type> TemplateEditorHandlers() =>
        [.. typeof(TemplateAggregateHandler).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(TemplateAggregateHandler).Namespace)
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .Where(type => type != typeof(TemplateApplicationHandler))];

    private static IEnumerable<string> TemplateSourceFiles()
    {
        var resolved = TemplateSourceRoots.Select(root => Path.Combine(BackendRoot(), root)).ToList();

        // A renamed root must fail loudly: silently skipping it would leave this absence gate scanning
        // less than it claims while the other roots keep it green.
        resolved.Where(root => !Directory.Exists(root)).Should().BeEmpty(
            "every declared template source root must exist so this gate actually visits it");

        return resolved.SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories));
    }

    private static string BackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Backend");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not resolve the Backend root from {AppContext.BaseDirectory}.");
    }
}
