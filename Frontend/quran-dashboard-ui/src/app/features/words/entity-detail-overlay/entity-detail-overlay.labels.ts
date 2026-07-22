import { DetailFrameKind } from '../../../core/navigation/detail-overlay/detail-overlay.models';

export const ENTITY_DETAIL_KIND_TITLES: Record<DetailFrameKind, string> = {
  root: 'تفاصيل الجذر',
  lemma: 'تفاصيل الصيغة المعجمية',
  stem: 'تفاصيل الأصل الصرفي',
  unique: 'تفاصيل الكلمة',
  wordType: 'تفاصيل كلمة النوع',
};

export const ENTITY_DETAIL_KIND_LABELS: Record<DetailFrameKind, string> = {
  root: 'جذر',
  lemma: 'صيغة معجمية',
  stem: 'أصل صرفي',
  unique: 'كلمة',
  wordType: 'نوع الكلمة',
};

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

export const ENTITY_DETAIL_STUB_PLACEHOLDER = 'سيتوفر عرض هذه التفاصيل هنا لاحقًا.';
