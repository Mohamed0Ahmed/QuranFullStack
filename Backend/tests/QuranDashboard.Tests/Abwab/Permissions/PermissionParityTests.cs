using System.Text.RegularExpressions;
using QuranDashboard.Api.Security.Authorization;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.Permissions;

[Collection(nameof(AbwabDbCollection))]
public sealed class PermissionParityTests(PostgresFixture fixture)
{
    private static readonly HashSet<string> Expected = new(StringComparer.Ordinal)
    {
        "attribution.view",
        "attribution.manage",
        "permission.administer",
        "audit.restore",
        "safetyPoint.manage",
        "section.view",
        "section.add",
        "section.edit",
        "section.reorder",
        "section.delete",
        "category.view",
        "category.add",
        "category.edit",
        "category.move",
        "category.reorder",
        "category.delete",
        "protection.view",
        "protection.apply",
        "protection.lift",
        "relationship.view",
        "relationship.add",
        "relationship.edit",
        "relationship.delete",
        "relationship.restore",
    };

    [Fact]
    public void Catalogue_MatchesTheIndependentExpectation()
    {
        PermissionCatalogue.Codes.Should().BeEquivalentTo(Expected);
    }

    [Fact]
    public async Task Seed_Catalogue_MatchesTheExpectation()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT code FROM permission_codes";

        var seeded = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            seeded.Add(reader.GetString(0));
        }

        seeded.Should().BeEquivalentTo(Expected);
    }

    [Fact]
    public void Policy_Catalogue_MatchesTheExpectation()
    {
        PermissionAuthorizationRegistration.PermissionPolicyNames.Should().BeEquivalentTo(Expected);
    }

    [Fact]
    public void MeProjection_Catalogue_MatchesTheExpectation()
    {
        var meUniverse = PermissionCatalogue.SystemOwnerOnlyCodes
            .Concat(PermissionCatalogue.All.Where(entry => entry.IsAssignable).Select(entry => entry.Code))
            .ToHashSet(StringComparer.Ordinal);

        meUniverse.Should().BeEquivalentTo(Expected);
    }

    [Fact]
    public void Frontend_Catalogue_MatchesTheExpectation()
    {
        var path = FrontendPermissionCodesPath();
        File.Exists(path).Should().BeTrue($"frontend permission-codes.ts must exist at {path}");

        var content = File.ReadAllText(path);
        var codes = Regex.Matches(content, "'([a-zA-Z][a-zA-Z.]*\\.[a-zA-Z]+)'")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        codes.Should().BeEquivalentTo(Expected);
    }

    private static string FrontendPermissionCodesPath()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        return Path.Combine(
            repoRoot, "Frontend", "quran-dashboard-ui", "src", "app", "core", "auth", "permission-codes.ts");
    }
}
