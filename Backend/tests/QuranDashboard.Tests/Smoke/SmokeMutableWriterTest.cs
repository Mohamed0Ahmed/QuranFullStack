using System.Net.Http.Headers;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Smoke;

public abstract class SmokeMutableWriterTest(AccessTestFixture fixture) : IAsyncLifetime
{
    protected AccessTestFixture Fixture { get; } = fixture;

    protected FakeExternalUserProfileSource ProfileSource => Fixture.ProfileSource;

    private protected TestSqlCommandCapture CommandCapture => Fixture.CommandCapture;

    protected IServiceProvider ApiServices => Fixture.ApiServices;

    public Task InitializeAsync() => Fixture.BeginScenarioAsync(
        additionalOwnerEmails: [FakeExternalUserProfileSource.EmailFor(SmokePersonas.OwnerSub)]);

    public Task DisposeAsync() => Fixture.EndScenarioAsync();

    protected HttpClient CreateClient() => Fixture.CreateApiClient();

    private protected HttpClient CreateClientFor(SmokePersona persona)
    {
        var client = CreateClient();
        if (persona is SmokePersona.InvalidToken)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokens.Mint(SmokePersonas.SubFor(persona)!, signingKey: TestJwtTokens.DifferentKey));
            return client;
        }

        if (SmokePersonas.SubFor(persona) is { } sub)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                TestJwtTokens.Mint(sub, additionalClaims: SmokePersonas.ClaimsFor(persona)));

            if (persona is SmokePersona.Owner)
            {
                client.DefaultRequestHeaders.Add(
                    "X-Interactive-Identity-Evidence",
                    TestJwtTokens.MintIdentityToken(
                        sub,
                        FakeExternalUserProfileSource.EmailFor(sub),
                        true));
            }
        }

        return client;
    }

    protected async Task SeedAuthorizationPersonasAsync(
        string exactPermissionCode,
        string neighboringPermissionCode)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissions = await db.AccessPermissions
            .Where(permission => permission.Code == exactPermissionCode
                                 || permission.Code == neighboringPermissionCode)
            .ToDictionaryAsync(permission => permission.Code, permission => permission.Id);
        var ownerRoleId = await db.AccessRoles.AsNoTracking()
            .Where(role => role.Name == RoleNames.Owner)
            .Select(role => role.Id)
            .SingleAsync();
        var normalizer = scope.ServiceProvider.GetRequiredService<IEmailIdentityNormalizer>();
        var users = new Dictionary<SmokePersona, User>();

        foreach (var persona in SmokePersonas.All.Where(persona => persona is not (
                     SmokePersona.Anonymous or SmokePersona.InvalidToken or SmokePersona.AuthenticatedUnknown)))
        {
            var definition = SmokePersonas.DefinitionFor(persona);
            var user = definition.BuildUser(definition.IsOwner ? ownerRoleId : null);
            user.NormalizedEmail = normalizer.Normalize(user.Email);
            users.Add(persona, user);
            db.AccessUsers.Add(user);
        }

        await db.SaveChangesAsync();
        AddGrant(SmokePersona.ExactPermission, exactPermissionCode);
        AddGrant(SmokePersona.NeighboringPermission, neighboringPermissionCode);
        AddGrant(SmokePersona.Disabled, exactPermissionCode);
        await db.SaveChangesAsync();

        void AddGrant(SmokePersona persona, string permissionCode)
        {
            var user = users[persona];
            db.AccessUserPermissions.Add(new UserPermission
            {
                UserId = user.Id,
                PermissionId = permissions[permissionCode],
                GrantedByUserId = user.Id,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
        }
    }

    private protected async Task<IReadOnlyList<string>> GetDirectPermissionCodesAsync(SmokePersona persona)
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var sub = SmokePersonas.SubFor(persona)
            ?? throw new InvalidOperationException($"{persona} does not have a local subject.");

        return await db.AccessUserPermissions.AsNoTracking()
            .Where(grant => grant.User.LogtoSub == sub)
            .Select(grant => grant.Permission.Code)
            .OrderBy(code => code)
            .ToListAsync();
    }

    private protected async Task<SmokeAbwabWriteState> GetAbwabWriteStateAsync()
    {
        await using var scope = Fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new SmokeAbwabWriteState(
            await db.AbwabSections.CountAsync(),
            await db.AbwabDoors.CountAsync(),
            await db.AbwabDoorAliases.CountAsync(),
            await db.AbwabDoorRelations.CountAsync(),
            await db.AbwabDoorInclusions.CountAsync(),
            await db.AbwabDoorInclusionUnitSyncs.CountAsync(),
            await db.LinkingSourceContributions.CountAsync(),
            await db.LinkingSourceContributionUnits.CountAsync(),
            await db.LinkingUnits.CountAsync(),
            await db.AbwabTemplates.CountAsync(),
            await db.AbwabTemplateNodes.CountAsync());
    }
}

internal sealed record SmokeAbwabWriteState(
    int Sections,
    int Doors,
    int DoorAliases,
    int DoorRelations,
    int DoorInclusions,
    int DoorInclusionUnitSyncs,
    int LinkingSourceContributions,
    int LinkingSourceContributionUnits,
    int LinkingUnits,
    int Templates,
    int TemplateNodes);
