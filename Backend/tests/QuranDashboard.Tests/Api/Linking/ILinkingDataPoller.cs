using System.Net;
using System.Text.Json;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Linking;

public interface ILinkingDataPoller
{
    Task<JsonElement> PollDataAsync(
        HttpClient client,
        string path,
        string resourceKind,
        string resourceId,
        Func<JsonElement, bool> completed,
        TimeSpan? timeout = null);
}

public sealed class LinkingDataPoller(
    Func<IReadOnlyList<string>>? sanitizedCommandTail = null) : ILinkingDataPoller
{
    public async Task<JsonElement> PollDataAsync(
        HttpClient client,
        string path,
        string resourceKind,
        string resourceId,
        Func<JsonElement, bool> completed,
        TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            last = await ApiEnvelope.ReadDataAsync(response);
            if (completed(last))
            {
                return last;
            }

            await Task.Delay(50);
        }

        var diagnosticSuffix = sanitizedCommandTail is null
            ? string.Empty
            : $"; sanitizedSqlTail={string.Join(" | ", sanitizedCommandTail())}";
        throw new TimeoutException(
            $"Timed out waiting for resourceKind={resourceKind}; resourceId={resourceId}; "
            + $"lastBusinessState={DescribeBusinessState(last)}{diagnosticSuffix}");
    }

    private static string DescribeBusinessState(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return "not-observed";
        }

        var status = data.TryGetProperty("status", out var statusValue)
            ? statusValue.GetString() ?? "unknown"
            : "unknown";
        var stage = data.TryGetProperty("stage", out var stageValue)
            ? stageValue.GetString() ?? "unknown"
            : "unknown";
        var failureCode = data.TryGetProperty("failureCode", out var failureValue)
            && failureValue.ValueKind == JsonValueKind.String
                ? failureValue.GetString() ?? "unknown"
                : "none";
        return $"status:{status},stage:{stage},failureCode:{failureCode}";
    }
}
