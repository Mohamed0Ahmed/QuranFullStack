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

export const ENTITY_DETAIL_BACK_LABEL = 'رجوع';

export const ENTITY_DETAIL_CLOSE_LABEL = 'إغلاق';

export const ENTITY_DETAIL_RESTORE_LABEL = 'استعادة التفاصيل';

export function entityDetailRestoreAriaLabel(title: string): string {
  return `استعادة ${title}`;
}

export const ENTITY_DETAIL_CAP_STATUS_MESSAGE = 'لا يمكن فتح أكثر من ثماني بطاقات تفاصيل.';
