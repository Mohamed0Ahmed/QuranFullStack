namespace QuranDashboard.Api.Common;

public static class ApiMessages
{
    public const string HealthOk = "الخدمة تعمل بشكل سليم";
    public const string HealthDegraded = "الخدمة تعمل مع وجود تنبيهات";
    public const string DashboardInfo = "تم جلب معلومات التطبيق";
    public const string UnexpectedError = "حدث خطأ غير متوقع";
    public const string OperationSuccess = "تمت العملية بنجاح";

    public const string NotFound = "المورد غير موجود";

    public const string MushafPageLoaded = "تم تحميل الصفحة بنجاح";
    public const string MushafSurahCatalogLoaded = "تم تحميل فهرس السور";
    public const string MushafStudySourceCatalogLoaded = "تم تحميل كتالوج مصادر الدراسة";
    public const string MushafInvalidPageNumber = "رقم الصفحة غير صالح. يجب أن يكون بين 1 و 604.";
    public const string MushafAyahStudyLoaded = "تم تحميل سياق دراسة الآية";
    public const string MushafSimilarAyahsLoaded = "تم تحميل الآيات القريبة في المعنى";
    public const string MushafAyahMutashabihatLoaded = "تم تحميل المتشابهات اللفظية";
    public const string MushafInvalidVerseKey = "مفتاح الآية غير صالح";
    public const string MushafWordAnalysisLoaded = "تم تحميل تحليل الكلمة";
    public const string MushafInvalidWordLocation = "موقع الكلمة غير صالح";
    public const string MushafWordNotAnalyzable = "هذه الكلمة غير قابلة للتحليل (علامة نهاية آية)";
    public const string MushafWordAnalysisIncomplete = "بيانات تحليل الكلمة غير مكتملة";

    public const string UniqueWordsListLoaded = "تم تحميل الكلمات الفريدة";
    public const string UniqueWordSummaryLoaded = "تم تحميل الكلمة الفريدة";
    public const string UniqueWordSurahsLoaded = "تم تحميل السور التي وردت فيها الكلمة";
    public const string UniqueWordMissingSurahsLoaded = "تم تحميل السور التي لم ترد فيها الكلمة";
    public const string UniqueWordAyahsLoaded = "تم تحميل الآيات التي وردت فيها الكلمة";
    public const string UniqueWordsInvalidKind = "نوع الكلمات غير صالح";
    public const string UniqueWordsInvalidId = "معرّف الكلمة غير صالح";
    public const string UniqueWordsInvalidPaging = "معطيات التصفح غير صالحة";
    public const string UniqueWordsInvalidSort = "خيار الترتيب غير صالح";
    public const string UniqueWordNotFound = "الكلمة غير موجودة";

    public const string RootsListLoaded = "تم تحميل الجذور";
    public const string RootSummaryLoaded = "تم تحميل الجذر";
    public const string RootWordsLoaded = "تم تحميل كلمات الجذر";
    public const string RootAyahsLoaded = "تم تحميل الآيات التي ورد فيها الجذر";
    public const string RootSurahsLoaded = "تم تحميل السور التي ورد فيها الجذر";
    public const string RootMissingSurahsLoaded = "تم تحميل السور التي لم يرد فيها الجذر";
    public const string RootLemmasLoaded = "تم تحميل الصيغ المعجمية للجذر";
    public const string RootStemsLoaded = "تم تحميل الأصول الصرفية للجذر";
    public const string RootsInvalidSort = "خيار الترتيب غير صالح";
    public const string RootsInvalidKind = "نوع الكلمات غير صالح";
    public const string RootsInvalidId = "معرّف الجذر غير صالح";
    public const string RootsInvalidPaging = "معطيات التصفح غير صالحة";
    public const string RootNotFound = "الجذر غير موجود";
}
