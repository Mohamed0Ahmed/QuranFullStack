import { describe, expect, it } from 'vitest';

import { ABWAB_LABELS } from './abwab.labels';

// Pins the locked strings from plan-slice-b.md §2 (items 2, 3, 6) — every consumer must
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
});
