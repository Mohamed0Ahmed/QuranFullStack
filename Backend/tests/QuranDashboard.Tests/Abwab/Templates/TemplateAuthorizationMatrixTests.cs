using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Abwab.Templates;
using QuranDashboard.Api.Security.Authorization;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Infrastructure.Security.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

// §5.2 / FR-018 demand more than a correctly spelled attribute: they demand that a PARTIAL grant is
// actually refused at runtime. TemplatePermissionOwnershipTests proves each endpoint declares the
// frozen policy; this one runs the REAL policy set, the REAL PermissionAuthorizationHandler, and the
// REAL EffectivePermissionResolver over real granted rows, so "template.add-only cannot add a node"
// is an observed denial rather than an inference from a policy name.
[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateAuthorizationMatrixTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly string[] TemplateCodes =
    [
        PermissionCatalogue.TemplateView,
        PermissionCatalogue.TemplateAdd,
        PermissionCatalogue.TemplateEdit,
        PermissionCatalogue.TemplateDelete,
        PermissionCatalogue.TemplateRestore,
        PermissionCatalogue.TemplateApply,
    ];

    public Task InitializeAsync() => SecurityTestHarness.ResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EachSingleTemplateGrant_AuthorizesOnlyItsOwnEndpoints_AndIsRefusedOnEveryOther()
    {
        var endpoints = TemplateEndpointPolicies();
        endpoints.Should().HaveCount(17, "the matrix must cover every template endpoint, not a subset");

        foreach (var grantedCode in TemplateCodes)
        {
            var subject = $"holder-of-{grantedCode}";
            await GrantAsync(subject, grantedCode);

            var allowed = new List<string>();
            var refused = new List<string>();
            foreach (var (action, policy) in endpoints)
            {
                var authorized = await AuthorizeAsync(subject, policy);
                (authorized ? allowed : refused).Add(action);
            }

            allowed.Should().BeEquivalentTo(
                endpoints.Where(e => e.Policy == grantedCode).Select(e => e.Action),
                $"holding only {grantedCode} must authorize exactly the endpoints §5.2 assigns to it");
            refused.Should().NotBeEmpty($"{grantedCode} must not authorize the whole template surface");
        }
    }

    [Fact]
    public async Task TemplateAddAlone_CannotAddANodeOrAnAlias_NorApplyTheTemplate()
    {
        const string subject = "add-only";
        await GrantAsync(subject, PermissionCatalogue.TemplateAdd);

        (await AuthorizeAsync(subject, PolicyFor(nameof(TemplatesController.Add))))
            .Should().BeTrue("template.add creates the aggregate");

        foreach (var action in new[]
        {
            nameof(TemplatesController.AddNode),
            nameof(TemplatesController.AddNodeAlias),
            nameof(TemplatesController.ReorderNodes),
            nameof(TemplatesController.Apply),
        })
        {
            (await AuthorizeAsync(subject, PolicyFor(action)))
                .Should().BeFalse($"template.add grants nothing beyond creating the aggregate — {action} must be refused");
        }
    }

    [Fact]
    public async Task TemplateEditAlone_CannotDeleteRestoreOrApplyTheAggregate()
    {
        const string subject = "edit-only";
        await GrantAsync(subject, PermissionCatalogue.TemplateEdit);

        (await AuthorizeAsync(subject, PolicyFor(nameof(TemplatesController.AddNode))))
            .Should().BeTrue("node internals are template.edit-owned");

        foreach (var action in new[]
        {
            nameof(TemplatesController.Delete),
            nameof(TemplatesController.Restore),
            nameof(TemplatesController.Apply),
        })
        {
            (await AuthorizeAsync(subject, PolicyFor(action)))
                .Should().BeFalse($"a partial grant may not borrow another verb — {action} must be refused");
        }
    }

    [Fact]
    public async Task ASubjectHoldingNoTemplateGrant_IsRefusedEveryTemplateEndpoint()
    {
        const string subject = "no-grants";

        foreach (var (action, policy) in TemplateEndpointPolicies())
        {
            (await AuthorizeAsync(subject, policy))
                .Should().BeFalse($"{action} must be refused when no template permission is held");
        }
    }

    [Fact]
    public async Task AnUnauthenticatedPrincipal_IsRefusedEvenWhenTheGrantExistsForThatSubject()
    {
        const string subject = "granted-but-anonymous";
        await GrantAsync(subject, PermissionCatalogue.TemplateApply);

        await using var db = SecurityTestHarness.CreateContext(fixture);
        await using var provider = BuildAuthorization(subject, db);
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        (await authorization.AuthorizeAsync(anonymous, PermissionCatalogue.TemplateApply)).Succeeded
            .Should().BeFalse("every template policy requires an authenticated user before the grant is even consulted");
    }

    private async Task<bool> AuthorizeAsync(string subject, string policy)
    {
        await using var db = SecurityTestHarness.CreateContext(fixture);
        await using var provider = BuildAuthorization(subject, db);

        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, subject)], "test"));

        return (await authorization.AuthorizeAsync(principal, policy)).Succeeded;
    }

    // The production registration helpers are used verbatim: a policy this feature forgot to register,
    // or a handler dropped from the composition root, fails here instead of passing on a stub.
    private static ServiceProvider BuildAuthorization(string subject, QuranDashboardDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(PermissionAuthorizationRegistration.AddPermissionPolicies);
        services.AddPermissionAuthorizationHandlers();
        services.AddScoped<ICurrentUser>(_ => new FixedCurrentUser(subject));
        services.AddScoped<ISystemOwnerStore>(_ => new SystemOwnerStore(db));
        services.AddScoped(_ => SecurityTestHarness.BuildResolver(db, new SecurityTestHarness.RecordingCache()));

        return services.BuildServiceProvider();
    }

    private Task GrantAsync(string subject, string permissionCode) =>
        SecurityTestHarness.GrantAsync(
            fixture,
            new GrantPermissionCommand(
                PermissionTargetKind.Subject,
                subject,
                permissionCode,
                ExpectedTimelineGeneration.Of(0),
                ExpectedVersion: 0,
                ActorSubject: "tester"));

    private static string PolicyFor(string action) =>
        TemplateEndpointPolicies().Single(endpoint => endpoint.Action == action).Policy;

    // Derived from the controller, never listed: a new endpoint joins the matrix automatically rather
    // than slipping past a hand-maintained table.
    private static IReadOnlyList<(string Action, string Policy)> TemplateEndpointPolicies() =>
        [.. typeof(TemplatesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(action => (
                Action: action.Name,
                Policy: action.GetCustomAttributes<AuthorizeAttribute>().Single().Policy!))];

    private sealed class FixedCurrentUser(string sub) : ICurrentUser
    {
        public string Sub { get; } = sub;
    }
}
