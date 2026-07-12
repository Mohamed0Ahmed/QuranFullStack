using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Api.Controllers.Words;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Thin-controller proof for the four grouped detail routes: the route templates are exactly the agreed
// family, and every outcome maps to the correct ApiResponse status (400 for invalid kind/id/filter/paging,
// 404 for an absent scoped group, 200 for success).
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedDetailsControllerTests(WordTypesTestFixture fixture)
{
    [Fact]
    public void GroupedDetailsController_UsesVerifiedRouteTemplates()
    {
        var templates = typeof(WordTypeGroupedDetailsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpGetAttribute>())
            .Select(attribute => attribute.Template)
            .ToList();

        templates.Should().BeEquivalentTo(new[]
        {
            "{kind}/{dimensionId:int}",
            "{kind}/{dimensionId:int}/words",
            "{kind}/{dimensionId:int}/ayahs",
            "{kind}/{dimensionId:int}/surahs",
        });
    }

    [Fact]
    public async Task GroupedDetailsController_MapsInvalidKindIdFilterPagingAndNotFound()
    {
        await using var scope = fixture.CreateScope();
        var controller = CreateController(scope.ServiceProvider);

        Unwrap(await controller.GetSummary("roots", 190700, "noun", null, null, null, null, CancellationToken.None))
            .Should().Be((200, true));
        Unwrap(await controller.GetSummary("bogus", 190700, "noun", null, null, null, null, CancellationToken.None))
            .Should().Be((400, false));
        Unwrap(await controller.GetSummary("roots", 0, "noun", null, null, null, null, CancellationToken.None))
            .Should().Be((400, false));
        Unwrap(await controller.GetSummary("roots", 190700, "noun", null, null, "past", null, CancellationToken.None))
            .Should().Be((400, false));
        Unwrap(await controller.GetSummary("roots", 190701, "noun", null, null, null, null, CancellationToken.None))
            .Should().Be((404, false));

        Unwrap(await controller.GetWords("roots", 190700, "noun", null, null, null, null, 1, 25, CancellationToken.None))
            .Should().Be((200, true));
        Unwrap(await controller.GetWords("roots", 190700, "noun", null, null, null, null, 0, 25, CancellationToken.None))
            .Should().Be((400, false));

        Unwrap(await controller.GetAyahs("roots", 190700, "noun", null, null, null, null, 1, 25, CancellationToken.None))
            .Should().Be((200, true));
        Unwrap(await controller.GetAyahs("roots", 190700, "noun", null, null, null, null, 0, 25, CancellationToken.None))
            .Should().Be((400, false));

        Unwrap(await controller.GetSurahs("roots", 190700, "noun", null, null, null, null, CancellationToken.None))
            .Should().Be((200, true));
        Unwrap(await controller.GetSurahs("roots", 190701, "noun", null, null, null, null, CancellationToken.None))
            .Should().Be((404, false));
    }

    private static WordTypeGroupedDetailsController CreateController(IServiceProvider services) => new(
        services.GetRequiredService<GetWordTypeGroupedSummaryHandler>(),
        services.GetRequiredService<GetWordTypeGroupedWordsHandler>(),
        services.GetRequiredService<GetWordTypeGroupedAyahsHandler>(),
        services.GetRequiredService<GetWordTypeGroupedSurahsHandler>());

    // Every action returns ActionResult<ApiResponse<T>> built through Ok/BadRequest/NotFound, so the
    // IActionResult is always an ObjectResult carrying the status code and the typed ApiResponse body.
    private static (int StatusCode, bool IsSuccess) Unwrap<T>(ActionResult<ApiResponse<T>> action)
    {
        var objectResult = action.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        var body = objectResult.Value.Should().BeOfType<ApiResponse<T>>().Subject;
        return (objectResult.StatusCode ?? 0, body.IsSuccess);
    }
}
