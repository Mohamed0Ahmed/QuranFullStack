using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.Words;

public sealed class WordSortTokenContractTests
{
    [Theory]
    [InlineData("mushaf-order")]
    [InlineData("occurrences")]
    [InlineData("occurrences-asc")]
    [InlineData("alpha")]
    [InlineData("alpha-desc")]
    [InlineData("ayahs")]
    [InlineData("ayahs-asc")]
    [InlineData("surahs")]
    [InlineData("surahs-asc")]
    [InlineData("simple")]
    [InlineData("simple-asc")]
    [InlineData("tashkeel")]
    [InlineData("tashkeel-asc")]
    [InlineData("lemmas")]
    [InlineData("lemmas-asc")]
    [InlineData("stems")]
    [InlineData("stems-asc")]
    public void Roots_canonical_token_round_trips(string token)
    {
        RootSortParser.TryParse(token, out var spec).Should().BeTrue();
        spec.CanonicalToken().Should().Be(token);
    }

    [Theory]
    [InlineData("occurrences-desc", "occurrences")]
    [InlineData("alpha-asc", "alpha")]
    [InlineData("ayahs-desc", "ayahs")]
    [InlineData("surahs-desc", "surahs")]
    [InlineData("simple-desc", "simple")]
    [InlineData("tashkeel-desc", "tashkeel")]
    [InlineData("lemmas-desc", "lemmas")]
    [InlineData("stems-desc", "stems")]
    public void Roots_alias_canonicalizes_to_the_bare_token(string alias, string canonical)
    {
        RootSortParser.TryParse(alias, out var aliasSpec).Should().BeTrue();
        RootSortParser.TryParse(canonical, out var canonicalSpec).Should().BeTrue();

        aliasSpec.Should().Be(canonicalSpec);
        aliasSpec.CanonicalToken().Should().Be(canonical);
    }

    [Theory]
    [InlineData("occurrences", RootSortColumn.Occurrences, WordSortDirection.Descending)]
    [InlineData("occurrences-asc", RootSortColumn.Occurrences, WordSortDirection.Ascending)]
    [InlineData("alpha", RootSortColumn.Alpha, WordSortDirection.Ascending)]
    [InlineData("alpha-desc", RootSortColumn.Alpha, WordSortDirection.Descending)]
    [InlineData("mushaf-order", RootSortColumn.MushafOrder, WordSortDirection.Ascending)]
    [InlineData("  OCCURRENCES-DESC  ", RootSortColumn.Occurrences, WordSortDirection.Descending)]
    public void Roots_parses_token_to_column_and_direction(
        string token,
        RootSortColumn expectedColumn,
        WordSortDirection expectedDirection)
    {
        RootSortParser.TryParse(token, out var spec).Should().BeTrue();

        spec.Column.Should().Be(expectedColumn);
        spec.Direction.Should().Be(expectedDirection);
    }

    [Theory]
    [InlineData("mushaf-order-asc")]
    [InlineData("mushaf-order-desc")]
    [InlineData("relevance")]
    [InlineData("relevance-asc")]
    [InlineData("occurrences-sideways")]
    [InlineData("-asc")]
    [InlineData("-desc")]
    [InlineData("")]
    [InlineData(null)]
    public void Roots_rejects_unlisted_and_direction_suffixed_mushaf_order(string? token)
    {
        RootSortParser.TryParse(token, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("lemmas")]
    [InlineData("stems")]
    public void Stems_rejects_columns_it_does_not_own(string token)
    {
        StemSortParser.TryParse(token, out _).Should().BeFalse();
    }

    [Fact]
    public void Lemmas_rejects_the_lemmas_column_it_does_not_own()
    {
        LemmaSortParser.TryParse("lemmas", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("tashkeel")]
    [InlineData("lemmas")]
    [InlineData("stems")]
    public void UniqueWords_rejects_columns_it_does_not_own(string token)
    {
        UniqueWordSortParser.TryParse(token, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("alpha", WordTypeSortColumn.Alpha, WordSortDirection.Ascending)]
    [InlineData("alpha-desc", WordTypeSortColumn.Alpha, WordSortDirection.Descending)]
    [InlineData("occurrences", WordTypeSortColumn.Occurrences, WordSortDirection.Descending)]
    [InlineData("occurrences-asc", WordTypeSortColumn.Occurrences, WordSortDirection.Ascending)]
    [InlineData("ayahs", WordTypeSortColumn.Ayahs, WordSortDirection.Descending)]
    [InlineData("surahs", WordTypeSortColumn.Surahs, WordSortDirection.Descending)]
    [InlineData("mushaf-order", WordTypeSortColumn.MushafOrder, WordSortDirection.Ascending)]
    public void WordTypes_parses_token_to_column_and_direction(
        string token,
        WordTypeSortColumn expectedColumn,
        WordSortDirection expectedDirection)
    {
        WordTypeSortParser.TryParse(token, out var spec).Should().BeTrue();

        spec.Column.Should().Be(expectedColumn);
        spec.Direction.Should().Be(expectedDirection);
    }

    [Fact]
    public void Default_spec_per_explorer_matches_its_documented_default_token()
    {
        RootSortSpec.Default.CanonicalToken().Should().Be("mushaf-order");
        LemmaSortSpec.Default.CanonicalToken().Should().Be("mushaf-order");
        StemSortSpec.Default.CanonicalToken().Should().Be("mushaf-order");
        UniqueWordSortSpec.Default.CanonicalToken().Should().Be("mushaf-order");
        WordTypeSortSpec.Default.CanonicalToken().Should().Be("occurrences");
    }

    [Fact]
    public void Canonical_token_is_defined_for_every_column_and_direction()
    {
        foreach (var column in Enum.GetValues<RootSortColumn>())
        {
            foreach (var direction in Enum.GetValues<WordSortDirection>())
            {
                var token = new RootSortSpec(column, direction).CanonicalToken();

                token.Should().NotBeNullOrWhiteSpace();
                RootSortParser.TryParse(token, out var reparsed).Should().BeTrue(
                    "every canonical token must itself be a valid request token");
                reparsed.CanonicalToken().Should().Be(token, "canonicalization must be idempotent");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ExplorerNames))]
    public void Every_canonical_token_parses_back_to_itself(string explorer)
    {
        var contract = Contracts[explorer];

        foreach (var token in contract.CanonicalTokens)
        {
            contract.Canonicalize(token).Should().Be(
                token,
                "'{0}' is a canonical token, so it must be accepted and canonicalize to itself",
                token);
        }
    }

    [Theory]
    [MemberData(nameof(ExplorerNames))]
    public void Each_column_has_exactly_one_direction_that_collapses_onto_its_bare_token(string explorer)
    {
        var contract = Contracts[explorer];

        foreach (var bare in contract.BareColumnTokens)
        {
            var suffixed = new[] { contract.Canonicalize(bare + "-asc"), contract.Canonicalize(bare + "-desc") };

            suffixed.Should().NotContainNulls("both directions of '{0}' must be accepted", bare);
            suffixed.Should().ContainSingle(
                token => token == bare,
                "exactly one direction of '{0}' is its natural one and must canonicalize to the bare token",
                bare);
            suffixed.Should().ContainSingle(
                token => token == bare + "-asc" || token == bare + "-desc",
                "the opposite direction of '{0}' must keep its suffix",
                bare);
        }
    }

    [Theory]
    [MemberData(nameof(ExplorerNames))]
    public void Rejects_blank_unknown_and_direction_suffixed_mushaf_order(string explorer)
    {
        var contract = Contracts[explorer];
        string?[] rejected =
        [
            null,
            "",
            "   ",
            "mushaf-order-asc",
            "mushaf-order-desc",
            "relevance",
            "relevance-desc",
            "occurrences-sideways",
            "alpha-asc-desc",
            "-asc",
            "-desc",
        ];

        foreach (var token in rejected)
        {
            contract.Canonicalize(token).Should().BeNull(
                "'{0}' is not an allowlisted token and must reach the controlled 400 path",
                token ?? "<null>");
        }
    }

    [Fact]
    public void Legacy_tokens_keep_their_pre_feature_meaning_on_every_explorer()
    {
        AssertParses(RootSortParser.TryParse, "occurrences", new RootSortSpec(RootSortColumn.Occurrences, WordSortDirection.Descending));
        AssertParses(RootSortParser.TryParse, "alpha", new RootSortSpec(RootSortColumn.Alpha, WordSortDirection.Ascending));
        AssertParses(LemmaSortParser.TryParse, "occurrences", new LemmaSortSpec(LemmaSortColumn.Occurrences, WordSortDirection.Descending));
        AssertParses(LemmaSortParser.TryParse, "alpha", new LemmaSortSpec(LemmaSortColumn.Alpha, WordSortDirection.Ascending));
        AssertParses(StemSortParser.TryParse, "occurrences", new StemSortSpec(StemSortColumn.Occurrences, WordSortDirection.Descending));
        AssertParses(StemSortParser.TryParse, "alpha", new StemSortSpec(StemSortColumn.Alpha, WordSortDirection.Ascending));
        AssertParses(UniqueWordSortParser.TryParse, "occurrences", new UniqueWordSortSpec(UniqueWordSortColumn.Occurrences, WordSortDirection.Descending));
        AssertParses(UniqueWordSortParser.TryParse, "alpha", new UniqueWordSortSpec(UniqueWordSortColumn.Alpha, WordSortDirection.Ascending));
        AssertParses(WordTypeSortParser.TryParse, "occurrences", new WordTypeSortSpec(WordTypeSortColumn.Occurrences, WordSortDirection.Descending));
        AssertParses(WordTypeSortParser.TryParse, "alpha", new WordTypeSortSpec(WordTypeSortColumn.Alpha, WordSortDirection.Ascending));
        AssertParses(WordTypeSortParser.TryParse, "ayahs", new WordTypeSortSpec(WordTypeSortColumn.Ayahs, WordSortDirection.Descending));
        AssertParses(WordTypeSortParser.TryParse, "surahs", new WordTypeSortSpec(WordTypeSortColumn.Surahs, WordSortDirection.Descending));
    }

    private delegate bool SortParser<TSpec>(string? value, out TSpec spec);

    private static void AssertParses<TSpec>(SortParser<TSpec> parse, string token, TSpec expected)
    {
        parse(token, out var spec).Should().BeTrue("'{0}' is a pre-feature token", token);
        spec.Should().Be(expected);
    }

    public static TheoryData<string> ExplorerNames => [.. Contracts.Keys];

    private sealed record SortContract(
        Func<string?, string?> Canonicalize,
        IReadOnlyList<string> CanonicalTokens)
    {
        public IEnumerable<string> BareColumnTokens => CanonicalTokens.Where(token =>
            token != WordSortKeysMushafOrder
            && !token.EndsWith(WordSortToken.AscendingSuffix, StringComparison.Ordinal)
            && !token.EndsWith(WordSortToken.DescendingSuffix, StringComparison.Ordinal));
    }

    private const string WordSortKeysMushafOrder = "mushaf-order";

    private static readonly Dictionary<string, SortContract> Contracts = new(StringComparer.Ordinal)
    {
        ["roots"] = new(
            token => RootSortParser.TryParse(token, out var spec) ? spec.CanonicalToken() : null,
            CanonicalTokensOf<RootSortColumn>((column, direction) => new RootSortSpec(column, direction).CanonicalToken())),
        ["lemmas"] = new(
            token => LemmaSortParser.TryParse(token, out var spec) ? spec.CanonicalToken() : null,
            CanonicalTokensOf<LemmaSortColumn>((column, direction) => new LemmaSortSpec(column, direction).CanonicalToken())),
        ["stems"] = new(
            token => StemSortParser.TryParse(token, out var spec) ? spec.CanonicalToken() : null,
            CanonicalTokensOf<StemSortColumn>((column, direction) => new StemSortSpec(column, direction).CanonicalToken())),
        ["unique-words"] = new(
            token => UniqueWordSortParser.TryParse(token, out var spec) ? spec.CanonicalToken() : null,
            CanonicalTokensOf<UniqueWordSortColumn>((column, direction) => new UniqueWordSortSpec(column, direction).CanonicalToken())),
        ["word-types"] = new(
            token => WordTypeSortParser.TryParse(token, out var spec) ? spec.CanonicalToken() : null,
            CanonicalTokensOf<WordTypeSortColumn>((column, direction) => new WordTypeSortSpec(column, direction).CanonicalToken())),
    };

    private static IReadOnlyList<string> CanonicalTokensOf<TColumn>(Func<TColumn, WordSortDirection, string> canonical)
        where TColumn : struct, Enum =>
        [.. Enum.GetValues<TColumn>()
            .SelectMany(column => Enum.GetValues<WordSortDirection>().Select(direction => canonical(column, direction)))
            .Distinct(StringComparer.Ordinal)];
}
