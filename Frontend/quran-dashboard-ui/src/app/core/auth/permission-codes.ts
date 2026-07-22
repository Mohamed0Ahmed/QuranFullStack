// The frontend leg of the 5-catalogue permission parity check (backend: PermissionCatalogue). These codes
// MUST stay identical to the backend seed / policy / /me / test catalogues — the backend PermissionParityTests
// reads this file and fails on any drift. Frontend permission checks are UX only; the backend policy is the
// authority (frontend hiding is non-authoritative).
export const PERMISSION_CODES = [
  'attribution.view',
  'attribution.manage',
  'permission.administer',
  'audit.restore',
  'safetyPoint.manage',
] as const;

export type PermissionCode = (typeof PERMISSION_CODES)[number];

// The DashboardAdminBaseline: always effective for the Admin baseline, never removable.
export const BASELINE_PERMISSION_CODE: PermissionCode = 'attribution.view';
