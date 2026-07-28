using System.Net.Http.Json;
using System.Text;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// Dedicated coverage for the write routes SmokeRoutePipelineTests deliberately skips (ParityOnly) —
// see SmokeRouteCatalog. Every test resets Abwab tables at the START, not just cleanup: a test that
// fails mid-way would otherwise poison whichever test runs next, which is what actually makes the
// "run the smoke filter twice" verification meaningful.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeAbwabWriteTests(SmokeApiFixture fixture)
{
    [Fact]
    public async Task CreateSection_WithValidName_ReturnsCreatedWithEnvelope()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "القسم الأول" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("name").GetString().Should().Be("القسم الأول");
    }

    [Theory]
    [InlineData("{\"name\": null}")]
    [InlineData("{}")]
    public async Task CreateSection_WithNullOrMissingName_ReturnsBadRequestInFailureEnvelope(string body)
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/abwab/sections", content);

        // Not AssertFailureEnvelopeAsync: that helper asserts an empty errors array, which fits handler
        // outcomes but not this one — [ApiController]'s own model-state 400 populates errors with the
        // per-field binding message (the "binding-level null-item" bug class this test exists to catch).
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.ValidationFailed);
        envelope.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSection_WithBlankName_ReturnsBadRequest()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "   " });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.BadRequest, ApiMessages.AbwabSectionInvalidName);
    }

    [Fact]
    public async Task CreateSection_WithDuplicateName_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var first = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "أبواب العقيدة" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "أبواب العقيدة" });

        await ApiEnvelope.AssertFailureEnvelopeAsync(second, HttpStatusCode.Conflict, ApiMessages.AbwabSectionDuplicateName);
    }

    [Fact]
    public async Task RenameSection_WithCorrectVersion_ReturnsOkWithUpdatedName()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateSectionAsync(client, "قسم قابل لإعادة التسمية");

        using var response = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "اسم جديد", version });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("name").GetString().Should().Be("اسم جديد");
    }

    [Fact]
    public async Task RenameSection_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PutAsJsonAsync("/api/abwab/sections/999999", new { name = "لا يوجد", version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabSectionNotFound);
    }

    [Fact]
    public async Task RenameSection_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, staleVersion) = await CreateSectionAsync(client, "قسم للتحديث المتزامن");
        using var firstRename = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "تحديث أول", version = staleVersion });
        firstRename.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "تحديث ثانٍ", version = staleVersion });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabSectionStaleVersion);
    }

    [Fact]
    public async Task RenameSection_ToAnotherSectionsName_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (_, _) = await CreateSectionAsync(client, "الاسم المحجوز");
        var (id, version) = await CreateSectionAsync(client, "اسم آخر");

        using var response = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "الاسم المحجوز", version });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabSectionDuplicateName);
    }

    [Fact]
    public async Task DeleteSection_WithNoLiveDoors_ReturnsNoContentAndNoBody()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateSectionAsync(client, "قسم فارغ للحذف");

        using var response = await client.DeleteAsync($"/api/abwab/sections/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSection_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.DeleteAsync("/api/abwab/sections/999999");

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabSectionNotFound);
    }

    [Fact]
    public async Task DeleteSection_WithLiveDoors_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateSectionAsync(client, "قسم يحوي بابًا حيًا");
        await InsertLiveDoorAsync(id);

        using var response = await client.DeleteAsync($"/api/abwab/sections/{id}");

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabSectionHasLiveDoors);
    }

    private static async Task<(int Id, uint Version)> CreateSectionAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name });
        var data = await ApiEnvelope.ReadDataAsync(response);
        return (data.GetProperty("id").GetInt32(), data.GetProperty("version").GetUInt32());
    }

    // Doors has no write endpoint yet in this commit (phase 2b lands doors next), so a live door for the
    // section-delete-conflict case is seeded directly through the DbContext rather than the API.
    private async Task InsertLiveDoorAsync(int sectionId)
    {
        using var scope = fixture.ApiServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var now = DateTimeOffset.UtcNow;
        db.AbwabDoors.Add(new AbwabDoor
        {
            SectionId = sectionId,
            Name = "باب حي",
            OrderValue = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        await db.SaveChangesAsync();
    }
}
