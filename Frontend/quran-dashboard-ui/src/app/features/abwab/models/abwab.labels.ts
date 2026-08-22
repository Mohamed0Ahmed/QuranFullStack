import type { AbwabModalKind, AbwabRelationGroupKey } from './abwab.models';

interface ArabicCountForms {
  readonly zero?: string;
  readonly one: string;
  readonly two: string;
  readonly few: string;
  readonly many: string;
}

function countPhrase(count: number, forms: ArabicCountForms): string {
  if (count === 0 && forms.zero !== undefined) {
    return forms.zero;
  }
  if (count === 1) {
    return forms.one;
  }
  if (count === 2) {
    return forms.two;
  }
  return `${count} ${count <= 10 ? forms.few : forms.many}`;
}

const RESULT_FORMS: ArabicCountForms = {
  zero: 'لا توجد نتائج',
  one: 'نتيجة واحدة',
  two: 'نتيجتان',
  few: 'نتائج',
  many: 'نتيجة',
};

const DOOR_FORMS: ArabicCountForms = {
  zero: 'لا أبواب',
  one: 'باب واحد',
  two: 'بابين',
  few: 'أبواب',
  many: 'بابًا',
};

const ELEMENT_FORMS: ArabicCountForms = { one: 'عنصر واحد', two: 'عنصرين', few: 'عناصر', many: 'عنصرًا' };

const RELATION_FORMS: ArabicCountForms = {
  zero: 'لا علاقات',
  one: 'علاقة واحدة',
  two: 'علاقتين',
  few: 'علاقات',
  many: 'علاقة',
};

const LEVEL_FORMS: ArabicCountForms = {
  zero: 'لا تفرّع',
  one: 'مستوى واحد',
  two: 'مستويين',
  few: 'مستويات',
  many: 'مستوى',
};

const TARGET_FORMS: ArabicCountForms = {
  one: 'باب مستهدف',
  two: 'بابين مستهدفين',
  few: 'أبواب مستهدفة',
  many: 'بابًا مستهدفًا',
};

const ROOT_DOOR_FORMS: ArabicCountForms = {
  one: 'باب رئيسي واحد',
  two: 'بابان رئيسيان',
  few: 'أبواب رئيسية',
  many: 'بابًا رئيسيًا',
};

const SELECTED_DOOR_FORMS: ArabicCountForms = {
  zero: 'لا أبواب محددة',
  one: 'باب محدد واحد',
  two: 'بابان محددان',
  few: 'أبواب محددة',
  many: 'بابًا محددًا',
};

export const ABWAB_LABELS = {
  pageTitle: 'الأبواب',
  pageSubtitle: 'هيكل التصنيفات القرآنية — كل عملية تتم في مكانها.',

  allDoorsTab: 'كل الأبواب',
  sectionTabsAriaLabel: 'أقسام الأبواب',
  tabRootCountAriaLabel: (sectionName: string, count: number): string =>
    `«${sectionName}»: ${countPhrase(count, ROOT_DOOR_FORMS)}`,
  allDoorsTabRootCountAriaLabel: (count: number): string => `كل الأبواب: ${countPhrase(count, ROOT_DOOR_FORMS)}`,
  statOpenScopeDoors: 'أبواب هذا التبويب',
  searchLabel: 'ابحث في الأبواب',
  searchPlaceholder: 'ابحث في الأبواب…',
  searchMatchCount: (count: number): string => countPhrase(count, RESULT_FORMS),
  searchResultsAriaLabel: 'نتائج البحث في الأبواب',
  searchResultAriaLabel: (doorName: string): string => `الانتقال إلى باب «${doorName}»`,
  searchScopeHintTree: 'يبحث في أسماء الأبواب وأسمائها البديلة',
  searchScopeHintCards: 'يبحث في أسماء الأبواب وأسمائها البديلة',
  searchScopeHintArchive: 'يبحث في أسماء الأبواب المؤرشفة وأسمائها البديلة',
  hideUnrelatedRootsLabel: 'إخفاء الأبواب الرئيسية بلا نتائج',
  viewToggleTree: 'شجرة',
  viewToggleCards: 'بطاقات',
  viewToggleAriaLabel: 'طريقة العرض',
  treeExpansionGroupAriaLabel: 'التحكم في فتح شجرة الأبواب',
  treeExpandAll: 'فتح الكل',
  treeCollapseAll: 'طي الكل',
  treeExpandBranch: 'فتح الفرع بالكامل',
  treeCollapseBranch: 'طي الفرع بالكامل',
  treeExpansionSearchDisabledHint: 'امسح البحث للتحكم في فتح الشجرة',
  archiveButton: 'الأرشيف',
  manageSectionsButton: 'إدارة الأقسام',
  addRootDoorButton: 'باب رئيسي جديد',
  addRootGhost: 'إضافة باب رئيسي',

  treeAriaLabel: 'شجرة الأبواب',
  archiveTreeAriaLabel: 'شجرة الأبواب المؤرشفة',
  rowAddChildAriaLabel: (doorName: string): string => `إضافة باب فرعي تحت «${doorName}»`,
  rowMenuAriaLabel: (doorName: string): string => `عمليات «${doorName}»`,
  rowDetailsAriaLabel: (doorName: string): string => `تفاصيل «${doorName}»`,
  rowRelationsAriaLabel: (doorName: string, count: number): string =>
    `عرض علاقات «${doorName}» — ${countPhrase(count, RELATION_FORMS)}`,
  rowLinksAriaLabel: (doorName: string, count: number): string =>
    `عرض روابط «${doorName}» — ${count} سجل`,
  rowPositionsAriaLabel: (doorName: string, count: number): string =>
    `مواضع الكلمات المحددة في «${doorName}» — ${count}`,
  rowOrderEditAriaLabel: (doorName: string, order: number): string =>
    `تعديل ترتيب «${doorName}» — الترتيب الحالي ${order}`,
  inclusionsContextMenuLabel: (sourceCount: number, consumerCount: number): string =>
    `إدارة مصادر الباب، مصادر: ${sourceCount}، أبواب مستفيدة: ${consumerCount}`,
  inclusionsContextMenuCounts: (sourceCount: number, consumerCount: number): string =>
    `مصادر: ${sourceCount}، مستفيدة: ${consumerCount}`,
  archivedInclusionsButton: 'مصادر الباب',
  archivedInclusionsAriaLabel: (doorName: string, sourceCount: number, consumerCount: number): string =>
    `عرض مصادر «${doorName}» للقراءة فقط، مصادر: ${sourceCount}، أبواب مستفيدة: ${consumerCount}`,

  rowChildCountAriaLabel: (count: number): string => `${countPhrase(count, DOOR_FORMS)} تحته مباشرة`,
  rowDescendantCountAriaLabel: (count: number): string =>
    `${countPhrase(count, DOOR_FORMS)} تحته في كل المستويات`,
  rowDepthAriaLabel: (depth: number): string => `أعمق تفرّع تحته: ${countPhrase(depth, LEVEL_FORMS)}`,
  rowDepthBadge: (depth: number): string => `${depth}`,

  rowHeaderDirect: 'مباشر',
  rowHeaderPositions: 'المواضع',
  rowHeaderLinks: 'الروابط',
  rowHeaderTotal: 'الكل',
  rowHeaderDepth: 'عمق',
  rowHeaderRelations: 'العلاقات',

  activeDoorHeading: 'الباب النشط',
  noSelectionHint: 'اختر بابًا من الشجرة أو البطاقات',
  clearSelection: 'مسح',
  operationsHeading: 'العمليات',
  bulkToggleLabel: 'تحديد جماعي',
  addChildOp: 'إضافة باب فرعي',
  editOp: 'تعديل التفاصيل',
  moveOp: 'نقل إلى…',
  archiveOp: 'أرشفة',

  bulkSelectedCount: (count: number): string => countPhrase(count, SELECTED_DOOR_FORMS),
  bulkMoveAll: 'نقل الكل إلى…',
  bulkArchiveAll: 'أرشفة الكل',
  bulkClear: 'إلغاء التحديد',

  addDoorTitle: 'إضافة باب جديد',
  editDoorTitle: 'تعديل تفاصيل الباب',
  contextRoot: 'سيُضاف كباب رئيسي',
  contextParent: (parentName: string): string => `سيُضاف تحت: «${parentName}»`,
  contextEdit: (doorName: string): string => `تعديل «${doorName}»`,
  nameFieldLabel: 'اسم الباب',
  descriptionFieldLabel: 'وصف الباب',
  descriptionPlaceholder: 'وصف مختصر يظهر للمشرفين…',
  ayahFieldLabel: 'آية تمثل الباب',
  ayahPlaceholder: 'نص حر — مقتطف يمثّل الباب',
  ayahHint: 'نص يكتبه المشرف، وليس مرجعًا قرآنيًا مُتحقَّقًا.',
  aliasFieldLabel: 'أسماء الباب للبحث',
  aliasPlaceholder: 'اكتب اسمًا واضغط Enter لإضافته',
  removeAliasAriaLabel: (alias: string): string => `إزالة ${alias}`,
  saveButton: 'حفظ',
  cancelButton: 'إلغاء',
  retryButton: 'إعادة المحاولة',
  dirtyCloseConfirm: 'هناك تغييرات غير محفوظة — هل تريد تجاهلها؟',
  discardChangesButton: 'تجاهل التغييرات',
  keepEditingButton: 'متابعة التعديل',
  nameRequiredError: 'اسم الباب مطلوب',
  doorModalSectionLabel: 'القسم',
  doorModalSectionRequiredError: 'اختر قسمًا للباب الرئيسي',
  doorModalNoSectionsHint: 'لا توجد أقسام حالية — أنشئ قسمًا أولًا',

  movePickerTitleSingle: (doorName: string): string => `نقل «${doorName}»`,
  movePickerTitleBulk: (count: number): string => `نقل ${countPhrase(count, DOOR_FORMS)}`,
  movePickerDescription: 'اختر الوجهة — باب يجعله فرعًا له، أو «كباب رئيسي».',
  asMainDoorOption: 'كباب رئيسي (أعلى الشجرة)',
  movePickerSectionStripLabel: 'الأقسام',
  movePickerSearchPlaceholder: 'ابحث عن باب في هذا القسم…',
  movePickerPickSectionHint: 'اختر قسمًا لعرض أبوابه',
  moveConfirm: 'نقل',

  sectionsModalTitle: 'إدارة الأقسام',
  sectionNameLabel: 'اسم القسم',
  addSectionButton: 'إضافة قسم',
  renameSectionButton: 'إعادة تسمية',
  deleteSectionButton: 'حذف',
  sectionDeleteConfirmTitle: 'حذف القسم',
  sectionDeleteConfirmBody: (name: string): string => `سيتم حذف القسم «${name}»`,
  sectionOrderAriaLabel: (sectionName: string, order: number): string => `ترتيب «${sectionName}»: ${order}`,
  sectionOrderInputAriaLabel: (sectionName: string): string => `أدخل ترتيبًا جديدًا لـ«${sectionName}»`,

  archiveEmptyMessage: 'لا توجد أبواب مؤرشفة.',
  archiveNoSearchMatchesMessage: 'لا يوجد باب مؤرشف مطابق لبحثك.',
  restoreButton: 'استرجاع',
  restoreParentFirstHint: 'استرجع الأب أولًا',
  restorePermissionHint: 'لا تملك صلاحية استرجاع الأبواب المؤرشفة.',
  restoreAnnouncement: 'استُرجع الباب',
  restoreModalTitle: 'استرجاع الباب',
  restoreModalSectionLabel: 'القسم بعد الاسترجاع',
  restoreModalRetiredHint: 'القسم الأصلي محذوف — اختر قسمًا بديلًا',
  restoreModalNoSectionsHint: 'لا توجد أقسام حالية — أنشئ قسمًا أولًا',
  restoreModalChildHint: 'يعود الباب تحت أبيه، في قسمه.',
  restoreModalConfirm: 'استرجاع',
  restoreModalCancel: 'إلغاء',

  bulkConflictMessage: (names: string): string => `فشلت العملية كاملة — حدث تعارض على: ${names}`,
  bulkVanishedMessage: (count: number, names: string): string =>
    `فشلت العملية كاملة — تعذر العثور على ${countPhrase(count, DOOR_FORMS)}: ${names}`,
  archiveConfirm: (count: number): string => `سيتم أرشفة ${countPhrase(count, DOOR_FORMS)}`,
  archiveConfirmTitle: 'تأكيد الأرشفة',

  loadErrorFallback: 'تعذر تحميل شجرة الأبواب. حاول مرة أخرى.',
  emptyTreeMessage: 'لا توجد أبواب بعد.',
  noSearchMatchesMessage: 'لا يوجد باب مطابق لبحثك.',
  loadingTreeMessage: 'جارٍ تحميل شجرة الأبواب...',

  doorLinksLoadError: 'تعذر تحميل روابط الباب.',
  doorLinkAyahsLoadError: 'تعذر تحميل آيات الرابط.',
  doorLinksStale: 'تغيرت روابط الباب. تم تحديث البيانات، فراجعها قبل إعادة المحاولة.',
  doorLinkWordsSaveError: 'تعذر حفظ الكلمات المحددة.',
  doorLinksDeleteError: 'تعذر حذف روابط الباب.',
  doorLinksHeading: 'روابط الباب',
  doorLinksRecordsCount: (count: number): string => `${count} سجل ربط`,
  doorLinksSelectedCount: (count: number): string => `${count} محدد`,
  doorLinksSelectAll: 'تحديد الكل',
  doorLinksClearSelection: 'مسح التحديد',
  doorLinksGrouped: 'ربط مجمع',
  doorLinksIndependent: 'ربط مستقل',
  doorLinksAyahCount: (count: number): string => `${count} آية`,
  doorLinksWordCount: (count: number): string => `${count} كلمة محددة`,
  doorLinksDescriptionCount: (count: number): string => `${count} وصف`,
  doorLinksSources: 'المصادر',
  doorLinksLoading: 'جارٍ تحميل روابط الباب…',
  doorLinksEmpty: 'لا توجد روابط لهذا الباب.',
  doorLinksAyahsEmpty: 'لا توجد آيات في سجل الربط.',
  doorLinksOperationsHeading: 'عمليات الروابط',
  doorLinksEdit: 'تعديل الربط',
  doorLinksEditRecordAriaLabel: (surahName: string, ayahNumber: number): string =>
    `تعديل ربط سورة ${surahName}، الآية ${ayahNumber}`,
  doorLinksDelete: 'حذف الربط',
  doorLinksCopy: 'نسخ الربط',
  doorLinksNoDoorHint: 'افتح روابط باب للبدء',
  doorLinksCopyHeading: 'نسخ روابط الباب',
  doorLinksCopyTarget: 'اختر باب النسخ أولًا',
  doorLinksCopyTargetHint: 'لا يمكن النسخ إلى باب المصدر أو باب غير نشط.',
  doorLinksCopyTargetSearch: 'ابحث عن باب مستهدف…',
  doorLinksCopyTargetEmpty: 'لا توجد أبواب متاحة للنسخ.',
  doorLinksCopySourceDoorTag: 'باب المصدر',
  doorLinksCopyUnavailableTag: 'غير متاح للنسخ',
  doorLinksCopyStart: 'بدء الفحص والنسخ',
  doorLinksCopyClose: 'إلغاء النسخ',
  doorLinksCopyRetry: 'تحديث وإعادة المحاولة',
  doorLinksCopyEnumerating: 'جارٍ حصر سجلات الربط المختارة…',
  doorLinksCopyPreparing: 'جارٍ تحضير جميع آيات النسخ…',
  doorLinksCopyRunning: 'عملية النسخ مفتوحة للمراجعة.',
  doorLinksCopySourceLabel: (doorName: string): string => `نسخ من باب «${doorName}»`,
  doorLinksCopyCompleted: 'اكتمل نسخ روابط الباب بنجاح.',
  doorLinksCopyNoRecords: 'لا توجد سجلات ربط ضمن نطاق النسخ.',
  doorLinksCopyLoadError: 'تعذر تحميل روابط الباب للنسخ.',
  doorLinksCopySourceChanged: 'تغيرت روابط باب المصدر. حدّث البيانات قبل إعادة المحاولة.',
  doorLinksCopyTargetUnavailable: 'باب النسخ المستهدف لم يعد متاحًا.',
  doorLinksCopyInvalid: 'تعذر تحضير سجلات النسخ.',
  doorLinksCopyStartError: 'تعذر بدء عملية النسخ.',
  doorLinksCopyStopped: 'توقفت عملية النسخ.',

  relationsOp: 'العلاقات',
  relationsFlagLabel: 'علاقات',
  relationsModalTitle: (doorName: string): string => `علاقات «${doorName}»`,
  relationsModalDescription: 'العلاقات المتبادلة تظهر تلقائيًا عند الطرف الآخر. الحذف من هنا يحذفها من الطرفين.',
  relationsReadOnlyDescription: 'اختر بابًا مرتبطًا للانتقال إليه في الشجرة.',
  relationsEmpty: 'لا توجد علاقات لهذا الباب بعد — أضف أول علاقة من الأسفل.',
  relationsReadOnlyEmpty: 'لا توجد علاقات لهذا الباب.',
  relationsLoading: 'يتم تحميل العلاقات…',
  relationsLoadError: 'تعذر تحميل علاقات الباب.',
  relationDeleteAriaLabel: (doorName: string): string => `حذف العلاقة مع «${doorName}»`,
  relationRevealAriaLabel: (doorName: string): string => `إظهار «${doorName}» في الشجرة`,
  relationDeleteConfirmTitle: 'حذف العلاقة',
  relationDeleteConfirmBody: (anchorName: string, otherName: string, group: AbwabRelationGroupKey): string => {
    switch (group) {
      case 'similarity':
        return `سيتم حذف علاقة التشابه بين «${anchorName}» و«${otherName}».`;
      case 'opposition':
        return `سيتم حذف علاقة التضاد بين «${anchorName}» و«${otherName}».`;
      case 'more-comprehensive':
        return `سيتم حذف علاقة الشمولية: «${otherName}» أكثر شمولية من «${anchorName}».`;
      case 'less-comprehensive':
        return `سيتم حذف علاقة الشمولية: «${otherName}» أقل شمولية من «${anchorName}».`;
    }
  },
  relationDeleteConfirmSides: 'ستُحذف العلاقة من الطرفين معًا.',
  revealUnavailable: 'تعذر إظهار الباب — لم يعد موجودًا في الشجرة',

  modalKindNames: {
    create: 'إضافة باب رئيسي',
    child: 'إضافة باب فرعي',
    edit: 'تعديل الباب',
    move: 'نقل الباب',
    sections: 'إدارة الأقسام',
    relations: 'علاقات الباب',
    inclusions: 'إدارة مصادر الباب',
  } satisfies Record<AbwabModalKind, string>,
  relationsOfDoorKindName: (doorName: string): string => `علاقات «${doorName}»`,
  inclusionsOfDoorKindName: (doorName: string): string => `مصادر «${doorName}»`,
  modalRestoreLabel: (kindName: string): string => `استعادة ${kindName}`,
  modalDiscardAriaLabel: (kindName: string): string => `تجاهل ${kindName}`,

  relationGroupSimilarity: 'تشابه',
  relationGroupOpposition: 'تضاد',
  relationGroupMoreComprehensive: 'أبواب أكثر شمولية',
  relationGroupLessComprehensive: 'أبواب أقل شمولية',

  relationAddTitle: 'إضافة علاقة جديدة',
  relationTypeTabsAriaLabel: 'نوع العلاقة',
  relationTypeSimilarity: 'تشابه',
  relationTypeOpposition: 'تضاد',
  relationTypeComprehensiveness: 'شمولية',
  relationDirectionLabel: 'الاتجاه:',
  relationDirectionAnchorMore: 'المحدد أقل شمولية',
  relationDirectionAnchorLess: 'المحدد أكثر شمولية',
  relationDirectionPreview: (doorName: string, anchorIsMore: boolean): string =>
    `الأبواب اللي هتختارها ${anchorIsMore ? 'أقل شمولية' : 'أكثر شمولية'} من «${doorName}»`,
  relationPickerPlaceholder: 'ابحث واختر بابًا أو أكثر… (تقدر تربط كذا باب مرة واحدة)',
  relationPickerExpandAriaLabel: (doorName: string): string => `عرض الأبواب الفرعية لـ«${doorName}»`,
  pickerNoMatches: 'لا يوجد باب مطابق لبحثك.',
  pickerExcludedAnchorTag: 'الباب المفتوح',
  pickerExcludedTargetTag: 'هدف محدد',
  relationPickerCollapseAriaLabel: (doorName: string): string => `إخفاء الأبواب الفرعية لـ«${doorName}»`,
  relationPickerEmptyDoors: 'لا توجد أبواب أخرى يمكن ربطها.',
  relationAlreadyLinked: 'مرتبط بالفعل بهذا النوع',
  relationNoneSelected: 'لم تختر شيئًا بعد',
  relationSelectedSummary: (names: readonly string[]): string => `${names.length} مختار: ${names.join('، ')}`,
  relationAddButton: (count: number): string =>
    count <= 1 ? 'أضف العلاقة' : `أضف ${countPhrase(count, RELATION_FORMS)}`,
  relationsCloseButton: 'إغلاق',

  inclusionsOp: 'إدارة مصادر الباب',
  inclusionsModalTitle: 'إدارة مصادر الباب',
  inclusionsModalDescription: 'اختر بابًا أو أكثر لإضافة محتواها الحالي إلى الباب المستهدف في عملية واحدة.',
  inclusionsArchivedTargetDescription: 'هذا الباب مؤرشف. يمكنك مراجعة مصادره فقط، ويظل المحتوى المتزامن محفوظًا.',
  inclusionsTargetLabel: 'الباب المستهدف',
  inclusionsSourcesHeading: 'أبواب المصدر',
  inclusionsConsumersHeading: 'الأبواب المستفيدة',
  inclusionsSourcePickerHeading: 'إضافة أبواب مصدر',
  inclusionsSourceSearch: 'ابحث واختر بابًا أو أكثر…',
  inclusionsTargetTag: 'الباب المستهدف',
  inclusionsExistingSourceTag: 'مصدر مُضاف',
  inclusionsPickerEmpty: 'لا توجد أبواب حية متاحة كمصادر.',
  inclusionsSourcesEmpty: 'لا توجد أبواب مصدر لهذا الباب بعد.',
  inclusionsConsumersEmpty: 'لا توجد أبواب مستفيدة من هذا الباب.',
  inclusionsArchivedStatus: 'مؤرشف',
  inclusionsArchiveExplanation: 'يبقى المحتوى المتزامن محفوظًا عند أرشفة باب مصدر أو باب مستهدف.',
  inclusionsLoading: 'جارٍ تحميل مصادر الباب…',
  inclusionsRefreshing: 'جارٍ تحديث مصادر الباب',
  inclusionsLoadError: 'تعذر تحميل مصادر الباب.',
  inclusionsAddError: 'تعذر إضافة أبواب المصدر المحددة.',
  inclusionsDetachError: 'تعذر فصل باب المصدر.',
  inclusionsAddedNotice: 'تمت إضافة أبواب المصدر بنجاح.',
  inclusionsDetachedNotice: (removedCount: number): string =>
    `تم فصل باب المصدر وإزالة ${removedCount} من السجلات المتزامنة من الباب المستهدف.`,
  inclusionsConflictRefreshed: 'تغير الباب المستهدف. تم تحديث مصادر الباب، فراجع اختيارك قبل المحاولة مجددًا.',
  inclusionsNoneSelected: 'لم تختر بابًا ليكون مصدرًا بعد.',
  inclusionsSelectedSummary: (count: number): string => `تم اختيار ${countPhrase(count, DOOR_FORMS)}`,
  inclusionsAddButton: (count: number): string => count <= 1 ? 'إضافة باب مصدر' : 'إضافة أبواب مصدر',
  inclusionsDetachButton: 'فصل',
  inclusionsDetachAriaLabel: (doorName: string): string => `فصل «${doorName}» عن الباب المستهدف`,
  inclusionsDetachConfirmTitle: 'فصل باب مصدر',
  inclusionsDetachConfirmLabel: 'فصل باب المصدر',
  inclusionsDetachConfirmBody: (sourceName: string, targetName: string): string =>
    `سيُفصل باب المصدر «${sourceName}» عن الباب المستهدف «${targetName}». `
    + 'يبقى الباب المصدر ومحتواه دون تغيير، وتُحذف فقط السجلات المتزامنة '
    + 'التي يملكها هذا المصدر من الباب المستهدف.',
  inclusionsDismissNotice: 'إخفاء التنبيه',
  inclusionsCloseButton: 'إغلاق',

  relationsBulkAddOp: 'إضافة علاقة',
  relationsBulkTitle: (count: number): string => `إضافة علاقة لـ ${countPhrase(count, DOOR_FORMS)}`,
  relationsBulkAnchorHint: 'اختر الباب الذي ترتبط به الأبواب المحددة',
  relationsBulkAnchorPlaceholder: 'ابحث واختر بابًا واحدًا…',
  relationsBulkDirectionAnchorMore: 'الباب المختار أكثر شمولية',
  relationsBulkDirectionAnchorLess: 'الباب المختار أقل شمولية',
  relationsBulkDirectionPreview: (count: number, doorName: string, anchorIsMore: boolean): string =>
    `الأبواب المحددة (${count}) هتبقى ${anchorIsMore ? 'أقل شمولية' : 'أكثر شمولية'} من «${doorName}»`,

  templatesButton: 'القوالب',
  templatesPageTitle: 'قوالب الأبواب',
  templatesPageSubtitle: 'هياكل جاهزة قابلة للعرض؛ تتاح أدوات النسخ والتحرير لأصحاب الصلاحية فقط.',
  backToDoorsButton: '↩ العودة للأبواب',

  newTemplateButton: '+ قالب جديد',
  newTemplateNameLabel: 'اسم القالب',
  newTemplateNamePlaceholder: 'اسم القالب… (Enter)',
  templateElementCount: (count: number): string => countPhrase(count, ELEMENT_FORMS),
  editTemplateButton: 'تعديل القالب',
  deleteTemplateButton: 'حذف القالب',
  copyToDoorsButton: 'نسخ إلى أبواب…',
  templatesListAriaLabel: 'قائمة القوالب',
  templateTreeAriaLabel: 'شجرة القالب',

  templatesEmptyMessage: 'لا توجد قوالب بعد — أنشئ أول قالب من الأعلى.',
  templateNoneSelectedMessage: 'اختر قالبًا من القائمة أو أنشئ قالبًا جديدًا.',
  templatesLoadingMessage: 'جارٍ تحميل القوالب...',
  templateLoadingMessage: 'جارٍ تحميل القالب...',
  templatesLoadError: 'تعذر تحميل القوالب. حاول مرة أخرى.',
  templateLoadError: 'تعذر تحميل القالب.',

  templateAddChildAriaLabel: (nodeName: string): string => `إضافة عنصر تحت «${nodeName}»`,
  templateNodeMenuAriaLabel: (nodeName: string): string => `عمليات «${nodeName}»`,
  templateAddChildPlaceholder: 'إضافة عنصر… (Enter)',
  templateNodeExpandAriaLabel: (nodeName: string): string => `عرض العناصر الفرعية لـ«${nodeName}»`,
  templateNodeCollapseAriaLabel: (nodeName: string): string => `إخفاء العناصر الفرعية لـ«${nodeName}»`,
  templateNodeOrderEditAriaLabel: (nodeName: string, order: number): string =>
    `تعديل ترتيب «${nodeName}» — الترتيب الحالي ${order}`,
  templateNodeEditOp: 'تعديل العنصر',
  templateNodeAddChildOp: 'إضافة عنصر فرعي',
  templateNodeDeleteOp: 'حذف العنصر',
  templateNodeDeleteConfirm: (nodeName: string): string =>
    `سيتم حذف «${nodeName}» وكل العناصر التي تحته.`,
  templateDeleteConfirm: 'سيتم حذف القالب نهائيًا. الأبواب المنسوخة منه لن تتأثر.',
  templateDeleteConfirmTitle: 'حذف القالب',
  templateNodeDeleteConfirmTitle: 'حذف العنصر',
  deleteConfirmButton: 'حذف',

  addTemplateNodeTitle: 'إضافة عنصر للقالب',
  editTemplateNodeTitle: 'تعديل عنصر القالب',
  templateNodeContextRoot: 'جذر القالب — اسمه هو اسم القالب',
  templateNodeContextParent: (parentName: string): string => `سيُضاف تحت: «${parentName}»`,
  templateNodeContextEdit: (nodeName: string): string => `تعديل «${nodeName}»`,

  templateCopyTitle: (templateName: string): string => `نسخ «${templateName}»`,
  templateCopyDescription: 'اختر الأبواب المستهدفة — عناصر القالب (بدون جذره) ستُنسخ داخل كل باب تختاره.',
  templateCopyPreview: (templateName: string, count: number): string =>
    `كل باب مستهدف سيكسب ${countPhrase(count, ELEMENT_FORMS)} من «${templateName}» بكامل تفرعها — جذر القالب نفسه لا يُنسخ.`,
  templateCopyEmptyTemplate: 'هذا القالب لا يحتوي عناصر — أضف عنصرًا واحدًا على الأقل قبل النسخ.',
  templateCopyPreviewNoRoot: 'لا يمكن النسخ كباب رئيسي — الهدف بابٌ موجود دائمًا.',
  templateCopyPreviewDetached: 'النسخ مستقلة عن القالب: تعديل القالب لاحقًا أو حذفه لا يغيّر الأبواب المنسوخة.',
  templateCopySearchPlaceholder: 'ابحث واختر بابًا أو أكثر…',
  templateCopyNoneSelected: 'لم تختر شيئًا بعد',
  templateCopySelectedSummary: (names: readonly string[]): string =>
    `${countPhrase(names.length, TARGET_FORMS)}: ${names.join('، ')}`,
  templateCopyConfirmButton: (count: number): string =>
    count <= 1 ? 'انسخ القالب' : `انسخ إلى ${countPhrase(count, DOOR_FORMS)}`,
  templateCopyEmptyDoors: 'لا توجد أبواب حية لنسخ القالب إليها.',

  templateCreatedAnnouncement: 'أُنشئ القالب',
  templateDeletedAnnouncement: 'حُذف القالب',
  templateAppliedAnnouncement: (count: number): string => `تم النسخ إلى ${countPhrase(count, DOOR_FORMS)}`,

  doorCreatedAnnouncement: 'أُنشئ الباب',
  doorUpdatedAnnouncement: 'حُدّث الباب',
  doorMovedAnnouncement: 'نُقل الباب',
  doorReorderedAnnouncement: 'أُعيد ترتيب الباب',
  doorArchivedAnnouncement: 'أُرشف الباب',
  bulkArchiveAnnouncement: (count: number): string => `تمت أرشفة ${countPhrase(count, DOOR_FORMS)}`,
  bulkMoveAnnouncement: (count: number): string => `تم نقل ${countPhrase(count, DOOR_FORMS)}`,
  sectionCreatedAnnouncement: 'أُنشئ القسم',
  sectionRenamedAnnouncement: 'أُعيدت تسمية القسم',
  sectionReorderedAnnouncement: 'أُعيد ترتيب القسم',
  sectionDeletedAnnouncement: 'حُذف القسم',
  relationsAddedAnnouncement: (count: number): string => `تمت إضافة ${countPhrase(count, RELATION_FORMS)}`,
  relationDeletedAnnouncement: 'حُذفت العلاقة',
  doorLinkWordsUpdatedAnnouncement: 'تم تحديث كلمات الرابط',
  doorLinksDeletedAnnouncement: 'تم حذف الروابط المحددة',

  writeConflictFallback: 'حدث تعارض أثناء الحفظ. يرجى تحديث البيانات والمحاولة مرة أخرى.',
  writeInvalidFallback: 'تعذر تنفيذ العملية. تحقق من البيانات وحاول مرة أخرى.',
  writePermissionDenied: 'لا تملك الصلاحية اللازمة لإتمام هذا الإجراء.',
  writeTransportFallback: 'تعذر الاتصال بالخادم. حاول مرة أخرى.',
} as const;
