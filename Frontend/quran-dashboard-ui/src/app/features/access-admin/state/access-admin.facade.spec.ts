import { describe, expect, it, vi } from 'vitest';
import { TestBed, getTestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';

import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import { AccessAdminApi } from '../data-access/access-admin.api';
import { AccessAdminFacade } from './access-admin.facade';

function user(version: number, permissionCodes: string[] = []): AccessUserDetail {
  return {
    id: 17,
    sub: 'subject-17',
    email: 'member@example.test',
    normalizedEmail: 'member@example.test',
    userName: null,
    displayName: 'عضو',
    title: null,
    status: 'active',
    isOwner: false,
    permissionCodes,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version,
  };
}

function permissions(version: number, permissionCodes: string[] = []): AccessUserPermissions {
  return { userId: 17, status: 'active', isOwner: false, version, permissionCodes };
}

function success<T>(data: T) {
  return { isSuccess: true, message: 'تم', data };
}

const CATALOGUE: PermissionCatalogueItem[] = [
  {
    code: 'abwab.doors.create',
    arabicLabel: 'إضافة باب',
    englishDescription: 'Create a door.',
    groupKey: 'doors',
    groupLabel: 'الأبواب',
    groupDisplayOrder: 1,
    displayOrder: 1,
  },
  {
    code: 'abwab.doors.edit',
    arabicLabel: 'تعديل باب',
    englishDescription: 'Edit a door.',
    groupKey: 'doors',
    groupLabel: 'الأبواب',
    groupDisplayOrder: 1,
    displayOrder: 2,
  },
];

function setup(isActive: boolean, isOwner: boolean) {
  getTestBed().resetTestingModule();
  const access = { isActive: signal(isActive), isOwner: signal(isOwner) };
  const api = {
    listUsers: vi.fn(() => of(success({ items: [], page: 1, pageSize: 25, totalCount: 0 }))),
    getUser: vi.fn(() => of(success(user(1)))),
    getUserPermissions: vi.fn(() => of(success(permissions(1)))),
    getPermissionCatalogue: vi.fn(() => of(success(CATALOGUE))),
    listAuditEvents: vi.fn(() => of(success({ items: [], nextCursor: null }))),
    getOwnerReconciliationStatus: vi.fn(() => of(success({
      canApply: false,
      candidates: [],
      configurationFingerprint: 'fingerprint',
      isReady: true,
      lastReconciliation: null,
    }))),
    acceptUser: vi.fn(),
    disableUser: vi.fn(),
    reactivateUser: vi.fn(),
    replacePermissions: vi.fn(),
    previewRelink: vi.fn(),
    confirmRelink: vi.fn(),
  };
  const handleAuthFailure = vi.fn().mockResolvedValue(null);

  TestBed.configureTestingModule({
    providers: [
      AccessAdminFacade,
      { provide: AccessAdminApi, useValue: api },
      {
        provide: CurrentUserStore,
        useValue: { isActive: access.isActive, isOwner: access.isOwner },
      },
      { provide: WriteAuthFailureCoordinator, useValue: { handle: handleAuthFailure } },
    ],
  });

  return { facade: TestBed.inject(AccessAdminFacade), api, handleAuthFailure, access };
}

describe('AccessAdminFacade', () => {
  it.each([
    ['a pending account', false, false],
    ['a disabled Owner', false, true],
    ['an active non-owner', true, false],
  ])('does not call security-admin APIs for %s', async (_name, isActive, isOwner) => {
    const { facade, api } = setup(isActive, isOwner);

    await facade.load();

    expect(api.listUsers).not.toHaveBeenCalled();
    expect(api.getPermissionCatalogue).not.toHaveBeenCalled();
    expect(api.listAuditEvents).not.toHaveBeenCalled();
    expect(api.getOwnerReconciliationStatus).not.toHaveBeenCalled();
  });

  it('refreshes the target state after a version conflict without retrying or retaining the attempted grants', async () => {
    const { facade, api } = setup(true, true);
    api.getUser
      .mockReturnValueOnce(of(success(user(1, ['abwab.doors.create']))))
      .mockReturnValueOnce(of(success(user(2, ['abwab.doors.edit']))));
    api.getUserPermissions
      .mockReturnValueOnce(of(success(permissions(1, ['abwab.doors.create']))))
      .mockReturnValueOnce(of(success(permissions(2, ['abwab.doors.edit']))));
    api.replacePermissions.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { isSuccess: false, message: 'تغيرت بيانات المستخدم', data: null },
          }),
      ),
    );

    await facade.loadPermissionCatalogue();
    await facade.selectUser(17);
    facade.setSelectedPermissionCodes(new Set(['abwab.doors.create', 'abwab.sections.create']));

    await expect(facade.replaceSelectedPermissions('تحديث الصلاحيات')).resolves.toBe('conflict');

    expect(api.replacePermissions).toHaveBeenCalledTimes(1);
    expect(api.replacePermissions).toHaveBeenCalledWith(17, {
      expectedVersion: 1,
      permissionCodes: ['abwab.doors.create'],
      reason: 'تحديث الصلاحيات',
    });
    expect(api.getUser).toHaveBeenCalledTimes(2);
    expect(api.getUserPermissions).toHaveBeenCalledTimes(2);
    expect(facade.selectedUser()?.version).toBe(2);
    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.edit']);
  });

  it('keeps fetched individual grants while the permission catalogue is still loading', async () => {
    const { facade, api } = setup(true, true);
    api.getUserPermissions.mockReturnValue(of(success(permissions(1, ['abwab.doors.create']))));

    await facade.selectUser(17);

    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);

    await facade.loadPermissionCatalogue();

    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);
  });

  it('submits only current individual permission codes when accepting a pending user', async () => {
    const { facade, api } = setup(true, true);
    const pendingUser = { ...user(1), status: 'pending' as const };
    const pendingPermissions = { ...permissions(1), status: 'pending' as const };
    api.getUser.mockReturnValue(of(success(pendingUser)));
    api.getUserPermissions.mockReturnValue(of(success(pendingPermissions)));
    api.acceptUser.mockReturnValue(of(success({ ...pendingUser, status: 'active' as const, version: 2 })));

    await facade.loadPermissionCatalogue();
    await facade.selectUser(17);
    facade.setSelectedPermissionCodes(
      new Set(['abwab.doors.create', 'abwab.doors.edit', 'abwab.doors.manage-all']),
    );

    await expect(facade.acceptSelectedUser('قبول الحساب')).resolves.toBe('success');

    expect(api.acceptUser).toHaveBeenCalledWith(17, {
      expectedVersion: 1,
      permissionCodes: ['abwab.doors.create', 'abwab.doors.edit'],
      reason: 'قبول الحساب',
    });
  });

  it('keeps the preview evidence for a separate relink confirmation request', async () => {
    const { facade, api } = setup(true, true);
    api.previewRelink.mockReturnValue(
      of(
        success({
          userId: 17,
          oldSub: 'subject-17',
          newSub: 'replacement-subject',
          version: 4,
          isOwner: false,
        }),
      ),
    );
    api.confirmRelink.mockReturnValue(of(success(user(5))));

    await facade.selectUser(17);

    await expect(
      facade.previewSelectedUserRelink({
        newSub: 'replacement-subject',
        evidenceToken: 'verified-evidence',
      }),
    ).resolves.toBe('success');
    await expect(facade.confirmSelectedUserRelink('تصحيح معرّف الدخول')).resolves.toBe('success');

    expect(api.confirmRelink).toHaveBeenCalledWith(17, {
      expectedVersion: 4,
      oldSub: 'subject-17',
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
      reason: 'تصحيح معرّف الدخول',
      confirmed: true,
    });
  });

  it('clears retained relink evidence when the preview is canceled', async () => {
    const { facade, api } = setup(true, true);
    api.previewRelink.mockReturnValue(
      of(
        success({
          userId: 17,
          oldSub: 'subject-17',
          newSub: 'replacement-subject',
          version: 4,
          isOwner: false,
        }),
      ),
    );

    await facade.selectUser(17);
    await facade.previewSelectedUserRelink({
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
    });

    facade.cancelSelectedUserRelink();

    await expect(facade.confirmSelectedUserRelink('إلغاء المعاينة')).resolves.toBe('invalid');
    expect(facade.relinkPreview()).toBeNull();
    expect(api.confirmRelink).not.toHaveBeenCalled();
  });

  it('does not preview a relink after the Owner access state becomes stale', async () => {
    const { facade, api, access } = setup(true, true);

    await facade.selectUser(17);
    access.isOwner.set(false);

    await expect(
      facade.previewSelectedUserRelink({ newSub: 'replacement-subject', evidenceToken: 'evidence' }),
    ).resolves.toBe('invalid');

    expect(api.previewRelink).not.toHaveBeenCalled();
  });

  it('does not submit a permission replacement without a confirmation reason', async () => {
    const { facade, api } = setup(true, true);

    await facade.loadPermissionCatalogue();
    await facade.selectUser(17);

    await expect(facade.replaceSelectedPermissions('   ')).resolves.toBe('invalid');

    expect(api.replacePermissions).not.toHaveBeenCalled();
  });
});
