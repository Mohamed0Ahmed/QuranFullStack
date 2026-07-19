namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedDetailsControllerTests(WordTypesTestFixture fixture)
{
    public static TheoryData<string, int, string, string> ApiResponseCases => new()
    {
        { Summary("roots", 190700, "type=noun"), 200, ApiMessages.WordTypeGroupedSummaryLoaded, "summary" },
        { Summary("bogus", 190700, "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedKind, "failure" },
        { Summary("roots", 0, "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedId, "failure" },
        { Summary("roots", 190700, "type=noun&tense=past"), 400, ApiMessages.WordTypesInvalidFilter, "failure" },
        { Summary("roots", 190701, "type=noun"), 404, ApiMessages.WordTypesGroupedNotFound, "failure" },

        { Detail("roots", 190700, "words", "type=noun"), 200, ApiMessages.WordTypeGroupedWordsLoaded, "paged" },
        { Detail("bogus", 190700, "words", "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedKind, "failure" },
        { Detail("roots", 0, "words", "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedId, "failure" },
        { Detail("roots", 190700, "words", "type=noun&tense=past"), 400, ApiMessages.WordTypesInvalidFilter, "failure" },
        { Detail("roots", 190700, "words", "type=noun&page=0&pageSize=25"), 400, ApiMessages.WordTypesInvalidPaging, "failure" },
        { Detail("roots", 190701, "words", "type=noun"), 404, ApiMessages.WordTypesGroupedNotFound, "failure" },

        { Detail("roots", 190700, "ayahs", "type=noun"), 200, ApiMessages.WordTypeGroupedAyahsLoaded, "paged" },
        { Detail("bogus", 190700, "ayahs", "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedKind, "failure" },
        { Detail("roots", 0, "ayahs", "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedId, "failure" },
        { Detail("roots", 190700, "ayahs", "type=noun&tense=past"), 400, ApiMessages.WordTypesInvalidFilter, "failure" },
        { Detail("roots", 190700, "ayahs", "type=noun&page=0&pageSize=25"), 400, ApiMessages.WordTypesInvalidPaging, "failure" },
        { Detail("roots", 190701, "ayahs", "type=noun"), 404, ApiMessages.WordTypesGroupedNotFound, "failure" },

        { Detail("roots", 190700, "surahs", "type=noun"), 200, ApiMessages.WordTypeGroupedSurahsLoaded, "surahs" },
        { Detail("bogus", 190700, "surahs", "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedKind, "failure" },
        { Detail("roots", 0, "surahs", "type=noun"), 400, ApiMessages.WordTypesInvalidGroupedId, "failure" },
        { Detail("roots", 190700, "surahs", "type=noun&tense=past"), 400, ApiMessages.WordTypesInvalidFilter, "failure" },
        { Detail("roots", 190701, "surahs", "type=noun"), 404, ApiMessages.WordTypesGroupedNotFound, "failure" },
    };

    [Theory]
    [MemberData(nameof(ApiResponseCases))]
    public async Task GroupedDetails_HttpPipeline_ReturnsCompleteApiResponseEnvelope(
        string requestUri,
        int expectedStatusCode,
        string expectedMessage,
        string expectedPayloadKind)
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(requestUri, CancellationToken.None);

        response.StatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;

        envelope.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["isSuccess", "message", "data", "errors"]);
        envelope.GetProperty("message").GetString().Should().Be(expectedMessage);

        if (expectedPayloadKind == "failure")
        {
            AssertFailureEnvelope(envelope);
            return;
        }

        envelope.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        envelope.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Null);
        var payload = envelope.GetProperty("data");
        payload.ValueKind.Should().Be(JsonValueKind.Object);
        AssertSuccessPayload(expectedPayloadKind, payload);
    }

    private static void AssertFailureEnvelope(JsonElement envelope)
    {
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
        var errors = envelope.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Array);
        errors.GetArrayLength().Should().Be(0);
    }

    private static void AssertSuccessPayload(string expectedPayloadKind, JsonElement payload)
    {
        switch (expectedPayloadKind)
        {
            case "summary":
                payload.GetProperty("kind").GetString().Should().Be("root");
                payload.GetProperty("dimensionId").GetInt32().Should().Be(190700);
                payload.GetProperty("occurrencesCount").GetInt32().Should().BeGreaterThan(0);
                break;
            case "paged":
                payload.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
                payload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
                break;
            case "surahs":
                payload.GetProperty("surahs").GetArrayLength().Should().BeGreaterThan(0);
                payload.GetProperty("missingSurahs").ValueKind.Should().Be(JsonValueKind.Array);
                break;
            default:
                throw new InvalidOperationException($"Unexpected payload assertion '{expectedPayloadKind}'.");
        }
    }

    private static string Summary(string kind, int dimensionId, string query) =>
        $"/api/words/word-types/table/{kind}/{dimensionId}?{query}";

    private static string Detail(string kind, int dimensionId, string detail, string query) =>
        $"/api/words/word-types/table/{kind}/{dimensionId}/{detail}?{query}";
}
