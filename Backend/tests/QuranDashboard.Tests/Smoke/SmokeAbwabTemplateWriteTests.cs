using System.Net.Http.Json;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(SmokeCollection))]
public sealed class SmokeAbwabTemplateWriteTests(SmokeApiFixture fixture)
{
    [Fact]
    public async Task GetTemplates_AndTemplateDetail_KeepTheirPublicSuccessAndNotFoundEnvelopes()
    {
        await fixture.ResetAsync();
        using var owner = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, _) = await SmokeAbwabApi.CreateTemplateAsync(owner, "__smoke_template_read__");
        using var anonymous = fixture.CreateClientFor(SmokePersona.Anonymous);

        using var list = await anonymous.GetAsync("/api/abwab/templates");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiEnvelope.ReadDataAsync(list)).EnumerateArray().Should().ContainSingle();

        using var detail = await anonymous.GetAsync($"/api/abwab/templates/{templateId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiEnvelope.ReadDataAsync(detail)).GetProperty("id").GetInt32().Should().Be(templateId);

        using var missing = await anonymous.GetAsync("/api/abwab/templates/999999");
        await ApiEnvelope.AssertFailureEnvelopeAsync(missing, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNotFound);
    }

    [Fact]
    public async Task CreateTemplate_UsesCreatedAndInvalidNameEnvelopes()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();

        using var created = await client.PostAsJsonAsync("/api/abwab/templates", new
        {
            name = "__smoke_template_create__",
            description = (string?)null,
            representativeAyahText = (string?)null,
            aliases = Array.Empty<string>(),
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ApiEnvelope.ReadDataAsync(created)).GetProperty("nodes").GetArrayLength().Should().Be(1);

        using var invalid = await client.PostAsJsonAsync("/api/abwab/templates", new
        {
            name = " ",
            description = (string?)null,
            representativeAyahText = (string?)null,
            aliases = Array.Empty<string>(),
        });
        await ApiEnvelope.AssertFailureEnvelopeAsync(invalid, HttpStatusCode.BadRequest, ApiMessages.AbwabTemplateInvalidName);
    }

    [Fact]
    public async Task DeleteTemplate_UsesNoContentAndNotFoundResponses()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, _) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_delete__");

        using var deleted = await client.DeleteAsync($"/api/abwab/templates/{templateId}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await deleted.Content.ReadAsByteArrayAsync()).Should().BeEmpty();

        using var missing = await client.DeleteAsync($"/api/abwab/templates/{templateId}");
        await ApiEnvelope.AssertFailureEnvelopeAsync(missing, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNotFound);
    }

    [Fact]
    public async Task ApplyTemplate_WithChildrenAndALiveTarget_ReturnsCreated()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, rootNodeId) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_apply__");
        await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_template_child__");
        var targetId = await CreateTargetDoorAsync(client, "__smoke_apply_target__");

        using var response = await client.PostAsJsonAsync($"/api/abwab/templates/{templateId}/apply", new
        {
            targetDoorIds = new[] { targetId },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ApiEnvelope.ReadDataAsync(response)).EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task ApplyTemplate_UsesEmptyMissingAndCollisionFailureEnvelopes()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var targetId = await CreateTargetDoorAsync(client, "__smoke_apply_failure_target__");
        var (emptyTemplateId, _) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_empty__");

        using var empty = await client.PostAsJsonAsync($"/api/abwab/templates/{emptyTemplateId}/apply", new
        {
            targetDoorIds = new[] { targetId },
        });
        await ApiEnvelope.AssertFailureEnvelopeAsync(empty, HttpStatusCode.BadRequest, ApiMessages.AbwabTemplateApplyEmpty);

        using var missing = await client.PostAsJsonAsync("/api/abwab/templates/999999/apply", new
        {
            targetDoorIds = new[] { targetId },
        });
        await ApiEnvelope.AssertFailureEnvelopeAsync(missing, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNotFound);

        var (templateId, rootNodeId) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_collision__");
        await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_collision_child__");
        var (sectionId, _) = await SmokeAbwabApi.CreateSectionAsync(client, "__smoke_collision_section__");
        var (collisionTargetId, _) = await SmokeAbwabApi.CreateDoorAsync(client, "__smoke_collision_target__", sectionId);
        await SmokeAbwabApi.CreateDoorAsync(client, "__smoke_collision_child__", sectionId, collisionTargetId);

        using var collision = await client.PostAsJsonAsync($"/api/abwab/templates/{templateId}/apply", new
        {
            targetDoorIds = new[] { collisionTargetId },
        });
        collision.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var collisionEnvelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(collision);
        collisionEnvelope.GetProperty("message").GetString().Should().Contain("__smoke_collision_target__");
        collisionEnvelope.GetProperty("message").GetString().Should().Contain("__smoke_collision_child__");
    }

    [Fact]
    public async Task AddTemplateNode_UsesCreatedAndAllFailureEnvelopes()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, rootNodeId) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_node_create__");
        var body = new
        {
            parentNodeId = rootNodeId,
            name = "__smoke_template_node__",
            description = (string?)null,
            representativeAyahText = (string?)null,
            aliases = Array.Empty<string>(),
        };

        using var created = await client.PostAsJsonAsync($"/api/abwab/templates/{templateId}/nodes", body);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        using var missingParent = await client.PostAsJsonAsync($"/api/abwab/templates/{templateId}/nodes", new
        {
            parentNodeId = (int?)null,
            name = "__smoke_template_node_no_parent__",
            description = (string?)null,
            representativeAyahText = (string?)null,
            aliases = Array.Empty<string>(),
        });
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            missingParent,
            HttpStatusCode.BadRequest,
            ApiMessages.AbwabTemplateNodeMissingParent);

        using var missingTemplate = await client.PostAsJsonAsync("/api/abwab/templates/999999/nodes", body);
        await ApiEnvelope.AssertFailureEnvelopeAsync(missingTemplate, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNotFound);

        using var duplicate = await client.PostAsJsonAsync($"/api/abwab/templates/{templateId}/nodes", body);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            duplicate,
            HttpStatusCode.Conflict,
            ApiMessages.AbwabTemplateNodeDuplicateName);
    }

    [Fact]
    public async Task EditTemplateNode_UsesSuccessAndAllFailureEnvelopes()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, rootNodeId) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_node_edit__");
        await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_template_node_first__");
        var secondNodeId = await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_template_node_second__");

        using var edited = await client.PutAsJsonAsync($"/api/abwab/template-nodes/{secondNodeId}", NodeBody("__smoke_template_node_edited__"));
        edited.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiEnvelope.ReadDataAsync(edited)).GetProperty("name").GetString().Should().Be("__smoke_template_node_edited__");

        using var invalid = await client.PutAsJsonAsync($"/api/abwab/template-nodes/{secondNodeId}", NodeBody(" "));
        await ApiEnvelope.AssertFailureEnvelopeAsync(invalid, HttpStatusCode.BadRequest, ApiMessages.AbwabTemplateNodeInvalidName);

        using var missing = await client.PutAsJsonAsync("/api/abwab/template-nodes/999999", NodeBody("__smoke_template_node_missing__"));
        await ApiEnvelope.AssertFailureEnvelopeAsync(missing, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNodeNotFound);

        using var duplicate = await client.PutAsJsonAsync($"/api/abwab/template-nodes/{secondNodeId}", NodeBody("__smoke_template_node_first__"));
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            duplicate,
            HttpStatusCode.Conflict,
            ApiMessages.AbwabTemplateNodeDuplicateName);
    }

    [Fact]
    public async Task ReorderTemplateNode_UsesSuccessAndAllFailureEnvelopes()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, rootNodeId) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_node_reorder__");
        await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_template_node_reorder_first__");
        var secondNodeId = await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_template_node_reorder_second__");

        using var reordered = await client.PostAsJsonAsync($"/api/abwab/template-nodes/{secondNodeId}/order", new { position = 1 });
        reordered.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiEnvelope.ReadDataAsync(reordered)).GetProperty("orderValue").GetInt32().Should().Be(1);

        using var root = await client.PostAsJsonAsync($"/api/abwab/template-nodes/{rootNodeId}/order", new { position = 1 });
        await ApiEnvelope.AssertFailureEnvelopeAsync(root, HttpStatusCode.BadRequest, ApiMessages.AbwabTemplateRootNotReorderable);

        using var missing = await client.PostAsJsonAsync("/api/abwab/template-nodes/999999/order", new { position = 1 });
        await ApiEnvelope.AssertFailureEnvelopeAsync(missing, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNodeNotFound);
    }

    [Fact]
    public async Task DeleteTemplateNode_UsesNoContentAndAllFailureEnvelopes()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (templateId, rootNodeId) = await SmokeAbwabApi.CreateTemplateAsync(client, "__smoke_template_node_delete__");
        var nodeId = await SmokeAbwabApi.AddTemplateNodeAsync(client, templateId, rootNodeId, "__smoke_template_node_delete_child__");

        using var deleted = await client.DeleteAsync($"/api/abwab/template-nodes/{nodeId}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await deleted.Content.ReadAsByteArrayAsync()).Should().BeEmpty();

        using var root = await client.DeleteAsync($"/api/abwab/template-nodes/{rootNodeId}");
        await ApiEnvelope.AssertFailureEnvelopeAsync(root, HttpStatusCode.BadRequest, ApiMessages.AbwabTemplateRootNotDeletable);

        using var missing = await client.DeleteAsync("/api/abwab/template-nodes/999999");
        await ApiEnvelope.AssertFailureEnvelopeAsync(missing, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNodeNotFound);
    }

    private static async Task<int> CreateTargetDoorAsync(HttpClient client, string name)
    {
        var (sectionId, _) = await SmokeAbwabApi.CreateSectionAsync(client, $"{name}_section");
        var (targetId, _) = await SmokeAbwabApi.CreateDoorAsync(client, name, sectionId);
        return targetId;
    }

    private static object NodeBody(string name) => new
    {
        name,
        description = (string?)null,
        representativeAyahText = (string?)null,
        aliases = Array.Empty<string>(),
    };
}
