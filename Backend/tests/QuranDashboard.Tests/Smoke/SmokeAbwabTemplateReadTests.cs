using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// The template detail's conditional GET must answer existence before the validator: the validator
// embeds only id + boot id + generation — no row data — so a caller holding the templates-list ETag
// can derive a matching If-None-Match for a template that never existed. An absence has no
// representation to validate (API_GUIDELINES.md, Conditional GETs), so the answer is 404, not 304.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeAbwabTemplateReadTests(SmokeApiFixture fixture)
{
    [Fact]
    public async Task GetTemplate_UnknownIdWithCraftedIfNoneMatch_ReturnsNotFoundWithoutValidator()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient();

        using var list = await client.GetAsync("/api/abwab/templates");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var craftedValidator = list.Headers.ETag!.ToString()
            .Replace("abwab-templates-", "abwab-template-999999-");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/abwab/templates/999999");
        request.Headers.TryAddWithoutValidation("If-None-Match", craftedValidator);
        using var response = await client.SendAsync(request);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.NotFound, ApiMessages.AbwabTemplateNotFound);
        response.Headers.ETag.Should().BeNull();
        response.Headers.CacheControl.Should().BeNull();
    }
}
