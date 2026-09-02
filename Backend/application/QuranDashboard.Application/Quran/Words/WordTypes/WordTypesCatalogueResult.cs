using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes;

public abstract class WordTypesCatalogueResult
{
    private WordTypesCatalogueResult() { }

    public abstract class Rows : WordTypesCatalogueResult
    {
        private Rows() { }

        public sealed class Success(PagedResult<WordTypeRowDto> page) : Rows
        {
            public PagedResult<WordTypeRowDto> Page { get; } = page;
        }

        public sealed class InvalidFilter : Rows;
        public sealed class InvalidSort : Rows;
        public sealed class InvalidPaging : Rows;
    }

    public abstract class Table : WordTypesCatalogueResult
    {
        private Table() { }

        public sealed class Success(PagedResult<WordTypeTableRowDto> page) : Table
        {
            public PagedResult<WordTypeTableRowDto> Page { get; } = page;
        }

        public sealed class InvalidFilter : Table;
        public sealed class InvalidTableView : Table;
        public sealed class InvalidSort : Table;
        public sealed class InvalidPaging : Table;
    }

    public abstract class ScopeCounts : WordTypesCatalogueResult
    {
        private ScopeCounts() { }

        public sealed class Success(WordTypeScopeCountsDto counts) : ScopeCounts
        {
            public WordTypeScopeCountsDto Counts { get; } = counts;
        }

        public sealed class InvalidFilter : ScopeCounts;
    }
}
