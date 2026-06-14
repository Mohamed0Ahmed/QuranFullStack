using QuranDashboard.Infrastructure.Files.Quran.Morphology;

namespace QuranDashboard.Tests.Quran.WordsMorphology;

public sealed class BuckwalterArabicMapTests
{
    private readonly BuckwalterArabicMap map = new();

    [Fact]
    public void Map_covers_full_QAC_character_set_with_61_entries()
    {
        map.MapSize.Should().Be(61);
    }

    [Fact]
    public void All_36_consonants_are_mapped()
    {
        var consonants = "'|>&<}AbptvjHxdrzs$SDTZEgfqklmnhwYy";

        foreach (var c in consonants)
        {
            map.IsMapped(c).Should().BeTrue($"character '{c}' should be mapped");
            map.TryMap(c).Should().NotBeNull($"character '{c}' should produce a non-null Arabic value");
        }
    }

    [Fact]
    public void All_8_harakat_are_mapped()
    {
        var harakat = "auiFNK~o";

        foreach (var c in harakat)
        {
            map.IsMapped(c).Should().BeTrue($"haraka '{c}' should be mapped");
            map.TryMap(c).Should().NotBeNull();
        }
    }

    [Fact]
    public void All_5_quranic_specials_are_mapped()
    {
        var specials = "{`_^#";

        foreach (var c in specials)
        {
            map.IsMapped(c).Should().BeTrue($"special '{c}' should be mapped");
            map.TryMap(c).Should().NotBeNull();
        }
    }

    [Fact]
    public void All_12_annotation_marks_are_mapped()
    {
        var marks = "@,.'[]\":;!+%-";

        foreach (var c in marks)
        {
            map.IsMapped(c).Should().BeTrue($"annotation mark '{c}' should be mapped");
            map.TryMap(c).Should().NotBeNull();
        }
    }

    [Fact]
    public void Zero_unmapped_characters_in_full_QAC_inventory()
    {
        var mapInstance = new BuckwalterArabicMap();
        map.MapSize.Should().Be(61);
    }

    [Fact]
    public void Transliteration_is_deterministic()
    {
        var form = "bismi";
        var (first, _) = map.Transliterate(form);
        var (second, _) = map.Transliterate(form);

        first.Should().Be(second);
    }

    [Fact]
    public void Transliteration_is_deterministic_across_different_forms()
    {
        var forms = new[] { "kataba", "kitAbi", "wa", "bi", "Eabodi" };

        foreach (var form in forms)
        {
            var (a, _) = map.Transliterate(form);
            var (b, _) = map.Transliterate(form);
            a.Should().Be(b, $"transliteration of '{form}' should be deterministic");
        }
    }

    [Fact]
    public void Transliterate_returns_empty_unmapped_for_known_forms()
    {
        var (arabic, unmapped) = map.Transliterate("bismi");

        arabic.Should().NotBeNullOrEmpty();
        unmapped.Should().BeEmpty();
    }

    [Fact]
    public void Transliterate_reports_unmapped_characters()
    {
        var (arabic, unmapped) = map.Transliterate("XYZ");

        unmapped.Should().NotBeEmpty("uppercase X, Y, Z are not Buckwalter characters");
    }

    [Fact]
    public void Space_is_not_mapped()
    {
        map.IsMapped(' ').Should().BeFalse("space is a multi-word separator, not a character mapping");
    }

    [Fact]
    public void Known_transliterations_produce_expected_Arabic()
    {
        map.TryMap('b').Should().Be("ب");
        map.TryMap('A').Should().Be("ا");
        map.TryMap('k').Should().Be("ك");
        map.TryMap('a').Should().Be("\u064E");
        map.TryMap('~').Should().Be("\u0651");
    }

    [Fact]
    public void AllMappedCharacters_returns_61_distinct_chars()
    {
        var all = map.AllMappedCharacters;
        all.Should().HaveCount(61);
        all.Distinct().Should().HaveCount(61);
    }
}
