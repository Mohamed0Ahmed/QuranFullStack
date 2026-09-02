using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes;

public abstract class WordTypeWordResult
{
    private WordTypeWordResult() { }

    public abstract class Summary : WordTypeWordResult
    {
        private Summary() { }

        public sealed class Success(WordTypeSummaryDto value) : Summary
        {
            public WordTypeSummaryDto Value { get; } = value;
        }

        public sealed class InvalidIdentity : Summary;
        public sealed class NotFound : Summary;
    }

    public abstract class Ayahs : WordTypeWordResult
    {
        private Ayahs() { }

        public sealed class Success(PagedResult<WordTypeAyahMatchDto> page) : Ayahs
        {
            public PagedResult<WordTypeAyahMatchDto> Page { get; } = page;
        }

        public sealed class InvalidIdentity : Ayahs;
        public sealed class InvalidPaging : Ayahs;
        public sealed class NotFound : Ayahs;
    }

    public abstract class Surahs : WordTypeWordResult
    {
        private Surahs() { }

        public sealed class Success(WordTypeSurahsResponse value) : Surahs
        {
            public WordTypeSurahsResponse Value { get; } = value;
        }

        public sealed class InvalidIdentity : Surahs;
        public sealed class NotFound : Surahs;
    }
}
