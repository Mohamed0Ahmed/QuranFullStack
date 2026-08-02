import type { AbwabModalKind } from './abwab.models';

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

/** Feminine «علاقة». The zero form exists for the tree row's relations control, which is
 * rendered on every row and has to say "none" rather than «0 علاقات» — the add button, the
 * only other consumer, never reaches `countPhrase` below a count of two. */
const RELATION_FORMS: ArabicCountForms = {
  zero: 'لا علاقات',
  one: 'علاقة واحدة',
  two: 'علاقتين',
  few: 'علاقات',
  many: 'علاقة',
};

/** Tree levels below a door — «مستوى». Zero has its own wording because most rows are leaves,
 * and «0 مستويات» is not how the absence of nesting is said. */
const LEVEL_FORMS: ArabicCountForms = {
  zero: 'لا تفرّع',
  one: 'مستوى واحد',
  two: 'مستويين',
  few: 'مستويات',
  many: 'مستوى',
};

/** The doors a template copy targets — the noun carries its adjective, so the phrase stays
 * grammatical at every count instead of interpolating a bare numeral before «مستهدف». */
const TARGET_FORMS: ArabicCountForms = {
  one: 'باب مستهدف',
  two: 'بابين مستهدفين',
  few: 'أبواب مستهدفة',
  many: 'بابًا مستهدفًا',
};

/** Item 19's badge — ROOT doors, distinct from the plain `DOOR_FORMS` above (item 17's
 * all-depths stat), because the adjective «رئيسي» changes agreement with the count form and
 * cannot be concatenated onto `DOOR_FORMS` (§4.2-13). No zero form: the badge's own zero-state
 * is a visual dim (`--empty`), not a distinct accessible phrase. */
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
  // Item 19's badge digits are Latin and aria-hidden (§17's count-meta convention) — the tab
  // itself carries the accessible name, distinct from item 17's shipped stat («أبواب») so a
  // screen reader hears which question is being answered (root doors, not all-depth doors).
  tabRootCountAriaLabel: (sectionName: string, count: number): string =>
    `«${sectionName}»: ${countPhrase(count, ROOT_DOOR_FORMS)}`,
  allDoorsTabRootCountAriaLabel: (count: number): string => `كل الأبواب: ${countPhrase(count, ROOT_DOOR_FORMS)}`,
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
  // The flag renders on every row now, including rows with none, so the count has to be in
  // the name — otherwise a screen reader hears the same «علاقات» on every row in the tree.
  rowRelationsAriaLabel: (doorName: string, count: number): string =>
    `عرض علاقات «${doorName}» — ${countPhrase(count, RELATION_FORMS)}`,

  // The row's three count badges. These aria-labels are the accessible layer — the visible
  // column header is `aria-hidden` and presentational, so meaning still lives here; «تحته
  // مباشرة» vs «في كل المستويات» is exactly the distinction the two counts make, and the depth
  // label has to pin "levels below this door" rather than the door's own position in the tree.
  rowChildCountAriaLabel: (count: number): string => `${countPhrase(count, DOOR_FORMS)} تحته مباشرة`,
  rowDescendantCountAriaLabel: (count: number): string =>
    `${countPhrase(count, DOOR_FORMS)} تحته في كل المستويات`,
  rowDepthAriaLabel: (depth: number): string => `أعمق تفرّع تحته: ${countPhrase(depth, LEVEL_FORMS)}`,
  /** Bare numeral: the «العمق» column header now does the disambiguating the «ع» prefix used to. */
  rowDepthBadge: (depth: number): string => `${depth}`,

  // The tree's aria-hidden column header. Right-to-left, these sit over the badges in DOM
  // order: direct children, then all descendants, then depth.
  //
  // Deliberately shorter than the aria-labels above, which are unchanged. The header is
  // `aria-hidden` and the full meaning already lives in the accessible layer, so these are
  // hints over a column, not the semantic carrier — and a hint may be abbreviated where a name
  // may not. The unabbreviated words («مباشرين» / «الجميع» / «العمق») needed a 2.5rem column and
  // would have taken 55px from the row's name; these need 1.75rem and take 19px.
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
  movePickerChangeSection: 'تغيير القسم',
  moveConfirm: 'نقل',

  sectionsModalTitle: 'إدارة الأقسام',
  sectionNameLabel: 'اسم القسم',
  addSectionButton: 'إضافة قسم',
  renameSectionButton: 'إعادة تسمية',
  deleteSectionButton: 'حذف',
  sectionDeleteConfirmTitle: 'حذف القسم',
  sectionDeleteConfirmBody: (name: string): string => `سيتم حذف القسم «${name}»`,
  // The order trigger's visible content is a bare numeral (the tree's chip convention), so its
  // accessible name has to say both which section and what the number means.
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
  // The backend's bulk 404 is generic («الباب غير موجود») and names no door, but the snapshot
  // knows every door's name and archive state, so the frontend names them itself — the
  // `bulkConflictMessage` precedent one line up. Phrased as a noun phrase after «تعذر العثور
  // على» so it stays grammatical at every count instead of needing verb agreement per form.
  bulkVanishedMessage: (count: number, names: string): string =>
    `فشلت العملية كاملة — تعذر العثور على ${countPhrase(count, DOOR_FORMS)}: ${names}`,
  archiveConfirm: (count: number): string => `سيتم أرشفة ${countPhrase(count, DOOR_FORMS)}`,
  archiveConfirmTitle: 'تأكيد الأرشفة',

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
  // Every group renders several of these buttons at once, so a static name would leave a screen
  // reader with N identical «حذف العلاقة» controls and no way to tell them apart.
  relationDeleteAriaLabel: (doorName: string): string => `حذف العلاقة مع «${doorName}»`,
  // The chip now carries two controls, so neither can rely on the chip's text to say what it
  // does — «إظهار في الشجرة» and «حذف العلاقة» have to be distinguishable by name alone.
  relationRevealAriaLabel: (doorName: string): string => `إظهار «${doorName}» في الشجرة`,
  // The reveal's guard: defensively unreachable (the read hides relations whose endpoint is
  // archived), so this says what did not happen rather than blaming the user.
  revealUnavailable: 'تعذر إظهار الباب — لم يعد موجودًا في الشجرة',

  // Audit item 11. The restore control is the only thing on screen that says which overlay
  // is waiting, so it names the overlay rather than saying «استعادة» alone — and the discard
  // X, having no text of its own, names it too.
  modalKindNames: {
    create: 'إضافة باب رئيسي',
    child: 'إضافة باب فرعي',
    edit: 'تعديل الباب',
    move: 'نقل الباب',
    sections: 'إدارة الأقسام',
    relations: 'علاقات الباب',
  } satisfies Record<AbwabModalKind, string>,
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
  // «المحدد» = the doors the picker selects, i.e. the TARGETS. Only true in door mode; the
  // anchor-pick pair below exists because the picker changes sides there.
  relationDirectionAnchorMore: 'المحدد أقل شمولية',
  relationDirectionAnchorLess: 'المحدد أكثر شمولية',
  relationDirectionPreview: (doorName: string, anchorIsMore: boolean): string =>
    `الأبواب اللي هتختارها ${anchorIsMore ? 'أقل شمولية' : 'أكثر شمولية'} من «${doorName}»`,
  relationPickerPlaceholder: 'ابحث واختر بابًا أو أكثر… (تقدر تربط كذا باب مرة واحدة)',
  relationPickerExpandAriaLabel: (doorName: string): string => `عرض الأبواب الفرعية لـ«${doorName}»`,
  // The shared picker's own "your search matched nothing", distinct from every host's "there is
  // nothing to pick": doors exist, this query just does not reach them.
  pickerNoMatches: 'لا يوجد باب مطابق لبحثك.',
  relationPickerCollapseAriaLabel: (doorName: string): string => `إخفاء الأبواب الفرعية لـ«${doorName}»`,
  // The picker's "there is nothing to pick" for this host — about doors, not about relations, so
  // it cannot reuse `relationsEmpty` above.
  relationPickerEmptyDoors: 'لا توجد أبواب أخرى يمكن ربطها.',
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
  // Door mode's placeholder invites «بابًا أو أكثر»; here exactly one door is choosable, and the
  // control says so in text as well as in its radio semantics.
  relationsBulkAnchorPlaceholder: 'ابحث واختر بابًا واحدًا…',
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
  templateLoadingMessage: 'جارٍ تحميل القالب...',
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
  // Deliberately separate constants from the `*Op` button labels above, even where the string
  // matches: a dialog title and a menu item are different surfaces and must be free to diverge.
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
