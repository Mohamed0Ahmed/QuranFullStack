export const ACCESS_ADMIN_TAB_KEYS = ['workspace', 'audit', 'security'] as const;

export type AccessAdminTab = (typeof ACCESS_ADMIN_TAB_KEYS)[number];

export const DEFAULT_ACCESS_ADMIN_TAB: AccessAdminTab = 'workspace';

export function parseAccessAdminTab(value: string | null): AccessAdminTab {
  return ACCESS_ADMIN_TAB_KEYS.find((tab) => tab === value) ?? DEFAULT_ACCESS_ADMIN_TAB;
}
