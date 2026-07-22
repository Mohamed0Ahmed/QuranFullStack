// Wire + view models for the Owner-only permission-administration surface (US5). Codes come from the shared
// parity source `core/auth/permission-codes.ts`; these shapes mirror the backend `PermissionAdmin*` DTOs.

export type PermissionTargetKind = 'Role' | 'Subject';

export interface PermissionCatalogueEntry {
  readonly code: string;
  readonly systemOwnerOnly: boolean;
  readonly dashboardAdminBaseline: boolean;
  readonly assignable: boolean;
}

export interface PermissionAssignmentView {
  readonly targetKind: string;
  readonly targetKey: string;
  readonly permissionCode: string;
  readonly version: number;
  // Revoked tombstones (isGranted=false) are returned so the client sends the correct expected version on a
  // re-grant after revoke; the UI shows only granted rows.
  readonly isGranted: boolean;
}

export interface PermissionAdminView {
  readonly catalogue: PermissionCatalogueEntry[];
  readonly assignments: PermissionAssignmentView[];
}

export interface PermissionMutationRequest {
  readonly targetKind: PermissionTargetKind;
  readonly targetKey: string;
  readonly permissionCode: string;
  readonly expectedTimelineGeneration: number;
  readonly expectedVersion: number;
}
