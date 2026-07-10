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

    public const string LemmasListLoaded = "تم تحميل الصيغ المعجمية";
    public const string LemmaSummaryLoaded = "تم تحميل الصيغة المعجمية";
    public const string LemmaWordsLoaded = "تم تحميل كلمات الصيغة المعجمية";
    public const string LemmaAyahsLoaded = "تم تحميل الآيات التي ورد فيها الصيغة المعجمية";
    public const string LemmaSurahsLoaded = "تم تحميل السور التي ورد فيها الصيغة المعجمية";
    public const string LemmaMissingSurahsLoaded = "تم تحميل السور التي لم ترد فيها الصيغة المعجمية";
    public const string LemmaStemsLoaded = "تم تحميل الأصول الصرفية للصيغة المعجمية";
    public const string LemmasInvalidSort = "خيار الترتيب غير صالح";
    public const string LemmasInvalidKind = "نوع الكلمات غير صالح";
    public const string LemmasInvalidId = "معرّف الصيغة المعجمية غير صالح";
    public const string LemmasInvalidPaging = "معطيات التصفح غير صالحة";
    public const string LemmaNotFound = "الصيغة المعجمية غير موجودة";

    public const string StemsListLoaded = "تم تحميل الأصول الصرفية";
    public const string StemSummaryLoaded = "تم تحميل الأصل الصرفي";
    public const string StemWordsLoaded = "تم تحميل كلمات الأصل الصرفي";
    public const string StemAyahsLoaded = "تم تحميل الآيات التي ورد فيها الأصل الصرفي";
    public const string StemSurahsLoaded = "تم تحميل السور التي ورد فيها الأصل الصرفي";
    public const string StemMissingSurahsLoaded = "تم تحميل السور التي لم ترد فيها الأصل الصرفي";
    public const string StemLemmasLoaded = "تم تحميل الصيغ المعجمية للأصل الصرفي";
    public const string StemsInvalidSort = "خيار الترتيب غير صالح";
    public const string StemsInvalidKind = "نوع الكلمات غير صالح";
    public const string StemsInvalidId = "معرّف الأصل الصرفي غير صالح";
    public const string StemsInvalidPaging = "معطيات التصفح غير صالحة";
    public const string StemNotFound = "الأصل الصرفي غير موجود";

    public const string WordTypesTreeLoaded = "تم تحميل أنواع الكلمات";
    public const string WordTypesRowsLoaded = "تم تحميل كلمات النوع";
    public const string WordTypeSummaryLoaded = "تم تحميل ملخص الكلمة";
    public const string WordTypeAyahsLoaded = "تم تحميل الآيات الخاصة بالكلمة";
    public const string WordTypeSurahsLoaded = "تم تحميل سور الكلمة";
    public const string WordTypesInvalidFilter = "مرشح نوع الكلمة غير صالح";
    public const string WordTypesInvalidIdentity = "هوية الكلمة غير صالحة";
    public const string WordTypesInvalidSort = "خيار الترتيب غير صالح";
    public const string WordTypesInvalidPaging = "معطيات التصفح غير صالحة";
    public const string WordTypeNotFound = "الكلمة المحددة غير موجودة";
}
