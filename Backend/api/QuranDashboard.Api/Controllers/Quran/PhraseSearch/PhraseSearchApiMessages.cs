namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

internal static class PhraseSearchApiMessages
{
    internal const string CapabilitiesLoaded = "تم تحميل إمكانات البحث في عبارات القرآن";
    internal const string RepetitionsLoaded = "تم تحميل العبارات المتكررة";
    internal const string OccurrencesLoaded = "تم تحميل مواضع العبارة";
    internal const string InvalidMode = "نمط نص العبارة غير صالح";
    internal const string InvalidLength = "عدد كلمات العبارة غير صالح";
    internal const string InvalidSort = "خيار ترتيب العبارات غير صالح";
    internal const string InvalidPaging = "معطيات تصفح العبارات غير صالحة";
    internal const string InvalidReference = "مرجع العبارة غير صالح";
    internal const string VariantNotFound = "العبارة المحددة غير موجودة";
    internal const string IndexChanged = "تغير فهرس البحث، أعد اختيار النتيجة";
    internal const string IndexUnavailable = "فهرس البحث في العبارات غير متاح حاليًا";
}

internal static class PhraseSearchErrorCodes
{
    internal const string InvalidMode = "phrase_mode_invalid";
    internal const string InvalidLength = "phrase_length_invalid";
    internal const string InvalidSort = "phrase_sort_invalid";
    internal const string InvalidPaging = "phrase_paging_invalid";
    internal const string InvalidReference = "phrase_reference_invalid";
    internal const string VariantNotFound = "phrase_variant_not_found";
    internal const string IndexChanged = "phrase_index_changed";
    internal const string IndexUnavailable = "phrase_index_unavailable";
}
