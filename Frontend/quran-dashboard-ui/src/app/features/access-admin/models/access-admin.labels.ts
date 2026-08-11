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
  permissionDiffSummary: (granted: number, revoked: number): string =>
    `صلاحيات مضافة: ${granted}، صلاحيات ملغاة: ${revoked}`,
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
