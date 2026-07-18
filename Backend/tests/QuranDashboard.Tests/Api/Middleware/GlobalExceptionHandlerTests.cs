using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Middleware;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Tests.Api.Middleware;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_UserProvisioningEmailConflictException_Returns409ConflictEnvelope_AndLogsWarning()
    {
        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "request-conflict-1";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/access/me";
        httpContext.Response.Body = new MemoryStream();

        var exception = new UserProvisioningEmailConflictException("collision@example.test");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        // An expected business conflict is logged at Warning, never Error — it is not a server fault.
        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        // The conflicting email must never be echoed back to the caller.
        body.Should().NotContain("collision@example.test");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be(ApiMessages.EmailAlreadyRegistered);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("errors").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task TryHandleAsync_LogsSafeFields_And_ReturnsSafe500Response()
    {
        using var activity = new Activity(nameof(TryHandleAsync_LogsSafeFields_And_ReturnsSafe500Response));
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var logger = new TestLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "request-123";
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/words/unique";
        httpContext.Response.Body = new MemoryStream();

        var exception = new InvalidOperationException("secret exception message");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        logger.Entries.Should().ContainSingle();

        var entry = logger.Entries[0];
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeSameAs(exception);

        var state = entry.State;
        state.Should().NotBeNull();
        var nonNullState = state!;
        var structuredFields = nonNullState.Where(pair => pair.Key != "{OriginalFormat}").ToArray();

        structuredFields.Should().HaveCount(4);
        GetStructuredFieldValue(structuredFields, "traceId").Should().Be(activity.TraceId.ToString());
        GetStructuredFieldValue(structuredFields, "requestId").Should().Be("request-123");
        GetStructuredFieldValue(structuredFields, "method").Should().Be(HttpMethods.Post);
        GetStructuredFieldValue(structuredFields, "path").Should().Be("/api/words/unique");

        var originalFormat = nonNullState.Single(pair => pair.Key == "{OriginalFormat}");
        originalFormat.Value.Should().Be("Unhandled exception while processing request {traceId} {requestId} {method} {path}");

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        body.Should().NotContain(exception.Message);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be(ApiMessages.UnexpectedError);
        root.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("errors").EnumerateArray().Should().BeEmpty();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(
                logLevel,
                eventId,
                exception,
                state as IReadOnlyList<KeyValuePair<string, object?>>));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        Exception? Exception,
        IReadOnlyList<KeyValuePair<string, object?>>? State);

    private static object? GetStructuredFieldValue(
        IEnumerable<KeyValuePair<string, object?>> structuredFields,
        string key) => structuredFields.Single(pair => pair.Key == key).Value;

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
