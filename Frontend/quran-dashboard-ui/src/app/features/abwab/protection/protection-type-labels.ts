import { ManualProtectionScope, ManualProtectionType } from '../../../core/api/generated/models';

export const MANUAL_PROTECTION_TYPE_LABELS: Record<ManualProtectionType, string> = {
  0: 'بيانات الباب',
  1: 'البنية الداخلية',
  2: 'محتوى قرآني',
  3: 'الحذف',
  4: 'العلاقات',
};

export const MANUAL_PROTECTION_SCOPE_LABELS: Record<ManualProtectionScope, string> = {
  0: 'الباب فقط',
  1: 'الباب والشجرة الفرعية',
};
