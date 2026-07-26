using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke._Fixtures;

namespace QuranDashboard.Tests.Smoke._Support;

public sealed record SmokeSeedContext(
    Guid SectionId,
    Guid RootCategoryId,
    Guid ChildCategoryId,
    Guid AliasId,
    Guid RelationshipId,
    Guid TemplateId,
    Guid NodeId,
    Guid NodeAliasId,
    Guid ExpendableSectionId,
    Guid ExpendableCategoryId,
    Guid ExpendableTemplateId);

internal static class SmokeSeed
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string? _seededFor;

    public static SmokeSeedContext Context { get; private set; } = null!;

    public static async Task EnsureSeededAsync(SmokeApiFixture fixture)
    {
        await Gate.WaitAsync();
        try
        {
            if (_seededFor == fixture.ConnectionString)
            {
                return;
            }

            await ResetAsync(fixture.ConnectionString);
            Context = await SeedAsync(fixture.ConnectionString);
            _seededFor = fixture.ConnectionString;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task ResetAsync(string connectionString)
    {
        await AbwabSubstrateReset.FullResetAsync(connectionString);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "TRUNCATE users RESTART IDENTITY CASCADE";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<SmokeSeedContext> SeedAsync(string connectionString)
    {
        await SecurityTestHarness.BootstrapOwnerAsync(connectionString, new BootstrapSystemOwnerCommand(
            Issuer: TestJwtTokens.TestIssuer,
            Subject: SmokePersonas.Owner,
            ExpectedIssuer: TestJwtTokens.TestIssuer,
            EmailVerified: true,
            AccountEnabled: true,
            ExpectedTimelineGeneration: ExpectedTimelineGeneration.Of(0),
            ActorSubject: SmokePersonas.Owner));

        await InsertUsersAsync(connectionString);

        foreach (var entry in PermissionCatalogue.All.Where(entry => !entry.SystemOwnerOnly))
        {
            await SecurityTestHarness.GrantAsync(connectionString, new GrantPermissionCommand(
                TargetKind: PermissionTargetKind.Subject,
                TargetKey: SmokePersonas.Granted,
                PermissionCode: entry.Code,
                ExpectedTimelineGeneration: ExpectedTimelineGeneration.Of(0),
                ExpectedVersion: 0,
                ActorSubject: SmokePersonas.Owner));
        }

        return await InsertAbwabDataAsync(connectionString);
    }

    private static async Task InsertUsersAsync(string connectionString)
    {
        await using var db = SecurityTestHarness.CreateContext(connectionString);
        var now = DateTimeOffset.UnixEpoch;

        db.AccessUsers.AddRange(
            new User
            {
                LogtoSub = SmokePersonas.Owner,
                Email = SmokePersonas.OwnerEmail,
                Status = UserStatus.Active,
                RoleId = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            new User
            {
                LogtoSub = SmokePersonas.Granted,
                Email = SmokePersonas.GrantedEmail,
                Status = UserStatus.Active,
                RoleId = null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            new User
            {
                LogtoSub = SmokePersonas.NoPermissions,
                Email = SmokePersonas.NoPermissionsEmail,
                Status = UserStatus.Active,
                RoleId = null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });

        await db.SaveChangesAsync();
    }

    private static async Task<SmokeSeedContext> InsertAbwabDataAsync(string connectionString)
    {
        var section = AbwabTreeSeeding.NewSection("قسم الدخان");
        var root = AbwabTreeSeeding.NewRootCategory("باب الدخان", section.SectionId, sectionOrder: 0, globalOrder: 0);
        var child = AbwabTreeSeeding.NewChildCategory("باب فرعي", root, siblingOrder: 0);
        var alias = AbwabTreeSeeding.NewAlias(root.CategoryId, "مرادف الدخان");
        var relationship = AbwabRelationshipTemplateSeeding.NewMutualRelationship(
            root.CategoryId.CompareTo(child.CategoryId) < 0 ? root.CategoryId : child.CategoryId,
            root.CategoryId.CompareTo(child.CategoryId) < 0 ? child.CategoryId : root.CategoryId);

        var template = AbwabRelationshipTemplateSeeding.NewDoorTemplate("قالب الدخان");
        var node = AbwabRelationshipTemplateSeeding.NewTemplateNode(template.DoorTemplateId, "عقدة الدخان", siblingOrder: 0);
        var nodeAlias = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(node.TemplateNodeId, "مرادف العقدة");

        var expendableSection = AbwabTreeSeeding.NewSection("قسم مستهلك", sortOrder: 1);
        var expendableCategory = AbwabTreeSeeding.NewRootCategory(
            "باب مستهلك", expendableSection.SectionId, sectionOrder: 0, globalOrder: 1);
        var expendableTemplate = AbwabRelationshipTemplateSeeding.NewDoorTemplate("قالب مستهلك");

        await AbwabTreeSeeding.InsertAsync(connectionString, section, root, child, alias, relationship);
        await AbwabTreeSeeding.InsertAsync(connectionString, template, node, nodeAlias);
        await AbwabTreeSeeding.InsertAsync(connectionString, expendableSection, expendableCategory, expendableTemplate);

        return new SmokeSeedContext(
            SectionId: section.SectionId,
            RootCategoryId: root.CategoryId,
            ChildCategoryId: child.CategoryId,
            AliasId: alias.CategorySearchAliasId,
            RelationshipId: relationship.CategoryRelationshipId,
            TemplateId: template.DoorTemplateId,
            NodeId: node.TemplateNodeId,
            NodeAliasId: nodeAlias.TemplateNodeSearchAliasId,
            ExpendableSectionId: expendableSection.SectionId,
            ExpendableCategoryId: expendableCategory.CategoryId,
            ExpendableTemplateId: expendableTemplate.DoorTemplateId);
    }
}
