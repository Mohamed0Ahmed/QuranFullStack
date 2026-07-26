using Microsoft.AspNetCore.Mvc.ApiExplorer;
using QuranDashboard.Api.Extensions;

namespace QuranDashboard.Tests.Abwab.Ci;

internal static class ApiContractSources
{
    public const string GeneratedContractRelativePath = "Frontend/quran-dashboard-ui/openapi/swagger.json";

    public static HashSet<string> ReadLiveEndpoints()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);
        services.AddRouting();

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Select(description => Endpoint(description.HttpMethod ?? "GET", "/" + (description.RelativePath ?? string.Empty)))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static HashSet<string> ReadCommittedEndpoints(JsonDocument document) =>
        document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => Endpoint(operation.Name, path.Name)))
            .ToHashSet(StringComparer.Ordinal);

    public static JsonDocument ReadGeneratedContract() =>
        JsonDocument.Parse(File.ReadAllText(RepositoryPath(GeneratedContractRelativePath)));

    public static string RepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not resolve '{relativePath}' by searching upward from {AppContext.BaseDirectory}.");
    }

    private static string Endpoint(string method, string path) => $"{method.ToUpperInvariant()} {path.TrimEnd('/')}";
}
