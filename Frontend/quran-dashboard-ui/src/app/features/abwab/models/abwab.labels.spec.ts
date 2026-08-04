import { describe, expect, it } from 'vitest';

import { ABWAB_LABELS } from './abwab.labels';

// Pins the locked Arabic strings — every consumer must
// read these, never restate them. §2's fourth locked string (the section-delete conflict)
// is deliberately absent: the backend sends its own copy on that 409 and the write
// controller prefers it, so a local constant would be dead. See features/abwab/README.md.
describe('ABWAB_LABELS — the locked strings', () => {
  it('restore-behind-archived-parent hint (§2.2)', () => {
    expect(ABWAB_LABELS.restoreParentFirstHint).toBe('استرجع الأب أولًا');
  });

  // No detach to announce any more: a door whose section was retired is restored INTO a section the
  // user picks, so the announcement says the one thing that happened.
  it('restore announcement (§2.3)', () => {
    expect(ABWAB_LABELS.restoreAnnouncement).toBe('استُرجع الباب');
  });

  it('restore modal asks for a replacement section when the original is gone (§5)', () => {
    expect(ABWAB_LABELS.restoreModalRetiredHint).toBe('القسم الأصلي محذوف — اختر قسمًا بديلًا');
    expect(ABWAB_LABELS.restoreModalNoSectionsHint).toBe('لا توجد أقسام حالية — أنشئ قسمًا أولًا');
  });

  it('root create names the section it needs (§5)', () => {
    expect(ABWAB_LABELS.doorModalSectionRequiredError).toBe('اختر قسمًا للباب الرئيسي');
  });

  // The same dead end reads the same on both surfaces: nothing to pick, so say what to do instead of
  // demanding a choice that cannot be made.
  it('root create and restore say the same thing when no live section exists', () => {
    expect(ABWAB_LABELS.doorModalNoSectionsHint).toBe('لا توجد أقسام حالية — أنشئ قسمًا أولًا');
    expect(ABWAB_LABELS.doorModalNoSectionsHint).toBe(ABWAB_LABELS.restoreModalNoSectionsHint);
  });

  it('bulk all-or-nothing conflict message names the failing doors (§2.6)', () => {
    expect(ABWAB_LABELS.bulkConflictMessage('الألوهية، الربوبية')).toBe(
      'فشلت العملية كاملة — حدث تعارض على: الألوهية، الربوبية',
    );
  });

  it('the live-subtree archive confirm names the count', () => {
    expect(ABWAB_LABELS.archiveConfirm(3)).toBe('سيتم أرشفة 3 أبواب');
  });

  // Item 17's stats bar (Slice B2, T1004): both labels are static — the shared `qd-result-count`
  // renders "label: N" (a data-display idiom, not a counted-noun sentence, per every words
  // call-site), so neither goes through `countPhrase`. `allDoorsTab` doubles as the total stat's
  // label; the open-scope stat gets its own so the two lines are never textually identical.
  it('the stats bar labels are distinct and neither interpolates a count', () => {
    expect(ABWAB_LABELS.allDoorsTab).toBe('كل الأبواب');
    expect(ABWAB_LABELS.statOpenScopeDoors).toBe('أبواب هذا التبويب');
    expect(ABWAB_LABELS.statOpenScopeDoors).not.toBe(ABWAB_LABELS.allDoorsTab);
  });
});

// Arabic counts have singular, dual, 3–10 and 11+ forms; «سيتم أرشفة 1 بابًا» is wrong
// Arabic and this product is Arabic-first (PRODUCT.md). Both counted labels share one
// helper, so both are pinned here.
describe('ABWAB_LABELS — Arabic number agreement on counted doors', () => {
  it.each([
    [0, 'سيتم أرشفة لا أبواب', 'نقل لا أبواب'],
    [1, 'سيتم أرشفة باب واحد', 'نقل باب واحد'],
    [2, 'سيتم أرشفة بابين', 'نقل بابين'],
    [3, 'سيتم أرشفة 3 أبواب', 'نقل 3 أبواب'],
    [10, 'سيتم أرشفة 10 أبواب', 'نقل 10 أبواب'],
    [11, 'سيتم أرشفة 11 بابًا', 'نقل 11 بابًا'],
  ])('count %i reads correctly in both the archive confirm and the move title', (count, archive, move) => {
    expect(ABWAB_LABELS.archiveConfirm(count)).toBe(archive);
    expect(ABWAB_LABELS.movePickerTitleBulk(count)).toBe(move);
  });
});

// F-60: the bulk bar is a counted-door sentence, not a stat tile, so it goes through
// countPhrase like every other counted door surface. SELECTED_DOOR_FORMS carries the
// adjective «محدد» with its own agreement per count form, like ROOT_DOOR_FORMS does for «رئيسي».
describe('ABWAB_LABELS — Arabic number agreement on the bulk-selected count', () => {
  it.each([
    [0, 'لا أبواب محددة'],
    [1, 'باب محدد واحد'],
    [2, 'بابان محددان'],
    [3, '3 أبواب محددة'],
    [10, '10 أبواب محددة'],
    [11, '11 بابًا محددًا'],
  ])('count %i reads correctly in the bulk bar', (count, expected) => {
    expect(ABWAB_LABELS.bulkSelectedCount(count)).toBe(expected);
  });
});

// ux-slice-l's search count — RESULT_FORMS, a counted noun of its own. Zero has a distinct
// wording here (unlike the tab badge below): «0 نتائج» beside a search box reads as a broken
// control rather than an answer.
describe('ABWAB_LABELS — Arabic number agreement on the search match count', () => {
  it.each([
    [0, 'لا توجد نتائج'],
    [1, 'نتيجة واحدة'],
    [2, 'نتيجتان'],
    [3, '3 نتائج'],
    [10, '10 نتائج'],
    [11, '11 نتيجة'],
  ])('count %i reads correctly', (count, expected) => {
    expect(ABWAB_LABELS.searchMatchCount(count)).toBe(expected);
  });
});

// Item 19's badge — ROOT_DOOR_FORMS, distinct from DOOR_FORMS (item 17) because the adjective
// «رئيسي» carries its own agreement per count form (§4.2-13). No zero form is pinned: the
// badge's zero-state is a visual dim, not a distinct accessible phrase.
describe('ABWAB_LABELS — root-door count agreement on the tab badge (§4.2-13)', () => {
  it.each([
    [0, '«اللغة العربية»: 0 أبواب رئيسية'],
    [1, '«اللغة العربية»: باب رئيسي واحد'],
    [2, '«اللغة العربية»: بابان رئيسيان'],
    [3, '«اللغة العربية»: 3 أبواب رئيسية'],
    [10, '«اللغة العربية»: 10 أبواب رئيسية'],
    [11, '«اللغة العربية»: 11 بابًا رئيسيًا'],
  ])('count %i reads correctly in a section tab’s accessible name', (count, expected) => {
    expect(ABWAB_LABELS.tabRootCountAriaLabel('اللغة العربية', count)).toBe(expected);
  });

  it('«كل الأبواب» names itself rather than a section', () => {
    expect(ABWAB_LABELS.allDoorsTabRootCountAriaLabel(8)).toBe('كل الأبواب: 8 أبواب رئيسية');
  });

  describe('the migrated confirm dialogs', () => {
    it('the section-delete confirm names the section it would remove', () => {
      expect(ABWAB_LABELS.sectionDeleteConfirmBody('اللغة العربية')).toBe('سيتم حذف القسم «اللغة العربية»');
    });

    it.each([
      ['archiveConfirmTitle', ABWAB_LABELS.archiveConfirmTitle, 'تأكيد الأرشفة'],
      ['sectionDeleteConfirmTitle', ABWAB_LABELS.sectionDeleteConfirmTitle, 'حذف القسم'],
      ['templateDeleteConfirmTitle', ABWAB_LABELS.templateDeleteConfirmTitle, 'حذف القالب'],
      ['templateNodeDeleteConfirmTitle', ABWAB_LABELS.templateNodeDeleteConfirmTitle, 'حذف العنصر'],
    ])('%s titles its dialog', (_key, actual, expected) => {
      expect(actual).toBe(expected);
    });

    // Same string today, separate constants on purpose: a dialog title and a menu item are
    // different surfaces and must be free to diverge without one silently changing the other.
    it('the node-delete title is not an alias of the menu item', () => {
      expect(ABWAB_LABELS.templateNodeDeleteConfirmTitle).toBe(ABWAB_LABELS.templateNodeDeleteOp);
    });
  });
});

// F-52's one success policy: short past-tense verb-first fusha, no exclamation, matching the
// existing register («استُرجع الباب», «أُنشئ القالب»). The bulk phrases follow the feature's own
// «سيتم أرشفة X» construction so the genitive counted noun stays grammatical.
describe('ABWAB_LABELS — write success announcements', () => {
  it.each([
    ['doorCreatedAnnouncement', ABWAB_LABELS.doorCreatedAnnouncement, 'أُنشئ الباب'],
    ['doorUpdatedAnnouncement', ABWAB_LABELS.doorUpdatedAnnouncement, 'حُدّث الباب'],
    ['doorArchivedAnnouncement', ABWAB_LABELS.doorArchivedAnnouncement, 'أُرشف الباب'],
    ['sectionDeletedAnnouncement', ABWAB_LABELS.sectionDeletedAnnouncement, 'حُذف القسم'],
    ['doorMovedAnnouncement', ABWAB_LABELS.doorMovedAnnouncement, 'نُقل الباب'],
    ['doorReorderedAnnouncement', ABWAB_LABELS.doorReorderedAnnouncement, 'أُعيد ترتيب الباب'],
    ['sectionCreatedAnnouncement', ABWAB_LABELS.sectionCreatedAnnouncement, 'أُنشئ القسم'],
    ['sectionRenamedAnnouncement', ABWAB_LABELS.sectionRenamedAnnouncement, 'أُعيدت تسمية القسم'],
    ['sectionReorderedAnnouncement', ABWAB_LABELS.sectionReorderedAnnouncement, 'أُعيد ترتيب القسم'],
    ['relationDeletedAnnouncement', ABWAB_LABELS.relationDeletedAnnouncement, 'حُذفت العلاقة'],
  ])('%s is the locked phrase', (_key, actual, expected) => {
    expect(actual).toBe(expected);
  });

  it.each([
    [1, 'تمت أرشفة باب واحد', 'تم نقل باب واحد'],
    [2, 'تمت أرشفة بابين', 'تم نقل بابين'],
    [3, 'تمت أرشفة 3 أبواب', 'تم نقل 3 أبواب'],
    [11, 'تمت أرشفة 11 بابًا', 'تم نقل 11 بابًا'],
  ])('the bulk phrases agree with count %i', (count, archive, move) => {
    expect(ABWAB_LABELS.bulkArchiveAnnouncement(count)).toBe(archive);
    expect(ABWAB_LABELS.bulkMoveAnnouncement(count)).toBe(move);
  });

  it.each([
    [1, 'تمت إضافة علاقة واحدة'],
    [2, 'تمت إضافة علاقتين'],
    [3, 'تمت إضافة 3 علاقات'],
    [11, 'تمت إضافة 11 علاقة'],
  ])('the added-relations phrase agrees with count %i', (count, expected) => {
    expect(ABWAB_LABELS.relationsAddedAnnouncement(count)).toBe(expected);
  });
});
