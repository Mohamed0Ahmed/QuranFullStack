import { AccessAdminTab } from './access-admin-tabs';

export const ACCESS_ADMIN_LABELS = {
  unsavedChangesTitle: 'تغييرات غير محفوظة',
  unsavedChangesSwitchUserBody:
    'لم تُحفظ تعديلات الصلاحيات الحالية، واختيار مستخدم آخر يتجاهلها.',
  unsavedChangesLeaveBody:
    'هناك تعديلات غير محفوظة على الصلاحيات، ومغادرة الصفحة تتجاهلها.',
  discardChangesButton: 'تجاهل التغييرات',
  keepEditingButton: 'متابعة التعديل',
  reviewDockLabel: 'تعديلات الصلاحيات غير المحفوظة',
  selectedContextLabel: 'الحساب المحدد',
  noSelectedContextLabel: 'لم يُحدَّد حساب بعد',
  openUserListButton: 'اختيار حساب',
  contextSearchLabel: 'البحث في الحسابات',
  contextSearchButton: 'عرض النتائج',
  noSelectionMessage: 'اختر مستخدمًا لعرض تفاصيل الوصول الخاصة به.',
  userListSheetTitle: 'حسابات الوصول',
  auditAppendedAnnouncement: (count: number): string =>
    count > 0 ? `أُضيف ${count} حدثًا إلى نهاية السجل.` : '',
  permissionDiffSummary: (granted: number, revoked: number): string =>
    `صلاحيات مضافة: ${granted}، صلاحيات ملغاة: ${revoked}`,
  tabsAriaLabel: 'أقسام إدارة الوصول',
  tab: (tab: AccessAdminTab): string => {
    if (tab === 'audit') {
      return 'سجل الوصول';
    }
    if (tab === 'security') {
      return 'الأمان المتقدم';
    }
    return 'مساحة العمل';
  },
  userStatus: (status: string): string => {
    if (status === 'active') {
      return 'نشط';
    }
    if (status === 'pending') {
      return 'معلّق';
    }
    if (status === 'disabled') {
      return 'معطّل';
    }
    return 'حالة غير معروفة';
  },
  systemActor: 'النظام',
  unnamedParticipant: 'حساب غير متاح',
  auditActionType: (actionType: string): string =>
    AUDIT_ACTION_TYPE_LABELS[actionType] ?? actionType,
  reconciliationCandidateState: (state: string): string =>
    RECONCILIATION_CANDIDATE_STATE_LABELS[state] ?? state,
} as const;

const AUDIT_ACTION_TYPE_LABELS: Readonly<Record<string, string>> = {
  UserAccepted: 'قبول حساب',
  UserActivated: 'تفعيل حساب',
  UserDisabled: 'تعطيل حساب',
  UserReactivated: 'إعادة تفعيل حساب',
  PermissionGranted: 'منح صلاحية',
  PermissionRevoked: 'سحب صلاحية',
  LogtoSubjectRelinked: 'إعادة ربط معرّف الدخول',
  OwnerGrantedByReconciliation: 'منح عضوية مالك بالمطابقة',
  OwnerRemovedByReconciliation: 'سحب عضوية مالك بالمطابقة',
  LegacyRoleRemoved: 'إزالة دور قديم',
};

const RECONCILIATION_CANDIDATE_STATE_LABELS: Readonly<Record<string, string>> = {
  Unchanged: 'دون تغيير',
  Added: 'مالك مُضاف',
  Removed: 'مالك مُزال',
  AwaitingVerifiedSignIn: 'بانتظار تسجيل دخول موثّق',
  Unresolved: 'هوية غير مطابقة',
  ConfiguredDisabled: 'مُعدّ كمالك لكنه معطّل',
  OwnerHasDirectGrants: 'مالك يحمل صلاحيات مباشرة',
  RemovalBlockedByLastOwner: 'تعذّرت الإزالة: آخر مالك نشط',
};
