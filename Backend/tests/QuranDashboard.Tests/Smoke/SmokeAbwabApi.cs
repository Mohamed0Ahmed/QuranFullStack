using System.Net.Http.Json;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

internal static class SmokeAbwabApi
{
    public static async Task<(int Id, uint Version)> CreateSectionAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var data = await ApiEnvelope.ReadDataAsync(response);
        return (data.GetProperty("id").GetInt32(), data.GetProperty("version").GetUInt32());
    }

    public static async Task<(int Id, uint Version)> CreateDoorAsync(
        HttpClient client,
        string name,
        int sectionId,
        int? parentId = null)
    {
        using var response = await client.PostAsJsonAsync("/api/abwab/doors", new
        {
            sectionId = parentId is null ? sectionId : (int?)null,
            parentId,
            name,
            aliases = Array.Empty<string>(),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var data = await ApiEnvelope.ReadDataAsync(response);
        return (data.GetProperty("id").GetInt32(), data.GetProperty("version").GetUInt32());
    }

    public static async Task<(int Id, int RootNodeId)> CreateTemplateAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/abwab/templates", new
        {
            name,
            description = (string?)null,
            representativeAyahText = (string?)null,
            aliases = Array.Empty<string>(),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var data = await ApiEnvelope.ReadDataAsync(response);
        var rootNodeId = data.GetProperty("nodes").EnumerateArray()
            .Single(node => node.GetProperty("parentNodeId").ValueKind == JsonValueKind.Null)
            .GetProperty("id")
            .GetInt32();
        return (data.GetProperty("id").GetInt32(), rootNodeId);
    }

    public static async Task<int> AddTemplateNodeAsync(HttpClient client, int templateId, int parentNodeId, string name)
    {
        using var response = await client.PostAsJsonAsync($"/api/abwab/templates/{templateId}/nodes", new
        {
            parentNodeId,
            name,
            description = (string?)null,
            representativeAyahText = (string?)null,
            aliases = Array.Empty<string>(),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("id").GetInt32();
    }
}
