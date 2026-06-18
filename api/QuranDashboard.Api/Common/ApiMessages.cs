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
    public const string MushafInvalidPageNumber = "رقم الصفحة غير صالح. يجب أن يكون بين 1 و 604.";
    public const string MushafAyahStudyLoaded = "تم تحميل سياق دراسة الآية";
    public const string MushafInvalidVerseKey = "مفتاح الآية غير صالح";
    public const string MushafWordAnalysisLoaded = "تم تحميل تحليل الكلمة";
    public const string MushafInvalidWordLocation = "موقع الكلمة غير صالح";
    public const string MushafWordNotAnalyzable = "هذه الكلمة غير قابلة للتحليل (علامة نهاية آية)";
}
