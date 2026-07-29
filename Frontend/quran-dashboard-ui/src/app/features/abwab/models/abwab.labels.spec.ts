import { describe, expect, it } from 'vitest';

import { ABWAB_LABELS } from './abwab.labels';

// Pins the four locked strings from plan-slice-b.md §2 (items 2, 3, 6) and the
// section-delete conflict message — every consumer must read these, never restate them.
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

  it('section-delete-with-live-doors conflict', () => {
    expect(ABWAB_LABELS.sectionDeleteConflict).toBe('القسم يحتوي أبوابًا نشطة');
  });

  it('the live-subtree archive confirm names the count', () => {
    expect(ABWAB_LABELS.archiveConfirm(3)).toBe('سيتم أرشفة 3 بابًا');
  });
});
