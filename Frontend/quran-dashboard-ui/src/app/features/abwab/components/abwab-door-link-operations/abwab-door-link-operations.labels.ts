export const ABWAB_DOOR_LINK_OPERATIONS_LABELS = {
  deleteTitle: 'تأكيد حذف الروابط',
  deleteConfirm: 'حذف الروابط',
  deleteAllMessage: (count: number): string =>
    `سيتم حذف جميع سجلات الربط في الباب وعددها ${count}. لا يمكن التراجع عن هذا الإجراء.`,
  deletePartialMessage: (count: number): string =>
    `سيتم حذف ${count} من سجلات الربط المحددة من الباب. لا يمكن التراجع عن هذا الإجراء.`,
} as const;
