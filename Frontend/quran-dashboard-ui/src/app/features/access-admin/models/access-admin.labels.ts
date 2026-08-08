import { AccessAdminTab } from './access-admin-tabs';

export const ACCESS_ADMIN_LABELS = {
  unsavedChangesTitle: 'تغييرات غير محفوظة',
  unsavedChangesSwitchUserBody:
    'لم تُحفظ تعديلات الصلاحيات الحالية، واختيار مستخدم آخر يتجاهلها.',
  unsavedChangesLeavePrompt:
    'هناك تعديلات غير محفوظة على الصلاحيات، ومغادرة الصفحة تتجاهلها. هل تريد المغادرة؟',
  discardChangesButton: 'تجاهل التغييرات',
  keepEditingButton: 'متابعة التعديل',
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
} as const;
