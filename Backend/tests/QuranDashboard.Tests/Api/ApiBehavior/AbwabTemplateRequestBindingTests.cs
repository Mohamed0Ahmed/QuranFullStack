using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using QuranDashboard.Api.Abwab.Templates;
using QuranDashboard.Application.Abstractions.Abwab.Templates;

namespace QuranDashboard.Tests.Api.ApiBehavior;

// Every template write request record is a positional record, and MVC throws InvalidOperationException
// during validation when validation metadata sits on a record PROPERTY whose name matches a primary
// constructor parameter — turning every one of these endpoints into a 500 before the handler is ever
// reached. Reflection over the contract types cannot catch that; only the real MVC pipeline can, so
// these tests drive routing, authorization, model binding and validation end to end.
public sealed class AbwabTemplateRequestBindingTests
{
    private static readonly Guid TemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NodeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AliasId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static TheoryData<string, HttpMethod, string, object> WriteRequests() => new()
    {
        {
            nameof(TemplatesController.Add), HttpMethod.Post, "/api/abwab/templates",
            new { name = "قالب", description = (string?)null, expectedTimelineGeneration = 0L }
        },
        {
            nameof(TemplatesController.Edit), HttpMethod.Put, $"/api/abwab/templates/{TemplateId}",
            new { name = "قالب", description = (string?)null, expectedVersion = 1u, expectedTimelineGeneration = 0L }
        },
        {
            nameof(TemplatesController.AddNode), HttpMethod.Post, $"/api/abwab/templates/{TemplateId}/nodes",
            new
            {
                parentTemplateNodeId = (Guid?)null,
                name = "عقدة",
                representativeQuranExcerpt = (string?)null,
                description = (string?)null,
                expectedTemplateRevision = 0L,
                expectedTimelineGeneration = 0L,
            }
        },
        {
            nameof(TemplatesController.EditNode), HttpMethod.Put, $"/api/abwab/templates/nodes/{NodeId}",
            new
            {
                name = "عقدة",
                representativeQuranExcerpt = (string?)null,
                description = (string?)null,
                expectedVersion = 1u,
                expectedTimelineGeneration = 0L,
            }
        },
        {
            nameof(TemplatesController.ReorderNodes), HttpMethod.Post, $"/api/abwab/templates/{TemplateId}/nodes/reorder",
            new
            {
                parentTemplateNodeId = (Guid?)null,
                orderedTemplateNodeIds = new[] { NodeId },
                expectedTemplateRevision = 0L,
                expectedTimelineGeneration = 0L,
            }
        },
        {
            nameof(TemplatesController.AddNodeAlias), HttpMethod.Post, $"/api/abwab/templates/nodes/{NodeId}/aliases",
            new { value = "مرادف", expectedTimelineGeneration = 0L }
        },
        {
            nameof(TemplatesController.EditNodeAlias), HttpMethod.Put, $"/api/abwab/templates/aliases/{AliasId}",
            new { value = "مرادف", expectedVersion = 1u, expectedTimelineGeneration = 0L }
        },
    };

    [Theory]
    [MemberData(nameof(WriteRequests))]
    public async Task ValidBody_BindsThroughTheRealPipeline_AndReachesTheHandler(
        string action, HttpMethod method, string route, object body)
    {
        using var factory = new TemplateBindingApiFactory();
        using var client = factory.CreateApiClient();

        using var request = new HttpRequestMessage(method, route) { Content = JsonContent.Create(body) };
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"{action} must bind its request contract and reach the write port, not fail validation metadata");
        factory.WritePort.Invocations.Should().ContainSingle();
    }

    [Fact]
    public async Task MissingRequiredName_Returns400_FromTheFrameworkConvention_WithoutReachingTheHandler()
    {
        using var factory = new TemplateBindingApiFactory();
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync(
            "/api/abwab/templates",
            new { description = (string?)null, expectedTimelineGeneration = 0L });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.WritePort.Invocations.Should().BeEmpty();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("message").GetString().Should().Be(ApiMessages.ValidationFailed);
        document.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().Contain(error => error!.StartsWith("name:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DuplicatedReorderId_Returns400_FromTheContractsOwnValidation()
    {
        using var factory = new TemplateBindingApiFactory();
        using var client = factory.CreateApiClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/abwab/templates/{TemplateId}/nodes/reorder",
            new
            {
                parentTemplateNodeId = (Guid?)null,
                orderedTemplateNodeIds = new[] { NodeId, NodeId },
                expectedTemplateRevision = 0L,
                expectedTimelineGeneration = 0L,
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.WritePort.Invocations.Should().BeEmpty();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("errors").EnumerateArray()
            .Select(error => error.GetString())
            .Should().Contain(error => error!.Contains(ApiMessages.AbwabTemplateReorderMalformed, StringComparison.Ordinal));
    }

    private sealed class TemplateBindingApiFactory : WebApplicationFactory<TemplatesController>
    {
        public RecordingTemplateWritePort WritePort { get; } = new();

        public HttpClient CreateApiClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:QuranDashboardDb"] =
                        "Host=localhost;Port=5432;Database=template_binding_tests;Username=none;Password=none",
                    ["Auth:Authority"] = "https://test-issuer.example/oidc",
                    ["Auth:Audience"] = "https://test-api.example/resource",
                    ["Cors:AllowedOrigins:0"] = "https://localhost",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAbwabTemplateWritePort>();
                services.AddSingleton<IAbwabTemplateWritePort>(WritePort);

                services.RemoveAll<IClaimsTransformation>();
                services.RemoveAll<IAuthorizationPolicyProvider>();
                services.AddSingleton<IAuthorizationPolicyProvider, AuthenticatedOnlyPolicyProvider>();

                services.AddAuthentication(SubjectAuthenticationHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, SubjectAuthenticationHandler>(
                        SubjectAuthenticationHandler.SchemeName, _ => { });
            });
        }
    }

    // The permission policies themselves are proven by TemplateAuthorizationMatrixTests against the
    // real handler and real granted rows; here authorization is reduced to "authenticated" so a
    // binding regression cannot hide behind a 403.
    private sealed class AuthenticatedOnlyPolicyProvider : IAuthorizationPolicyProvider
    {
        private static readonly AuthorizationPolicy Authenticated =
            new AuthorizationPolicyBuilder(SubjectAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .Build();

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => Task.FromResult(Authenticated);

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName) =>
            Task.FromResult<AuthorizationPolicy?>(Authenticated);
    }

    private sealed class SubjectAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TemplateBindingTests";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([new Claim("sub", "template-binding-tester")], SchemeName);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed class RecordingTemplateWritePort : IAbwabTemplateWritePort
    {
        private readonly List<object> _invocations = [];

        public IReadOnlyList<object> Invocations => _invocations;

        public Task<Guid> AddDoorTemplateAsync(AddDoorTemplateCommand command, CancellationToken cancellationToken) =>
            RecordId(command);

        public Task EditDoorTemplateAsync(EditDoorTemplateCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task DeleteDoorTemplateAsync(DeleteDoorTemplateCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task RestoreDoorTemplateAsync(RestoreDoorTemplateCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task<Guid> AddTemplateNodeAsync(AddTemplateNodeCommand command, CancellationToken cancellationToken) =>
            RecordId(command);

        public Task EditTemplateNodeAsync(EditTemplateNodeCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task ReparentTemplateNodeAsync(ReparentTemplateNodeCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task ReorderTemplateNodesAsync(ReorderTemplateNodesCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task RemoveTemplateNodeAsync(RemoveTemplateNodeCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task<Guid> AddTemplateNodeAliasAsync(AddTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
            RecordId(command);

        public Task EditTemplateNodeAliasAsync(EditTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task RemoveTemplateNodeAliasAsync(RemoveTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task RestoreTemplateNodeAliasAsync(RestoreTemplateNodeAliasCommand command, CancellationToken cancellationToken) =>
            Record(command);

        public Task ApplyDoorTemplateAsync(ApplyDoorTemplateCommand command, CancellationToken cancellationToken) =>
            Record(command);

        private Task Record(object command)
        {
            _invocations.Add(command);

            return Task.CompletedTask;
        }

        private Task<Guid> RecordId(object command)
        {
            _invocations.Add(command);

            return Task.FromResult(Guid.NewGuid());
        }
    }
}
