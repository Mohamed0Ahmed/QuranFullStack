import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PermissionsApi } from '../data-access/permissions.api';
import { PermissionAdminView, PermissionMutationRequest } from '../models/permission.models';
import { PermissionsFacade } from './permissions.facade';

// Re-grant-after-revoke: a revoked tombstone (isGranted=false, version=2) is returned by List, so the facade
// must compute expectedVersion=2 for the next grant — never a blind 0 that would perpetually 409.
const VIEW: PermissionAdminView = {
  catalogue: [
    { code: 'attribution.manage', systemOwnerOnly: false, dashboardAdminBaseline: false, assignable: true },
  ],
  assignments: [
    { targetKind: 'Role', targetKey: 'Admin', permissionCode: 'attribution.manage', version: 2, isGranted: false },
  ],
};

class StubPermissionsApi {
  list(): Observable<ApiResponse<PermissionAdminView>> {
    return of({ isSuccess: true, message: null, data: VIEW });
  }

  grant(): Observable<ApiResponse<unknown>> {
    return of({ isSuccess: true, message: null, data: null });
  }

  revoke(): Observable<ApiResponse<unknown>> {
    return of({ isSuccess: true, message: null, data: null });
  }
}

function createFacade(): PermissionsFacade {
  TestBed.configureTestingModule({
    providers: [PermissionsFacade, { provide: PermissionsApi, useClass: StubPermissionsApi }],
  });
  return TestBed.inject(PermissionsFacade);
}

describe('PermissionsFacade re-grant after revoke', () => {
  it('sends the tombstone version as expectedVersion on a re-grant', async () => {
    const facade = createFacade();
    await facade.load();

    const request: PermissionMutationRequest = facade.buildRequest('Role', 'Admin', 'attribution.manage');

    expect(request.expectedVersion).toBe(2);
    expect(request.expectedTimelineGeneration).toBe(0);
  });

  it('hides the revoked tombstone from the granted display list', async () => {
    const facade = createFacade();
    await facade.load();

    expect(facade.grantedAssignments()).toHaveLength(0);
    expect(facade.assignments()).toHaveLength(1);
  });

  it('uses expectedVersion 0 for a never-assigned target/code', async () => {
    const facade = createFacade();
    await facade.load();

    expect(facade.buildRequest('Subject', 'someone', 'attribution.manage').expectedVersion).toBe(0);
  });
});
