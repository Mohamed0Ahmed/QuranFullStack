using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes;

public abstract class WordTypeGroupedResult
{
    private WordTypeGroupedResult() { }

    public abstract class Summary : WordTypeGroupedResult
    {
        private Summary() { }

        public sealed class Success(WordTypeGroupedSummaryDto value) : Summary
        {
            public WordTypeGroupedSummaryDto Value { get; } = value;
        }

        public sealed class InvalidKind : Summary;
        public sealed class InvalidId : Summary;
        public sealed class InvalidFilter : Summary;
        public sealed class NotFound : Summary;
    }

    public abstract class Words : WordTypeGroupedResult
    {
        private Words() { }

        public sealed class Success(PagedResult<WordTypeGroupedMemberWordDto> page) : Words
        {
            public PagedResult<WordTypeGroupedMemberWordDto> Page { get; } = page;
        }

        public sealed class InvalidKind : Words;
        public sealed class InvalidId : Words;
        public sealed class InvalidFilter : Words;
        public sealed class InvalidPaging : Words;
        public sealed class NotFound : Words;
    }

    public abstract class Ayahs : WordTypeGroupedResult
    {
        private Ayahs() { }

        public sealed class Success(PagedResult<WordTypeAyahMatchDto> page) : Ayahs
        {
            public PagedResult<WordTypeAyahMatchDto> Page { get; } = page;
        }

        public sealed class InvalidKind : Ayahs;
        public sealed class InvalidId : Ayahs;
        public sealed class InvalidFilter : Ayahs;
        public sealed class InvalidPaging : Ayahs;
        public sealed class NotFound : Ayahs;
    }

    public abstract class Surahs : WordTypeGroupedResult
    {
        private Surahs() { }

        public sealed class Success(WordTypeSurahsResponse value) : Surahs
        {
            public WordTypeSurahsResponse Value { get; } = value;
        }

        public sealed class InvalidKind : Surahs;
        public sealed class InvalidId : Surahs;
        public sealed class InvalidFilter : Surahs;
        public sealed class NotFound : Surahs;
    }
}
