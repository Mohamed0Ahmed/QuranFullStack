import { DetailFrameKind } from '../../../core/navigation/detail-overlay/detail-overlay.models';

/**
 * Arabic labels for the global entity-detail overlay (Feature 029, Change B).
 * Kind titles are the dialog fallback while an entity title loads; terminology
 * follows the explorers (stem = الأصل الصرفي, lemma = الصيغة المعجمية).
 */
export const ENTITY_DETAIL_KIND_TITLES: Record<DetailFrameKind, string> = {
  root: 'تفاصيل الجذر',
  lemma: 'تفاصيل الصيغة المعجمية',
  stem: 'تفاصيل الأصل الصرفي',
  unique: 'تفاصيل الكلمة',
  wordType: 'تفاصيل كلمة النوع',
};

/**
 * Short kind labels for the dialog header chip. Unlike `ENTITY_DETAIL_KIND_TITLES`
 * these are not a title fallback: the chip names the entity kind beside the real
 * title, so it stays synchronous with the frame and never waits on a load.
 */
export const ENTITY_DETAIL_KIND_LABELS: Record<DetailFrameKind, string> = {
  root: 'جذر',
  lemma: 'صيغة معجمية',
  stem: 'أصل صرفي',
  unique: 'كلمة',
  wordType: 'نوع الكلمة',
};

/**
 * Header meta for the entity's ayah count. Latin digits match the explorer
 * tables. The count is entity-level and does not track the ayah-tab `typeCode`
 * filter (Feature 030, N6-b).
 */
export function entityDetailAyahCountText(count: number): string {
  return `الآيات: ${count}`;
}

export const ENTITY_DETAIL_BACK_LABEL = 'رجوع';

export const ENTITY_DETAIL_CLOSE_LABEL = 'إغلاق';

export const ENTITY_DETAIL_RESTORE_LABEL = 'استعادة التفاصيل';

export function entityDetailRestoreAriaLabel(title: string): string {
  return `استعادة ${title}`;
}

export const ENTITY_DETAIL_CAP_STATUS_MESSAGE = 'لا يمكن فتح أكثر من ثماني بطاقات تفاصيل.';

/** Placeholder body for adapters whose full detail rendering lands in a later phase. */
export const ENTITY_DETAIL_STUB_PLACEHOLDER = 'سيتوفر عرض هذه التفاصيل هنا لاحقًا.';
