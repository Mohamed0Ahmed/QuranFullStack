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
} as const;
