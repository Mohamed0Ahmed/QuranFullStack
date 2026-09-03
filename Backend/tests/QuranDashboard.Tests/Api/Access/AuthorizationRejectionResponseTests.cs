using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using QuranDashboard.Api.Authorization;
using ApiAuthorizationFailureReason = QuranDashboard.Api.Authorization.AuthorizationFailureReason;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class AuthorizationRejectionResponseTests(AccessTestFixture fixture) : AccessMutableWriterTest(fixture)
{
    public static TheoryData<ApiAuthorizationFailureReason, int, string> ForbiddenResponses => new()
    {
        { ApiAuthorizationFailureReason.Unprovisioned, StatusCodes.Status403Forbidden, ApiMessages.AccessUnprovisioned },
        { ApiAuthorizationFailureReason.Inactive, StatusCodes.Status403Forbidden, ApiMessages.AccessInactive },
        { ApiAuthorizationFailureReason.MissingPermission, StatusCodes.Status403Forbidden, ApiMessages.AccessPermissionDenied },
        { ApiAuthorizationFailureReason.OwnerRequired, StatusCodes.Status403Forbidden, ApiMessages.AccessOwnerRequired },
        { ApiAuthorizationFailureReason.InfrastructureUnavailable, StatusCodes.Status503ServiceUnavailable, ApiMessages.AuthorizationUnavailable },
    };

    [Fact]
    public async Task InfrastructureFailure_Writes503WithoutInvokingTheEndpoint()
    {
        await using var scope = Fixture.ApiServices.CreateAsyncScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
        };
        httpContext.Response.Body = new MemoryStream();
        scope.ServiceProvider.GetRequiredService<AuthorizationFailureState>()
            .Record(ApiAuthorizationFailureReason.InfrastructureUnavailable);
        var nextWasCalled = false;

        await scope.ServiceProvider.GetRequiredService<IAuthorizationMiddlewareResultHandler>()
            .HandleAsync(
                _ =>
                {
                    nextWasCalled = true;
                    return Task.CompletedTask;
                },
                httpContext,
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                PolicyAuthorizationResult.Forbid());

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        nextWasCalled.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ForbiddenResponses))]
    public async Task ForbiddenFailure_WritesTheExactSharedEnvelope(
        ApiAuthorizationFailureReason failureReason,
        int expectedStatus,
        string expectedMessage)
    {
        await using var scope = Fixture.ApiServices.CreateAsyncScope();
        var httpContext = CreateHttpContext(scope.ServiceProvider);
        scope.ServiceProvider.GetRequiredService<AuthorizationFailureState>().Record(failureReason);

        await scope.ServiceProvider.GetRequiredService<IAuthorizationMiddlewareResultHandler>()
            .HandleAsync(
                _ => Task.CompletedTask,
                httpContext,
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                PolicyAuthorizationResult.Forbid());

        httpContext.Response.StatusCode.Should().Be(expectedStatus);
        httpContext.Response.ContentType.Should().StartWith("application/json");
        var envelope = await ReadEnvelopeAsync(httpContext);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(expectedMessage);
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        envelope.GetProperty("errors").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Challenge_WritesTheExistingUnauthorizedEnvelope()
    {
        await using var scope = Fixture.ApiServices.CreateAsyncScope();
        var httpContext = CreateHttpContext(scope.ServiceProvider);
        scope.ServiceProvider.GetRequiredService<AuthorizationFailureState>()
            .Record(ApiAuthorizationFailureReason.InfrastructureUnavailable);

        await scope.ServiceProvider.GetRequiredService<IAuthorizationMiddlewareResultHandler>()
            .HandleAsync(
                _ => Task.CompletedTask,
                httpContext,
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                PolicyAuthorizationResult.Challenge());

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var envelope = await ReadEnvelopeAsync(httpContext);
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.Unauthorized);
    }

    [Fact]
    public async Task SuccessfulAuthorization_InvokesTheExistingPipeline()
    {
        await using var scope = Fixture.ApiServices.CreateAsyncScope();
        var httpContext = CreateHttpContext(scope.ServiceProvider);
        var nextWasCalled = false;

        await scope.ServiceProvider.GetRequiredService<IAuthorizationMiddlewareResultHandler>()
            .HandleAsync(
                context =>
                {
                    nextWasCalled = true;
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return Task.CompletedTask;
                },
                httpContext,
                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build(),
                PolicyAuthorizationResult.Success());

        nextWasCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task RejectionWriter_ShortCircuitsWhenTheResponseHasStarted()
    {
        await using var scope = Fixture.ApiServices.CreateAsyncScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
        };
        httpContext.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        await scope.ServiceProvider.GetRequiredService<AuthorizationRejectionWriter>()
            .WriteForbidAsync(
                httpContext,
                failureReason: ApiAuthorizationFailureReason.InfrastructureUnavailable,
                cancellationToken: CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        httpContext.Response.Headers.Should().BeEmpty();
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider serviceProvider)
    {
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<JsonElement> ReadEnvelopeAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        return document.RootElement.Clone();
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
