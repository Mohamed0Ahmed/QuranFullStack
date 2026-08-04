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
  viewToggleTree: 'شجرة',
  viewToggleCards: 'بطاقات',
  archiveButton: 'الأرشيف',
  manageSectionsButton: 'إدارة الأقسام',
  addRootDoorButton: 'باب رئيسي جديد',
  addRootGhost: 'إضافة باب رئيسي',

  treeAriaLabel: 'شجرة الأبواب',
  archiveTreeAriaLabel: 'شجرة الأبواب المؤرشفة',
  rowAddChildAriaLabel: (doorName: string): string => `إضافة باب فرعي تحت «${doorName}»`,
  rowMenuAriaLabel: (doorName: string): string => `عمليات «${doorName}»`,
  rowRelationsAriaLabel: (doorName: string, count: number): string =>
    `عرض علاقات «${doorName}» — ${countPhrase(count, RELATION_FORMS)}`,

  rowChildCountAriaLabel: (count: number): string => `${countPhrase(count, DOOR_FORMS)} تحته مباشرة`,
  rowDescendantCountAriaLabel: (count: number): string =>
    `${countPhrase(count, DOOR_FORMS)} تحته في كل المستويات`,
  rowDepthAriaLabel: (depth: number): string => `أعمق تفرّع تحته: ${countPhrase(depth, LEVEL_FORMS)}`,
  rowDepthBadge: (depth: number): string => `${depth}`,

  rowHeaderDirect: 'مباشر',
  rowHeaderTotal: 'الكل',
  rowHeaderDepth: 'عمق',

  activeDoorHeading: 'الباب النشط',
  noSelectionHint: 'اختر بابًا من الشجرة أو البطاقات',
  clearSelection: 'مسح',
  operationsHeading: 'العمليات',
  bulkToggleLabel: 'تحديد جماعي',
  addChildOp: 'إضافة باب فرعي',
  editOp: 'تعديل التفاصيل',
  moveOp: 'نقل إلى…',
  archiveOp: 'أرشفة',

  bulkCountSuffix: 'باب محدد',
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
  restoreButton: 'استرجاع',
  restoreParentFirstHint: 'استرجع الأب أولًا',
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
  loadingTreeMessage: 'جارٍ تحميل شجرة الأبواب...',

  relationsOp: 'العلاقات',
  relationsFlagLabel: 'علاقات',
  relationsModalTitle: (doorName: string): string => `علاقات «${doorName}»`,
  relationsModalDescription: 'العلاقات المتبادلة تظهر تلقائيًا عند الطرف الآخر. الحذف من هنا يحذفها من الطرفين.',
  relationsEmpty: 'لا توجد علاقات لهذا الباب بعد — أضف أول علاقة من الأسفل.',
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
  } satisfies Record<AbwabModalKind, string>,
  relationsOfDoorKindName: (doorName: string): string => `علاقات «${doorName}»`,
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
  templatesPageSubtitle: 'هياكل جاهزة تُنسخ داخل أي باب — للمشرفين فقط، لا تظهر للزوار.',
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

  writeConflictFallback: 'حدث تعارض أثناء الحفظ. يرجى تحديث البيانات والمحاولة مرة أخرى.',
  writeInvalidFallback: 'تعذر تنفيذ العملية. تحقق من البيانات وحاول مرة أخرى.',
  writeTransportFallback: 'تعذر الاتصال بالخادم. حاول مرة أخرى.',
} as const;
