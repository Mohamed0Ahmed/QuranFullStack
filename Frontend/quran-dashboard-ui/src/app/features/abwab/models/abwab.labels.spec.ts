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

  it('restore-detach announcement (§2.3)', () => {
    expect(ABWAB_LABELS.restoreDetachedAnnouncement).toBe('استُرجع الباب خارج قسمه المحذوف');
  });

  it('bulk all-or-nothing conflict message names the failing doors (§2.6)', () => {
    expect(ABWAB_LABELS.bulkConflictMessage('الألوهية، الربوبية')).toBe(
      'فشلت العملية كاملة — حدث تعارض على: الألوهية، الربوبية',
    );
  });

  it('the live-subtree archive confirm names the count', () => {
    expect(ABWAB_LABELS.archiveConfirm(3)).toBe('سيتم أرشفة 3 أبواب');
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
