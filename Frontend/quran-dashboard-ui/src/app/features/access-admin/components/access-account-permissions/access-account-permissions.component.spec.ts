import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { PermissionCode } from '../../../../core/auth/permission-code';
import { AccessPermissionDiff } from '../../models/access-admin.models';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessAccountPermissionsComponent } from './access-account-permissions.component';

const GROUPS: AccessPermissionGroup[] = [
  {
    key: 'doors',
    label: 'الأبواب',
    codes: ['abwab.doors.create', 'abwab.doors.edit'],
    labels: new Map([
      ['abwab.doors.create', 'إضافة باب'],
      ['abwab.doors.edit', 'تعديل باب'],
    ]),
  },
];

const NO_CHANGES: AccessPermissionDiff = { granted: [], revoked: [] };

const USER: AccessUserDetail = {
  id: 31,
  sub: 'subject-31',
  email: 'member@example.test',
  normalizedEmail: 'member@example.test',
  userName: null,
  displayName: 'عضو',
  title: null,
  status: 'active',
  isOwner: false,
  permissionCodes: [],
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
  version: 2,
};

const BODY_TEST_IDS = [
  'access-permissions-section',
  'access-owner-permissions',
  'access-disabled-permissions',
  'access-unknown-status',
];

interface Harness {
  readonly fixture: ComponentFixture<AccessAccountPermissionsComponent>;
  readonly selections: PermissionCode[][];
  readonly retries: number[];
}

function setup(
  user: Partial<AccessUserDetail> = {},
  inputs: {
    permissionDiff?: AccessPermissionDiff;
    hasUnsavedPermissions?: boolean;
    catalogueLoading?: boolean;
    catalogueError?: string | null;
    canAssignPermissions?: boolean;
    busyAction?: string | null;
    selectedCodes?: ReadonlySet<PermissionCode>;
  } = {},
): Harness {
  TestBed.configureTestingModule({ imports: [AccessAccountPermissionsComponent] });
  const fixture = TestBed.createComponent(AccessAccountPermissionsComponent);
  fixture.componentRef.setInput('user', { ...USER, ...user });
  fixture.componentRef.setInput('groups', GROUPS);
  fixture.componentRef.setInput('selectedCodes', inputs.selectedCodes ?? new Set<PermissionCode>());
  fixture.componentRef.setInput('permissionDiff', inputs.permissionDiff ?? NO_CHANGES);
  fixture.componentRef.setInput('hasUnsavedPermissions', inputs.hasUnsavedPermissions ?? false);
  fixture.componentRef.setInput('catalogueLoading', inputs.catalogueLoading ?? false);
  fixture.componentRef.setInput('catalogueError', inputs.catalogueError ?? null);
  fixture.componentRef.setInput('canAssignPermissions', inputs.canAssignPermissions ?? true);
  fixture.componentRef.setInput('busyAction', inputs.busyAction ?? null);
  const selections: PermissionCode[][] = [];
  const retries: number[] = [];
  fixture.componentInstance.selectionChange.subscribe((codes) => selections.push(codes));
  fixture.componentInstance.catalogueRetryRequested.subscribe(() => retries.push(1));
  fixture.detectChanges();
  return { fixture, selections, retries };
}

function element(harness: Harness, testId: string): HTMLElement {
  const found = harness.fixture.nativeElement.querySelector(
    `[data-testid="${testId}"]`,
  ) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function renderedBodies(harness: Harness): string[] {
  return BODY_TEST_IDS.filter(
    (testId) => harness.fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) !== null,
  );
}

describe('AccessAccountPermissionsComponent', () => {
  it.each([
    ['pending', false, 'access-permissions-section'],
    ['active', false, 'access-permissions-section'],
    ['disabled', false, 'access-disabled-permissions'],
    ['pending', true, 'access-owner-permissions'],
    ['active', true, 'access-owner-permissions'],
    ['disabled', true, 'access-owner-permissions'],
    ['archived', false, 'access-unknown-status'],
    ['archived', true, 'access-unknown-status'],
  ] as const)(
    'shows a %s account (owner: %s) only its own permissions body',
    (status, isOwner, expected) => {
      const harness = setup({ status, isOwner });

      expect(renderedBodies(harness)).toEqual([expected]);
    },
  );

  it.each([
    ['disabled', false],
    ['active', true],
    ['unheard-of', false],
  ] as const)(
    'offers no permission editor to a %s account (owner: %s)',
    (status, isOwner) => {
      const harness = setup({ status, isOwner });

      expect(harness.fixture.nativeElement.querySelector('qd-access-permission-editor')).toBeNull();
    },
  );

  it.each([
    ['active', 'يحصل حساب المالك النشط على كامل صلاحيات الإدارة عبر تجاوز المالك'],
    ['pending', 'لا يسري تجاوز المالك إلا على حساب مالك نشط'],
    ['disabled', 'لا يسري تجاوز المالك إلا على حساب مالك نشط'],
  ] as const)('tells a %s Owner whether the owner bypass is what grants its access', (status, copy) => {
    const harness = setup({ status, isOwner: true });

    expect(element(harness, 'access-owner-permissions').textContent).toContain(copy);
  });

  it('asks for the catalogue again when the read failed and the reader retries', () => {
    const harness = setup({}, { catalogueError: 'تعذّر تحميل الصلاحيات.' });

    expect(element(harness, 'access-catalogue-error').textContent).toContain(
      'تعذّر تحميل الصلاحيات.',
    );
    (element(harness, 'access-catalogue-retry') as HTMLButtonElement).click();

    expect(harness.retries).toHaveLength(1);
  });

  it('passes an editor selection up untouched', () => {
    const harness = setup();

    const checkbox = element(harness, 'access-permission-abwab.doors.edit') as HTMLInputElement;
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));

    expect(harness.selections).toEqual([['abwab.doors.edit']]);
  });

  it.each([
    [{ canAssignPermissions: false }, 'access-permissions-unavailable'],
    [{ busyAction: 'permissions' }, null],
  ])('locks the editor while assignment is not available (%o)', (inputs, noteTestId) => {
    const harness = setup({}, inputs);

    const checkbox = element(harness, 'access-permission-abwab.doors.edit') as HTMLInputElement;
    expect(checkbox.disabled).toBe(true);
    if (noteTestId !== null) {
      expect(element(harness, noteTestId).textContent).toContain('للاطّلاع فقط');
    }
  });

  it('warns a pending account that its selected permissions land only on activation', () => {
    const harness = setup({ status: 'pending' });

    expect(element(harness, 'access-pending-permissions-note').textContent).toContain(
      'تُمنح الصلاحيات المحددة عند تفعيل الحساب',
    );
  });

  it('summarises an unsaved permission draft for both readers and screen readers', () => {
    const harness = setup(
      {},
      {
        hasUnsavedPermissions: true,
        permissionDiff: { granted: ['abwab.doors.edit'], revoked: [] },
      },
    );

    expect(element(harness, 'access-permission-diff-summary-text').textContent).toBe(
      'صلاحيات مضافة: 1، صلاحيات ملغاة: 0',
    );
    const numeric = element(harness, 'access-permission-diff-summary');
    expect(numeric.getAttribute('aria-hidden')).toBe('true');
    expect(numeric.textContent).toContain('+1');
  });

  it('keeps the diff summary out of a clean draft', () => {
    const harness = setup();

    expect(
      harness.fixture.nativeElement.querySelector('[data-testid="access-permission-diff-summary"]'),
    ).toBeNull();
  });
});
