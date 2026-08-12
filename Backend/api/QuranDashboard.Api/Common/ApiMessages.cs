using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Linking;

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
    public const string AccessUnprovisioned = "لم يتم إعداد حسابك للوصول إلى هذا المورد";
    public const string AccessInactive = "حسابك غير نشط";
    public const string AccessPermissionDenied = "ليس لديك الصلاحية اللازمة للوصول إلى هذا المورد";
    public const string AccessOwnerRequired = "يتطلب هذا المورد صلاحية المالك";
    public const string AuthorizationUnavailable = "تعذّر التحقق من صلاحيات الوصول. يرجى المحاولة لاحقًا.";
    public const string ValidationFailed = "الطلب غير صالح";
    public const string EmailAlreadyRegistered = "هذا البريد الإلكتروني مسجَّل بالفعل لحساب آخر";

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

    public const string AccessAdministrationUsersLoaded = "تم تحميل المستخدمين";
    public const string AccessAdministrationUserLoaded = "تم تحميل بيانات المستخدم";
    public const string AccessAdministrationUserAccepted = "تم قبول المستخدم";
    public const string AccessAdministrationUserDisabled = "تم تعطيل المستخدم";
    public const string AccessAdministrationUserReactivated = "تمت إعادة تفعيل المستخدم";
    public const string AccessAdministrationPermissionCatalogueLoaded = "تم تحميل كتالوج الصلاحيات";
    public const string AccessAdministrationPermissionsLoaded = "تم تحميل صلاحيات المستخدم";
    public const string AccessAdministrationPermissionsReplaced = "تم تحديث صلاحيات المستخدم";
    public const string AccessAdministrationAuditEventsLoaded = "تم تحميل سجل تدقيق الوصول";
    public const string AccessAdministrationRelinkPreviewLoaded = "تم التحقق من معاينة ربط الهوية";
    public const string AccessAdministrationRelinkConfirmed = "تم تحديث معرّف الهوية";
    public const string AccessAdministrationOwnerReconciliationLoaded = "تم تحميل حالة مطابقة المالكين";
    public const string AccessAdministrationInvalidRequest = "طلب إدارة الوصول غير صالح";
    public const string AccessAdministrationUserNotFound = "المستخدم غير موجود";
    public const string AccessAdministrationOwnerTargetForbidden = "لا يمكن تنفيذ هذه العملية على مالك";
    public const string AccessAdministrationInvalidTransition = "انتقال حالة المستخدم غير صالح";
    public const string AccessAdministrationStaleVersion = "تم تعديل المستخدم من عملية أخرى، يرجى التحديث والمحاولة مرة أخرى";
    public const string AccessAdministrationInvalidPermissionCodes = "رموز الصلاحيات غير صالحة";
    public const string AccessAdministrationSubjectAlreadyLinked = "معرّف الهوية مرتبط بالفعل بمستخدم آخر";
    public const string AccessAdministrationNormalizedEmailCollision = "تعارضت هوية البريد الإلكتروني الموحّدة";
    public const string AccessAdministrationInvalidRelinkEvidence = "تعذّر التحقق من دليل ربط الهوية";
    public const string AccessAdministrationOwnerRelinkNotReconciled = "لا يمكن ربط هوية المالك قبل اكتمال المطابقة";
    public const string AccessAdministrationRelinkConfirmationRequired = "يلزم تأكيد عملية ربط الهوية";

    public const string AbwabSectionCreated = "تم إنشاء القسم";
    public const string AbwabSectionRenamed = "تم تعديل اسم القسم";
    public const string AbwabSectionInvalidName = "اسم القسم غير صالح";
    public const string AbwabSectionNotFound = "القسم غير موجود";
    public const string AbwabSectionDuplicateName = "يوجد قسم آخر بنفس الاسم";
    public const string AbwabSectionHasLiveDoors = "لا يمكن حذف القسم لاحتوائه على أبواب حالية";
    public const string AbwabSectionStaleVersion = "تم تعديل القسم من مستخدم آخر، يرجى التحديث والمحاولة مرة أخرى";
    public const string AbwabSectionReordered = "تم تعديل ترتيب القسم";
    public const string AbwabSectionInvalidPosition = "الترتيب المطلوب خارج نطاق الأقسام";

    public const string AbwabDoorCreated = "تم إنشاء الباب";
    public const string AbwabDoorEdited = "تم تعديل الباب";
    public const string AbwabDoorMoved = "تم نقل الباب";
    public const string AbwabDoorReordered = "تم إعادة ترتيب الباب";
    public const string AbwabDoorsBulkMoved = "تم نقل الأبواب المحددة";
    public const string AbwabDoorsBulkArchived = "تم أرشفة الأبواب المحددة";
    public const string AbwabDoorRestored = "تم استعادة الباب";
    public const string AbwabDoorInvalidName = "اسم الباب غير صالح";
    public const string AbwabDoorsBulkInvalidRequest = "يجب تحديد باب واحد على الأقل";
    public const string AbwabDoorNotFound = "الباب غير موجود";
    public const string AbwabDoorParentNotFound = "الباب الأب غير موجود";
    public const string AbwabDoorSectionNotFound = "القسم غير موجود";
    public const string AbwabDoorSectionParentMismatch = "الباب الفرعي يتبع قسم الباب الأب، والقسم المحدد يخالفه";
    public const string AbwabDoorSectionRequired = "يجب تحديد قسم للباب الرئيسي";
    public const string AbwabDoorRestoreSectionRequired = "قسم الباب الأصلي محذوف، حدد قسمًا للاسترجاع";
    public const string AbwabDoorDuplicateName = "يوجد باب آخر بنفس الاسم في هذا الموضع";
    public const string AbwabDoorStaleVersion = "تم تعديل الباب من مستخدم آخر، يرجى التحديث والمحاولة مرة أخرى";
    public const string AbwabDoorWouldCycle = "لا يمكن نقل الباب إلى داخل فرعه الخاص";
    public const string AbwabDoorInvalidPosition = "الترتيب المطلوب خارج نطاق الأبواب المجاورة";
    public const string AbwabDoorParentStillArchived = "لا يمكن استعادة الباب لأن الباب الأب ما زال مؤرشفًا";
    public const string AbwabDoorInvalidScope = "نطاق الترتيب غير صالح";
    public const string AbwabDoorScopeNotApplicable = "الترتيب العام غير متاح للأبواب الفرعية";

    public const string AbwabTreeLoaded = "تم تحميل شجرة الأبواب";

    public const string AbwabDoorRelationsLoaded = "تم تحميل علاقات الباب";
    public const string AbwabDoorRelationsCreated = "تم إنشاء العلاقات";
    public const string AbwabDoorRelationsInvalidRequest = "يجب اختيار باب واحد على الأقل";
    public const string AbwabDoorRelationInvalidType = "نوع العلاقة غير صالح";
    public const string AbwabDoorRelationInvalidDirection = "اتجاه الشمولية غير صالح";
    public const string AbwabDoorRelationSelf = "لا يمكن ربط الباب بنفسه";
    public const string AbwabDoorRelationArchivedDoor = "لا يمكن إنشاء علاقة مع باب مؤرشف";
    public const string AbwabDoorRelationDuplicate = "توجد علاقة من هذا النوع بالفعل مع هذا الباب";
    public const string AbwabDoorRelationNotFound = "العلاقة غير موجودة";

    private const string AbwabDoorRelationDuplicatePrefix = "توجد علاقة من هذا النوع بالفعل مع";

    public static string AbwabDoorRelationDuplicateWith(IReadOnlyList<string> doorNames) =>
        doorNames.Count == 0
            ? AbwabDoorRelationDuplicate
            : $"{AbwabDoorRelationDuplicatePrefix}: {string.Join("، ", doorNames)}";

    public const string AbwabTemplatesLoaded = "تم تحميل القوالب";
    public const string AbwabTemplateLoaded = "تم تحميل القالب";
    public const string AbwabTemplateNotFound = "القالب غير موجود";
    public const string AbwabTemplateCreated = "تم إنشاء القالب";
    public const string AbwabTemplateInvalidName = "اسم القالب غير صالح";
    public const string AbwabTemplateNodeCreated = "تم إضافة العنصر";
    public const string AbwabTemplateNodeEdited = "تم تعديل العنصر";
    public const string AbwabTemplateNodeReordered = "تم إعادة ترتيب العنصر";
    public const string AbwabTemplateNodeInvalidName = "اسم العنصر غير صالح";
    public const string AbwabTemplateNodeMissingParent = "يجب تحديد العنصر الأب";
    public const string AbwabTemplateNodeNotFound = "العنصر غير موجود";
    public const string AbwabTemplateNodeParentNotFound = "العنصر الأب غير موجود في هذا القالب";
    public const string AbwabTemplateNodeDuplicateName = "يوجد عنصر آخر بنفس الاسم تحت العنصر الأب";
    public const string AbwabTemplateNodeInvalidPosition = "الترتيب المطلوب خارج نطاق العناصر المجاورة";
    public const string AbwabTemplateRootNotReorderable = "جذر القالب ليس له عناصر مجاورة لإعادة ترتيبها";
    public const string AbwabTemplateRootNotDeletable = "لا يمكن حذف جذر القالب، احذف القالب نفسه";

    public const string AbwabTemplateApplied = "تم نسخ القالب";
    public const string AbwabTemplateApplyNoTargets = "يجب اختيار باب مستهدف واحد على الأقل";
    public const string AbwabTemplateApplyTargetArchived = "لا يمكن النسخ داخل باب مؤرشف";
    public const string AbwabTemplateApplyEmpty = "القالب لا يحتوي عناصر لنسخها";
    public const string AbwabTemplateApplyCollision = "يوجد باب بنفس اسم أحد عناصر القالب داخل الباب المستهدف";

    private const string AbwabTemplateApplyCollisionPrefix = "لم يتم النسخ — أسماء موجودة داخل الأبواب المستهدفة";

    public static string AbwabTemplateApplyCollisionWith(IReadOnlyList<AbwabTemplateApplyCollisionPair> collisions) =>
        collisions.Count == 0
            ? AbwabTemplateApplyCollision
            : $"{AbwabTemplateApplyCollisionPrefix}: {string.Join("، ", collisions.Select(c => $"«{c.TargetName}» ← «{c.ChildName}»"))}";

    public const string LinkingSourceResolved = "تم تحميل آيات المصدر";
    public const string LinkingSourceNotFound = "المصدر المشار إليه غير موجود";

    private const string LinkingSourceDescriptorInvalidPrefix = "بيانات المصدر غير صالحة — الحقل";
    private const string LinkingSourceAyahLimitPrefix = "تجاوز عدد آيات المصدر الحد الأقصى المسموح به";
    private const string LinkingManualAyahIncompletePrefix = "تعذر التحقق من اكتمال كلمات الآية";

    public static string LinkingSourceNotFoundMessage(string? reference) =>
        string.IsNullOrWhiteSpace(reference)
            ? LinkingSourceNotFound
            : $"{LinkingSourceNotFound} «{reference}»";

    public const string LinkingWorkspaceLoaded = "تم تحميل مساحة العمل";
    public const string LinkingWorkspaceSourceAdded = "تمت إضافة المصدر إلى مساحة العمل";
    public const string LinkingWorkspaceSourceRemoved = "تم حذف المصدر من مساحة العمل";
    public const string LinkingWorkspaceSourcesReordered = "تم تحديث ترتيب المصادر";
    public const string LinkingWorkspaceConfigurationSaved = "تم حفظ إعدادات المصدر";
    public const string LinkingWorkspaceCleared = "تم إفراغ مساحة العمل";
    public const string LinkingWorkspaceSourceNotFound = "المصدر غير موجود في مساحة العمل";
    public const string LinkingWorkspaceStaleVersion =
        "تم تعديل البيانات من نافذة أخرى — أعد التحميل ثم أعد المحاولة";
    public const string LinkingWorkspaceDuplicateSource = "هذا المصدر مضاف مسبقًا إلى مساحة العمل";

    private const string LinkingWorkspaceLimitPrefix = "تجاوز عدد المصادر المحضّرة الحد الأقصى المسموح به";
    private const string LinkingWorkspaceInvalidPrefix = "طلب غير صالح — الحقل";
    private const string LinkingWorkspaceReferenceUnknown = "العنصر المشار إليه غير موجود";
    private const string LinkingDescriptionLimitPrefix = "تجاوز عدد الأوصاف الحد الأقصى المسموح به";

    public static string LinkingWorkspaceViolationMessage(LinkingWorkspaceViolation violation) =>
        violation.Code switch
        {
            LinkingWorkspaceViolationCode.PreparedSourceLimitExceeded =>
                $"{LinkingWorkspaceLimitPrefix} ({LinkingLimits.MaxPreparedSources} مصدر)",
            LinkingWorkspaceViolationCode.WordsNotAllowedOnAutomaticSource =>
                "لا يمكن تحديد كلمات يدويًا على مصدر تلقائي",
            LinkingWorkspaceViolationCode.SelectedWordInvalid =>
                $"كلمة محددة غير صالحة «{violation.Value}»",
            LinkingWorkspaceViolationCode.SelectedWordAyahOutsideManualSet =>
                $"الآية «{violation.Value}» ليست ضمن آيات المصدر اليدوي",
            LinkingWorkspaceViolationCode.AyahReferenceUnknown =>
                $"الآية المشار إليها غير موجودة «{violation.Value}»",
            LinkingWorkspaceViolationCode.ConfigurationIncoherent =>
                $"إعدادات المصدر لا تتوافق مع نوعه — الحقل «{violation.Field}»",
            LinkingWorkspaceViolationCode.ReorderSetMismatch =>
                "قائمة الترتيب يجب أن تضم مصادر مساحة العمل نفسها دون زيادة أو نقصان",
            LinkingWorkspaceViolationCode.LabelInvalid =>
                "اسم المصدر مطلوب",
            LinkingWorkspaceViolationCode.VersionRequired =>
                "رقم الإصدار مطلوب مع كل عملية تعديل",
            LinkingWorkspaceViolationCode.ReferenceUnknown => violation.Field is null
                ? LinkingWorkspaceReferenceUnknown
                : $"{LinkingWorkspaceReferenceUnknown} — الحقل «{violation.Field}» بالقيمة «{violation.Value}»",
            LinkingWorkspaceViolationCode.SelectedWordAyahConflict =>
                $"الكلمة «{violation.Value}» محددة على أكثر من آية",
            LinkingWorkspaceViolationCode.DescriptionLimitExceeded =>
                $"{LinkingDescriptionLimitPrefix} ({LinkingLimits.MaxDescriptionsPerSourceAyah} وصف) — الآية «{violation.Value}»",
            LinkingWorkspaceViolationCode.DescriptionBodyInvalid =>
                $"نص الوصف مطلوب ويجب ألا يتجاوز {LinkingLimits.MaxDescriptionLength} حرف — الآية «{violation.Value}»",
            LinkingWorkspaceViolationCode.DescriptionOrderConflict =>
                $"ترتيب الأوصاف يجب أن يكون فريدًا وموجبًا لكل آية — الآية «{violation.Value}»",
            LinkingWorkspaceViolationCode.DescriptionAyahOutsideSource =>
                $"الآية «{violation.Value}» ليست ضمن آيات هذا المصدر",
            _ => violation.Value is null
                ? $"{LinkingWorkspaceInvalidPrefix} «{violation.Field}»"
                : $"{LinkingWorkspaceInvalidPrefix} «{violation.Field}» بالقيمة «{violation.Value}»",
        };

    public static string LinkingDescriptorViolationMessage(LinkingDescriptorViolation violation) =>
        violation.Code switch
        {
            LinkingDescriptorViolationCode.ResolvedAyahLimitExceeded =>
                $"{LinkingSourceAyahLimitPrefix} ({LinkingLimits.MaxResolvedAyahs} آية)",
            LinkingDescriptorViolationCode.ManualAyahCompletenessFailed =>
                $"{LinkingManualAyahIncompletePrefix} «{violation.Value}»",
            _ => violation.Value is null
                ? $"{LinkingSourceDescriptorInvalidPrefix} «{violation.Field}»"
                : $"{LinkingSourceDescriptorInvalidPrefix} «{violation.Field}» بالقيمة «{violation.Value}»",
        };
}
