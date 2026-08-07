using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.Access;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Smoke;

public sealed class SmokeApiFixture : IAsyncLifetime
{
    private readonly object _apiFactoryLock = new();
    private PostgreSqlDatabaseLease? _databaseLease;
    private WebApplicationFactory<HealthController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    // Shared as a singleton so its call counters survive across requests.
    public FakeExternalUserProfileSource ProfileSource { get; } = new();

    internal SmokeSqlCommandCapture CommandCapture { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(SmokeApiFixture));
        ConnectionString = _databaseLease.ConnectionString;

        try
        {
            _queryProvider = BuildQueryProvider();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        // The lease is released in the finally so a failing host or provider disposal cannot strand a
        // leased database on the shared server for the rest of the test process.
        try
        {
            if (_apiFactory is not null)
            {
                await _apiFactory.DisposeAsync();
                _apiFactory = null;
            }

            if (_queryProvider is not null)
            {
                await _queryProvider.DisposeAsync();
                _queryProvider = null;
            }
        }
        finally
        {
            if (_databaseLease is not null)
            {
                await _databaseLease.DisposeAsync();
                _databaseLease = null;
            }
        }
    }

    public HttpClient CreateClient() => SmokeApiHost.CreateClient(Factory);

    internal HttpClient CreateClientFor(SmokePersona persona)
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
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
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

    internal async Task<HttpClient> CreateActiveOwnerClientAsync()
    {
        var client = CreateClientFor(SmokePersona.Owner);
        using var response = await client.GetAsync("/api/access/me");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            client.Dispose();
            throw new InvalidOperationException("The smoke Owner could not be provisioned.");
        }

        return client;
    }

    public IServiceProvider ApiServices => Factory.Services;

    public async Task ResetAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE users, abwab_sections, abwab_doors, abwab_door_aliases, abwab_door_relations, "
            + "abwab_templates, abwab_template_nodes RESTART IDENTITY CASCADE;");
        ProfileSource.Reset();
        InvalidateAbwabCaches();
    }

    // Through the host's own services, never a second provider: the generation counter that decides
    // whether a cached tree is stale is a singleton of the container serving the requests.
    private void InvalidateAbwabCaches()
    {
        using var scope = ApiServices.CreateScope();
        var caches = scope.ServiceProvider.GetRequiredService<IAbwabCacheInvalidator>();
        caches.InvalidateTree();
        caches.InvalidateTemplates();
    }

    // Reads via an independent DbContext, never the pipeline's own instance (test isolation).
    public async Task<User?> GetUserBySubAsync(string logtoSub)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUsers.AsNoTracking().SingleOrDefaultAsync(u => u.LogtoSub == logtoSub);
    }

    internal async Task SeedAuthorizationPersonasAsync(
        string exactPermissionCode,
        string neighboringPermissionCode)
    {
        await using (var apiScope = ApiServices.CreateAsyncScope())
        {
            await apiScope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(CancellationToken.None);
        }

        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var normalizer = scope.ServiceProvider.GetRequiredService<IEmailIdentityNormalizer>();
        var ownerRoleId = await db.AccessRoles.AsNoTracking()
            .Where(role => role.Name == RoleNames.Owner)
            .Select(role => role.Id)
            .SingleAsync();
        var users = new Dictionary<SmokePersona, User>();

        foreach (var persona in SmokePersonas.All.Where(persona => persona is not (
                     SmokePersona.Anonymous or SmokePersona.InvalidToken or SmokePersona.AuthenticatedUnknown)))
        {
            var definition = SmokePersonas.DefinitionFor(persona);
            int? roleId = definition.IsOwner ? ownerRoleId : null;
            var user = definition.BuildUser(roleId);
            user.NormalizedEmail = normalizer.Normalize(user.Email);
            users.Add(persona, user);
            db.AccessUsers.Add(user);
        }

        await db.SaveChangesAsync();

        var permissionIds = await db.AccessPermissions
            .Where(permission => permission.Code == exactPermissionCode || permission.Code == neighboringPermissionCode)
            .ToDictionaryAsync(permission => permission.Code, permission => permission.Id);
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
                PermissionId = permissionIds[permissionCode],
                GrantedByUserId = user.Id,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
        }
    }

    internal async Task<IReadOnlyList<string>> GetDirectPermissionCodesAsync(SmokePersona persona)
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var sub = SmokePersonas.SubFor(persona)
            ?? throw new InvalidOperationException($"{persona} does not have a local subject.");

        return await db.AccessUserPermissions.AsNoTracking()
            .Where(grant => grant.User.LogtoSub == sub)
            .Select(grant => grant.Permission.Code)
            .OrderBy(code => code)
            .ToListAsync();
    }

    internal async Task<SmokeAbwabWriteState> GetAbwabWriteStateAsync()
    {
        await using var scope = QueryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        return new SmokeAbwabWriteState(
            await db.AbwabSections.CountAsync(),
            await db.AbwabDoors.CountAsync(),
            await db.AbwabDoorAliases.CountAsync(),
            await db.AbwabDoorRelations.CountAsync(),
            await db.AbwabTemplates.CountAsync(),
            await db.AbwabTemplateNodes.CountAsync());
    }

    private ServiceProvider QueryProvider => _queryProvider
        ?? throw new InvalidOperationException(
            $"{nameof(SmokeApiFixture)} has not been initialized. Ensure it is used as an ICollectionFixture.");

    private WebApplicationFactory<HealthController> Factory
    {
        get
        {
            // Guard the lazy init so concurrent callers reuse the single factory instead of racing to
            // construct (and leak) multiple WebApplicationFactory instances.
            lock (_apiFactoryLock)
            {
                return _apiFactory ??= SmokeApiHost.Build(ConnectionString, ProfileSource, CommandCapture);
            }
        }
    }

    private ServiceProvider BuildQueryProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .AddSingleton<IEmailIdentityNormalizer, EmailIdentityNormalizer>()
            .BuildServiceProvider();
    }
}

internal sealed record SmokeAbwabWriteState(
    int Sections,
    int Doors,
    int DoorAliases,
    int DoorRelations,
    int Templates,
    int TemplateNodes);
