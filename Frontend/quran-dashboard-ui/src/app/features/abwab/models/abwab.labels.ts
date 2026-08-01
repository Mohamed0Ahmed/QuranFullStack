// Every Arabic string the Abwab feature shows lives here (root CLAUDE.md: Arabic strings
// live only in this file). Consumers read these through TDZ-safe getters — never
// `readonly` field initialisers — per the words/README label-getter rule; that rule binds
// the *consumer*, not this module.

/** Arabic counts do not read like English ones: 1 and 2 have their own forms, 3–10 take the
 * broken plural, and 11+ take the accusative singular. «سيتم أرشفة 1 بابًا» is wrong Arabic,
 * and this product is Arabic-first, so every user-facing count goes through here — one branch,
 * one noun table per counted noun. */
interface ArabicCountForms {
  /** Only where zero has its own wording. Without it, zero falls to `few` («0 عناصر»), which is
   * what a chip on a freshly created template wants — there the numeral is the point. */
  readonly zero?: string;
  readonly one: string;
  readonly two: string;
  /** 3–10, the broken plural. */
  readonly few: string;
  /** 11+, the accusative singular. */
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

const DOOR_FORMS: ArabicCountForms = {
  zero: 'لا أبواب',
  one: 'باب واحد',
  two: 'بابين',
  few: 'أبواب',
  many: 'بابًا',
};

/** A template node. */
const ELEMENT_FORMS: ArabicCountForms = { one: 'عنصر واحد', two: 'عنصرين', few: 'عناصر', many: 'عنصرًا' };

/** Feminine «علاقة». */
const RELATION_FORMS: ArabicCountForms = { one: 'علاقة واحدة', two: 'علاقتين', few: 'علاقات', many: 'علاقة' };

/** The doors a template copy targets — the noun carries its adjective, so the phrase stays
 * grammatical at every count instead of interpolating a bare numeral before «مستهدف». */
const TARGET_FORMS: ArabicCountForms = {
  one: 'باب مستهدف',
  two: 'بابين مستهدفين',
  few: 'أبواب مستهدفة',
  many: 'بابًا مستهدفًا',
};

export const ABWAB_LABELS = {
  pageTitle: 'الأبواب',
  pageSubtitle: 'هيكل التصنيفات القرآنية — كل عملية تتم في مكانها.',

  allDoorsTab: 'كل الأبواب',
  sectionTabsAriaLabel: 'أقسام الأبواب',
  // Item 17's stats bar (Slice B2, T1003/T1004): `allDoorsTab` above doubles as the total stat's
  // label — it is already the right noun phrase and reusing it keeps one string per concept. The
  // open-scope stat needs its own, distinct label because it names a different quantity (a
  // total vs. "whatever the current tab shows") and gets its own so the two lines never read as
  // duplicates of each other even when their numbers happen to coincide on «كل الأبواب» itself.
  // Both stats render through the shared `qd-result-count`'s "label: N" idiom (precedent:
  // `explorer-result-count.component.html`, four words pages), which is a data-display convention,
  // not a counted-noun sentence — so neither label goes through `countPhrase`; see
  // features/abwab/README.md for why «سيتم أرشفة 1 بابًا»'s rule does not reach this pattern.
  statOpenScopeDoors: 'أبواب هذا التبويب',
  searchLabel: 'ابحث في الأبواب',
  searchPlaceholder: 'ابحث في الأبواب… (يفلتر الشجرة مباشرة)',
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
  trackingDataHeading: 'بيانات التتبع',
  trackingAddedByLabel: 'أُضيف بواسطة',
  trackingAddedByPlaceholder: '— (يُملأ مع تفعيل الحسابات)',
  trackingApprovedLabel: 'اعتمده',
  trackingApprovedPlaceholder: '— (لم يُعتمد بعد)',
  trackingArchiveLabel: 'الأرشفة',
  trackingArchiveActiveValue: 'نشط',

  movePickerTitleSingle: (doorName: string): string => `نقل «${doorName}»`,
  movePickerTitleBulk: (count: number): string => `نقل ${countPhrase(count, DOOR_FORMS)}`,
  movePickerDescription: 'اختر الوجهة — باب يجعله فرعًا له، أو «كباب رئيسي».',
  asMainDoorOption: 'كباب رئيسي (أعلى الشجرة)',
  noSectionOption: 'بلا قسم',
  moveConfirm: 'نقل',

  sectionsModalTitle: 'إدارة الأقسام',
  sectionNameLabel: 'اسم القسم',
  addSectionButton: 'إضافة قسم',
  renameSectionButton: 'إعادة تسمية',
  deleteSectionButton: 'حذف',

  archiveEmptyMessage: 'لا توجد أبواب مؤرشفة.',
  restoreButton: 'استرجاع',
  restoreParentFirstHint: 'استرجع الأب أولًا',
  restoreDetachedAnnouncement: 'استُرجع الباب خارج قسمه المحذوف',

  bulkConflictMessage: (names: string): string => `فشلت العملية كاملة — حدث تعارض على: ${names}`,
  archiveConfirm: (count: number): string => `سيتم أرشفة ${countPhrase(count, DOOR_FORMS)}`,

  loadErrorFallback: 'تعذر تحميل شجرة الأبواب. حاول مرة أخرى.',
  emptyTreeMessage: 'لا توجد أبواب بعد.',
  loadingTreeMessage: 'جارٍ تحميل شجرة الأبواب...',

  // Relations. Copy is the approved contract's (docs/design-preview/abwab-relations-concept.html),
  // with one deliberate exception: the contract's `TYPE_META.hier.label` («أعم / أخص», :183) and its
  // hint paragraph (:155) violate the locked comprehensiveness-only vocabulary. Both are unreachable
  // in the contract's own rendering, and neither string is reproduced here.
  relationsOp: 'العلاقات',
  relationsFlagLabel: 'علاقات',
  relationsModalTitle: (doorName: string): string => `علاقات «${doorName}»`,
  relationsModalDescription: 'العلاقات المتبادلة تظهر تلقائيًا عند الطرف الآخر. الحذف من هنا يحذفها من الطرفين.',
  relationsEmpty: 'لا توجد علاقات لهذا الباب بعد — أضف أول علاقة من الأسفل.',
  relationsLoadError: 'تعذر تحميل علاقات الباب.',
  relationDeleteAriaLabel: 'حذف العلاقة',

  relationGroupSimilarity: 'تشابه',
  relationGroupOpposition: 'تضاد',
  relationGroupMoreComprehensive: 'أبواب أكثر شمولية',
  relationGroupLessComprehensive: 'أبواب أقل شمولية',

  relationAddTitle: 'إضافة علاقة جديدة',
  relationTypeSimilarity: 'تشابه',
  relationTypeOpposition: 'تضاد',
  relationTypeComprehensiveness: 'شمولية',
  relationDirectionLabel: 'الاتجاه:',
  // «المحدد» = the doors the picker selects, i.e. the TARGETS. Only true in door mode; the
  // anchor-pick pair below exists because the picker changes sides there.
  relationDirectionAnchorMore: 'المحدد أقل شمولية',
  relationDirectionAnchorLess: 'المحدد أكثر شمولية',
  relationDirectionPreview: (doorName: string, anchorIsMore: boolean): string =>
    `الأبواب اللي هتختارها ${anchorIsMore ? 'أقل شمولية' : 'أكثر شمولية'} من «${doorName}»`,
  relationPickerPlaceholder: 'ابحث واختر بابًا أو أكثر… (تقدر تربط كذا باب مرة واحدة)',
  relationPickerExpandAriaLabel: (doorName: string): string => `عرض الأبواب الفرعية لـ«${doorName}»`,
  relationPickerCollapseAriaLabel: (doorName: string): string => `إخفاء الأبواب الفرعية لـ«${doorName}»`,
  relationAlreadyLinked: 'مرتبط بالفعل بهذا النوع',
  relationNoneSelected: 'لم تختر شيئًا بعد',
  relationSelectedSummary: (names: readonly string[]): string => `${names.length} مختار: ${names.join('، ')}`,
  relationAddButton: (count: number): string =>
    count <= 1 ? 'أضف العلاقة' : `أضف ${countPhrase(count, RELATION_FORMS)}`,
  relationsCloseButton: 'إغلاق',

  // Bulk mode opens the same modal with the selection as the fixed target list, so the picker
  // chooses the single anchor instead (plan §7 T602).
  relationsBulkAddOp: 'إضافة علاقة',
  relationsBulkTitle: (count: number): string => `إضافة علاقة لـ ${countPhrase(count, DOOR_FORMS)}`,
  relationsBulkAnchorHint: 'اختر الباب الذي ترتبط به الأبواب المحددة',
  // The pill must name the side the picker chooses, and in this mode that is the ANCHOR, not the
  // targets. Reusing the door-mode pair here would state the opposite of what the row stores:
  // `anchor-more` makes the picked door the more comprehensive endpoint (plan §5.3).
  relationsBulkDirectionAnchorMore: 'الباب المختار أكثر شمولية',
  relationsBulkDirectionAnchorLess: 'الباب المختار أقل شمولية',
  relationsBulkDirectionPreview: (count: number, doorName: string, anchorIsMore: boolean): string =>
    `الأبواب المحددة (${count}) هتبقى ${anchorIsMore ? 'أقل شمولية' : 'أكثر شمولية'} من «${doorName}»`,

  // Templates workshop. Copy is the approved contract's
  // (docs/design-preview/abwab-templates-concept.html) except where plan §9 names a line the
  // decisions overrule: «إعادة تسمية» (`:127`) becomes «تعديل القالب», because the root is edited
  // through the full authoring modal like every other node. No conflict copy is authored here —
  // the write path prefers the backend's own Arabic message, so a duplicate string would be dead.
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
  templatesLoadError: 'تعذر تحميل القوالب. حاول مرة أخرى.',
  templateLoadError: 'تعذر تحميل القالب.',

  templateAddChildAriaLabel: (nodeName: string): string => `إضافة عنصر تحت «${nodeName}»`,
  templateNodeMenuAriaLabel: (nodeName: string): string => `عمليات «${nodeName}»`,
  templateAddChildPlaceholder: 'إضافة عنصر… (Enter)',
  // Not the copy modal's pair: there the chevron opens sub-doors, here it opens the template's
  // own sub-elements, and a screen reader must hear the noun the tree actually holds.
  templateNodeExpandAriaLabel: (nodeName: string): string => `عرض العناصر الفرعية لـ«${nodeName}»`,
  templateNodeCollapseAriaLabel: (nodeName: string): string => `إخفاء العناصر الفرعية لـ«${nodeName}»`,
  templateNodeEditOp: 'تعديل العنصر',
  templateNodeAddChildOp: 'إضافة عنصر فرعي',
  templateNodeDeleteOp: 'حذف العنصر',
  templateNodeDeleteConfirm: (nodeName: string): string =>
    `سيتم حذف «${nodeName}» وكل العناصر التي تحته.`,
  templateDeleteConfirm: 'سيتم حذف القالب نهائيًا. الأبواب المنسوخة منه لن تتأثر.',
  deleteConfirmButton: 'حذف',

  addTemplateNodeTitle: 'إضافة عنصر للقالب',
  editTemplateNodeTitle: 'تعديل عنصر القالب',
  templateNodeContextRoot: 'جذر القالب — اسمه هو اسم القالب',
  templateNodeContextParent: (parentName: string): string => `سيُضاف تحت: «${parentName}»`,
  templateNodeContextEdit: (nodeName: string): string => `تعديل «${nodeName}»`,

  templateCopyTitle: (templateName: string): string => `نسخ «${templateName}»`,
  templateCopyDescription: 'اختر الأبواب المستهدفة — القالب سيُنسخ كاملًا (بجذره وكل فروعه) داخل كل باب تختاره.',
  templateCopyPreview: (templateName: string, count: number): string =>
    `كل باب مستهدف سيكسب ابنًا جديدًا: «${templateName}» وبداخله ${countPhrase(count, ELEMENT_FORMS)} بكامل تفرعها.`,
  templateCopyPreviewNoRoot: 'لا يمكن النسخ كباب رئيسي — الهدف بابٌ موجود دائمًا.',
  // The single most likely wrong expectation this feature invites (plan §5.6), so it is stated
  // before the copy happens rather than discovered afterwards.
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
