namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

internal static class PhraseSearchApiMessages
{
    internal const string CapabilitiesLoaded = "تم تحميل إمكانات البحث في عبارات القرآن";
    internal const string RepetitionsLoaded = "تم تحميل العبارات المتكررة";
    internal const string OccurrencesLoaded = "تم تحميل مواضع العبارة";
    internal const string QueryResolved = "تم تحليل عبارة البحث";
    internal const string ContextBranchesLoaded = "تم تحميل مسارات سياق العبارة";
    internal const string ContextGroupsLoaded = "تم تحميل السياقات الكاملة للعبارة";
    internal const string ContextOccurrencesLoaded = "تم تحميل مواضع السياق الكامل";
    internal const string ContextResultsLoaded = "تم تحميل آيات السياق";
    internal const string SimilaritiesLoaded = "تم تحميل العبارات المتشابهة";
    internal const string SimilarityGroupsLoaded = "تم تحميل مجموعات العبارات المتشابهة";
    internal const string SimilarityMatchesLoaded = "تم تحميل العبارات المطابقة لمجموعة التشابه";
    internal const string InvalidMode = "نمط نص العبارة غير صالح";
    internal const string InvalidLength = "عدد كلمات العبارة غير صالح";
    internal const string InvalidSort = "خيار ترتيب العبارات غير صالح";
    internal const string InvalidPaging = "معطيات تصفح العبارات غير صالحة";
    internal const string InvalidReference = "مرجع العبارة غير صالح";
    internal const string InvalidQuery = "نص عبارة البحث غير صالح";
    internal const string InvalidQueryEncoding = "ترميز عبارة البحث غير صالح";
    internal const string QueryTooLong = "عبارة البحث أطول من الحد المسموح";
    internal const string InvalidCursor = "مرجع متابعة النتائج غير صالح";
    internal const string InvalidSimilarityThreshold = "نسبة تشابه العبارات غير صالحة";
    internal const string InvalidMinimumMatchedWords = "الحد الأدنى للكلمات المتطابقة غير صالح";
    internal const string VariantNotFound = "العبارة المحددة غير موجودة";
    internal const string SimilarityGroupNotFound = "مجموعة التشابه المحددة غير موجودة";
    internal const string IndexChanged = "تغير فهرس البحث، أعد اختيار النتيجة";
    internal const string IndexUnavailable = "فهرس البحث في العبارات غير متاح حاليًا";
    internal const string ComputeTimeout = "استغرق حساب نتائج العبارة وقتًا أطول من المسموح";
}

internal static class PhraseSearchErrorCodes
{
    internal const string InvalidMode = "phrase_mode_invalid";
    internal const string InvalidLength = "phrase_length_invalid";
    internal const string InvalidSort = "phrase_sort_invalid";
    internal const string InvalidPaging = "phrase_paging_invalid";
    internal const string InvalidReference = "phrase_reference_invalid";
    internal const string InvalidQuery = "phrase_query_invalid";
    internal const string InvalidQueryEncoding = "phrase_query_encoding_invalid";
    internal const string QueryTooLong = "phrase_query_too_long";
    internal const string InvalidCursor = "phrase_cursor_invalid";
    internal const string InvalidSimilarityThreshold = "phrase_similarity_threshold_invalid";
    internal const string InvalidMinimumMatchedWords = "phrase_minimum_matched_words_invalid";
    internal const string VariantNotFound = "phrase_variant_not_found";
    internal const string SimilarityGroupNotFound = "phrase_similarity_group_not_found";
    internal const string IndexChanged = "phrase_index_changed";
    internal const string IndexUnavailable = "phrase_index_unavailable";
    internal const string ComputeTimeout = "phrase_compute_timeout";
}
