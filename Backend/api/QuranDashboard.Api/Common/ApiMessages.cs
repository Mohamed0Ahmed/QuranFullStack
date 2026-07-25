namespace QuranDashboard.Api.Common;

public static class ApiMessages
{
    public const string HealthOk = "الخدمة تعمل بشكل سليم";
    public const string HealthDegraded = "الخدمة تعمل مع وجود تنبيهات";
    public const string HealthUnhealthy = "الخدمة غير سليمة أو تعذّر الوصول إلى إحدى اعتمادياتها";
    public const string DashboardInfo = "تم جلب معلومات التطبيق";
    public const string UnexpectedError = "حدث خطأ غير متوقع";
    public const string OperationSuccess = "تمت العملية بنجاح";
    public const string TooManyRequests = "عدد كبير من الطلبات. يرجى المحاولة بعد قليل.";
    public const string Unauthorized = "يجب تسجيل الدخول للوصول إلى هذا المورد";
    public const string ValidationFailed = "الطلب غير صالح";
    public const string EmailAlreadyRegistered = "هذا البريد الإلكتروني مسجَّل بالفعل لحساب آخر";
    public const string AbwabTimelineGenerationStale = "تعذّرت العملية لأن حالة البيانات تغيّرت. يرجى تحديث الصفحة والمحاولة مجددًا.";
    public const string AbwabWriteBarrierClosed = "التعديلات متوقفة مؤقتًا أثناء عملية صيانة داخلية. يرجى المحاولة بعد قليل.";
    public const string AbwabStabilizationActive = "العمليات الإدارية متوقفة مؤقتًا أثناء عملية داخلية. يرجى المحاولة بعد قليل.";
    public const string AbwabPermissionAssignmentStale = "تعذّرت العملية لأن حالة الصلاحية تغيّرت. يرجى تحديث الصفحة والمحاولة مجددًا.";
    public const string AbwabPermissionBaselineLocked = "لا يمكن إسناد هذه الصلاحية أو إزالتها لأنها محميّة.";
    public const string AbwabLastSystemOwner = "لا يمكن إزالة آخر مالك فعّال للنظام؛ يجب بقاء مالك واحد على الأقل.";
    public const string PermissionsLoaded = "تم تحميل الصلاحيات";
    public const string PermissionGranted = "تم إسناد الصلاحية";
    public const string PermissionRevoked = "تمت إزالة الصلاحية";
    public const string UnknownPermissionCode = "رمز الصلاحية غير معروف";
    public const string Forbidden = "لا تملك صلاحية الوصول إلى هذا المورد";

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
    public const string UniqueWordsInvalidFilter = "نطاق التصفية غير صالح";
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
    public const string RootsInvalidFilter = "نطاق التصفية غير صالح";
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
    public const string LemmasInvalidFilter = "نطاق التصفية غير صالح";
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
    public const string StemsInvalidFilter = "نطاق التصفية غير صالح";
    public const string StemNotFound = "الأصل الصرفي غير موجود";

    public const string WordTypesTreeLoaded = "تم تحميل أنواع الكلمات";
    public const string WordTypesRowsLoaded = "تم تحميل كلمات النوع";
    public const string WordTypesTableLoaded = "تم تحميل جدول النوع";
    public const string WordTypesScopeCountsLoaded = "تم تحميل إحصاء النطاق";
    public const string WordTypeSummaryLoaded = "تم تحميل ملخص الكلمة";
    public const string WordTypeAyahsLoaded = "تم تحميل الآيات الخاصة بالكلمة";
    public const string WordTypeSurahsLoaded = "تم تحميل سور الكلمة";
    public const string WordTypesInvalidFilter = "مرشح نوع الكلمة غير صالح";
    public const string WordTypesInvalidIdentity = "هوية الكلمة غير صالحة";
    public const string WordTypesInvalidSort = "خيار الترتيب غير صالح";
    public const string WordTypesInvalidPaging = "معطيات التصفح غير صالحة";
    public const string WordTypesInvalidTableView = "طريقة عرض الجدول غير صالحة";
    public const string WordTypeNotFound = "الكلمة المحددة غير موجودة";

    public const string WordTypeGroupedSummaryLoaded = "تم تحميل ملخص التجميع";
    public const string WordTypeGroupedWordsLoaded = "تم تحميل كلمات التجميع";
    public const string WordTypeGroupedAyahsLoaded = "تم تحميل آيات التجميع";
    public const string WordTypeGroupedSurahsLoaded = "تم تحميل سور التجميع";
    public const string WordTypesInvalidGroupedKind = "نوع التجميع غير صالح";
    public const string WordTypesInvalidGroupedId = "معرّف التجميع غير صالح";
    public const string WordTypesGroupedNotFound = "التجميع المحدد غير موجود";

    public const string CurrentUserLoaded = "تم تحميل بيانات المستخدم الحالي";

    public const string AbwabTreeSnapshotLoaded = "تم تحميل شجرة الأبواب";
    public const string AbwabCategorySearchLoaded = "تم تحميل نتائج البحث عن الأبواب";

    public const string AbwabSectionNameConflict = "يوجد قسم آخر بهذا الاسم بالفعل";
    public const string AbwabSectionNotEmpty = "لا يمكن حذف القسم لاحتوائه على أبواب رئيسية فعّالة";
    public const string AbwabPermanentDefaultSection = "القسم الافتراضي الدائم يُعاد ترتيبه فقط، ولا يُعاد تسميته أو حذفه أو تكراره";
    public const string AbwabCategoryNameConflict = "يوجد باب آخر بهذا الاسم في هذا النطاق بالفعل";
    public const string AbwabCategoryAliasConflict = "يوجد مرادف مطابق فعّال لهذا الباب بالفعل";
    public const string AbwabCategoryCycle = "لا يمكن نقل الباب إلى نفسه أو إلى أحد فروعه";
    public const string AbwabCategoryOverlappingMove = "يحتوي طلب النقل على تداخل بين الأبواب المحددة";
    public const string AbwabCategoryUnavailable = "الباب أو الوجهة غير متاحة";
    public const string AbwabCategoryReservedByPending = "يوجد طلب معلّق يمنع حذف هذا الباب";
    public const string AbwabManualProtection = "الباب محمي يدويًا، ولا يمكن تنفيذ هذا الإجراء";
    public const string AbwabManualProtectionScopeConflict = "تغيّر نطاق الحماية اليدوية منذ آخر قراءة";
    public const string AbwabOrdinaryProtection = "الباب داخل نافذة الحماية العادية (24 ساعة)";
    public const string AbwabRelationshipDuplicate = "توجد علاقة فعّالة بهذا النوع بين البابين بالفعل";
    public const string AbwabRelationshipCycle = "لا يمكن إنشاء هذه العلاقة لأنها تُغلق دورة بين الأعم والأخص";
    public const string AbwabRelationshipSelfLink = "لا يمكن ربط الباب بنفسه";
    public const string AbwabRowStale = "تغيّرت البيانات منذ آخر قراءة. يرجى تحديث الصفحة والمحاولة مجددًا.";
    public const string AbwabTreeRevisionStale = "تغيّرت شجرة الأبواب منذ آخر قراءة. يرجى تحديث الصفحة والمحاولة مجددًا.";

    public const string AbwabSectionAdded = "تمت إضافة القسم";
    public const string AbwabSectionEdited = "تم تعديل القسم";
    public const string AbwabSectionsReordered = "تم إعادة ترتيب الأقسام";
    public const string AbwabSectionDeleted = "تم حذف القسم";
    public const string AbwabCategoryAdded = "تمت إضافة الباب";
    public const string AbwabCategoryEdited = "تم تعديل الباب";
    public const string AbwabCategoriesMoved = "تم نقل الأبواب المحددة";
    public const string AbwabCategoriesReordered = "تم إعادة ترتيب الأبواب";
    public const string AbwabCategorySubtreeDeleted = "تم حذف الباب وفروعه";
    public const string AbwabCategoryOperationRestored = "تمت استعادة عملية الحذف";
    public const string AbwabCategoryAliasAdded = "تمت إضافة المرادف";
    public const string AbwabCategoryAliasEdited = "تم تعديل المرادف";
    public const string AbwabCategoryAliasRemoved = "تمت إزالة المرادف";
    public const string AbwabManualProtectionApplied = "تم تطبيق الحماية اليدوية";
    public const string AbwabManualProtectionLifted = "تم رفع الحماية اليدوية";
    public const string AbwabFullProtectionPresetApplied = "تم تطبيق الحماية الكاملة على الأنواع الخمسة";
    public const string AbwabCompositeReadDenied = "لا تملك الصلاحيات اللازمة لعرض شجرة الأبواب";

    public const string AbwabRelationshipsLoaded = "تم تحميل علاقات الباب";
    public const string AbwabRelationshipAdded = "تمت إضافة العلاقة";
    public const string AbwabRelationshipEdited = "تم تعديل العلاقة";
    public const string AbwabRelationshipDeleted = "تم حذف العلاقة";
    public const string AbwabRelationshipRestored = "تمت استعادة العلاقة";
}
