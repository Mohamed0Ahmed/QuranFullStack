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
  // isGranted=false tombstones are still returned so a re-grant sends the correct expectedVersion (else 409); the UI shows only granted rows.
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
