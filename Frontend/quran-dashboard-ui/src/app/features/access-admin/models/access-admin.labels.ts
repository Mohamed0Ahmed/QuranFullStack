export const ACCESS_ADMIN_LABELS = {
  loadError: 'تعذر تحميل بيانات إدارة الوصول.',
  accessDeniedError: 'لا تملك صلاحية إدارة الوصول.',
  writeError: 'تعذر إتمام التغيير المطلوب.',
  mutationSuccess: 'تم حفظ التغيير.',
  conflictNotice: 'تغيرت بيانات المستخدم. تم تحديث الحالة الحالية.',
  conflictReloadError: 'تغيرت بيانات المستخدم، وتعذر تحديث الحالة الحالية.',
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
