export const ABWAB_CONFLICT_CODES = [
  'abwab.section_name_conflict',
  'abwab.section_not_empty',
  'abwab.permanent_default_section',
  'abwab.category_name_conflict',
  'abwab.category_alias_conflict',
  'abwab.category_cycle',
  'abwab.category_overlapping_move',
  'abwab.category_unavailable',
  'abwab.category_reserved_by_pending',
  'abwab.manual_protection',
  'abwab.manual_protection_scope_conflict',
  'abwab.ordinary_protection',
  'abwab.relationship_duplicate',
  'abwab.relationship_cycle',
  'abwab.stabilization_active',
  'abwab.tree_revision_stale',
  'abwab.timeline_generation_stale',
  'abwab.row_stale',
] as const;

export type AbwabConflictCode = (typeof ABWAB_CONFLICT_CODES)[number];

export function isAbwabConflictCode(value: string): value is AbwabConflictCode {
  return (ABWAB_CONFLICT_CODES as readonly string[]).includes(value);
}

// Thrown by both the core mock and the HTTP adapter for the SAME abwab.* code — the shared
// versioned contract callers (facades/UI) branch on, never a transport-specific shape.
export class AbwabConflictError extends Error {
  constructor(readonly code: AbwabConflictCode) {
    super(ABWAB_CONFLICT_MESSAGES[code]);
    this.name = 'AbwabConflictError';
  }
}

export const ABWAB_CONFLICT_MESSAGES: Record<AbwabConflictCode, string> = {
  'abwab.section_name_conflict': 'اسم القسم مستخدم بالفعل.',
  'abwab.section_not_empty': 'لا يمكن حذف قسم يحتوي أبوابًا رئيسية نشطة.',
  'abwab.permanent_default_section': 'القسم الافتراضي الدائم لا يمكن إعادة تسميته أو حذفه أو تكراره.',
  'abwab.category_name_conflict': 'اسم الباب مستخدم بالفعل في هذا الموضع.',
  'abwab.category_alias_conflict': 'الاسم البديل مستخدم بالفعل لهذا الباب.',
  'abwab.category_cycle': 'لا يمكن نقل الباب إلى داخل نفسه أو أحد أبنائه.',
  'abwab.category_overlapping_move': 'التحديد يتضمن بابًا رئيسيًا وأحد أبنائه في نفس عملية النقل.',
  'abwab.category_unavailable': 'الوجهة غير متاحة أو غير موجودة.',
  'abwab.category_reserved_by_pending': 'الباب محجوز بطلب معلّق.',
  'abwab.manual_protection': 'هذا الباب محمي يدويًا من هذا الإجراء.',
  'abwab.manual_protection_scope_conflict': 'نطاق الحماية اليدوية الحالي مختلف عمّا تم إرساله.',
  'abwab.ordinary_protection': 'الباب ضمن نافذة الحماية الاعتيادية الحالية.',
  'abwab.relationship_duplicate': 'توجد علاقة فعّالة بهذا النوع بين البابين بالفعل.',
  'abwab.relationship_cycle': 'لا يمكن إنشاء هذه العلاقة لأنها تُغلق دورة بين الأعم والأخص.',
  'abwab.stabilization_active': 'العملية محظورة أثناء فترة الاستقرار.',
  'abwab.tree_revision_stale': 'تغيّرت شجرة الأبواب أثناء التعديل. يرجى إعادة المحاولة.',
  'abwab.timeline_generation_stale': 'تغيّرت حالة النظام أثناء التعديل. يرجى إعادة المحاولة.',
  'abwab.row_stale': 'تغيّرت البيانات أثناء التعديل. يرجى إعادة المحاولة.',
};
