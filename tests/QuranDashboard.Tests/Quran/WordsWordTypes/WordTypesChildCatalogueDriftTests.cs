using QuranDashboard.Application.Quran.Words.WordTypes.Queries;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Guards the duplicated noun child-code catalogue. Clean Architecture forbids the Application-layer
// validation from referencing the Infrastructure PosTagSeed, so the allowed noun child codes are
// hand-mirrored in WordTypesHandlerValidation. The E1 tree, however, renders noun children straight
// from the live catalogue ordered by SortOrder. If the catalogue ever gains a noun-category code
// that validation does not accept, that child would render as selectable yet be rejected with 400.
// This pure unit test (no database) fails loudly the moment such drift is introduced.
public sealed class WordTypesChildCatalogueDriftTests
{
    [Fact]
    public void Validation_Accepts_EveryNounCategoryCode_InThePosCatalogue()
    {
        var nounCatalogueCodes = PosTagSeed.GetAll()
            .Where(pos => pos.Category == "noun")
            .Select(pos => pos.Code)
            .ToList();

        nounCatalogueCodes.Should().NotBeEmpty();
        nounCatalogueCodes.Should().OnlyContain(
            code => WordTypesHandlerValidation.IsValidChildCode("noun", code),
            "every noun-category POS code is rendered as a selectable tree child, so noun child-code validation must accept it");
    }

    [Fact]
    public void Validation_Accepts_EveryParticleCategoryCode_ExceptInl()
    {
        var particleCatalogueCodes = PosTagSeed.GetAll()
            .Where(pos => pos.Category == "particle" && pos.Code != "INL")
            .Select(pos => pos.Code)
            .ToList();

        particleCatalogueCodes.Should().NotBeEmpty();
        particleCatalogueCodes.Should().OnlyContain(
            code => WordTypesHandlerValidation.IsValidChildCode("particle", code),
            "every non-INL particle-category POS code is rendered as a selectable tree child, so particle child-code validation must accept it");
        WordTypesHandlerValidation.IsValidChildCode("particle", "INL").Should().BeFalse();
    }
}
