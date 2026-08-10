import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NEVER, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { AccessUserDetail } from '../../core/api/generated/models/access-user-detail';
import { CurrentUserResponse } from '../../core/api/generated/models/current-user-response';
import { PermissionCatalogueItem } from '../../core/api/generated/models/permission-catalogue-item';
import { CurrentUserStore } from '../../core/auth/current-user.store';
import { accessAdminUnsavedChangesGuard } from './access-admin-unsaved-changes.guard';
import { AccessAdminApi } from './data-access/access-admin.api';
import { AccessAdminPageComponent } from './pages/access-admin-page/access-admin-page.component';

let activePage: DraftPageStub;

@Component({ standalone: true, template: '<p>access</p>' })
class DraftPageStub {
  dirty = true;
  decisions = 0;
  private pending: Promise<boolean> | null = null;
  private resolver: ((allowed: boolean) => void) | null = null;

  constructor() {
    activePage = this;
  }

  hasUnsavedChanges(): boolean {
    return this.dirty;
  }

  confirmRouteLeave(): Promise<boolean> {
    if (!this.dirty) {
      return Promise.resolve(true);
    }
    if (this.pending !== null) {
      return this.pending;
    }
    this.decisions += 1;
    this.pending = new Promise<boolean>((resolve) => {
      this.resolver = resolve;
    });
    return this.pending;
  }

  settle(allowed: boolean): void {
    const resolve = this.resolver;
    this.resolver = null;
    this.pending = null;
    resolve?.(allowed);
  }
}

@Component({ standalone: true, template: '<p>elsewhere</p>' })
class ElsewhereStub {}

const guard = accessAdminUnsavedChangesGuard as unknown as (
  component: DraftPageStub,
) => boolean | Promise<boolean>;

describe('accessAdminUnsavedChangesGuard', () => {
  let router: Router;
  let location: Location;
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideLocationMocks(),
        provideRouter([
          { path: 'settings/access', component: DraftPageStub, canDeactivate: [guard] },
          { path: 'elsewhere', component: ElsewhereStub },
        ]),
      ],
    });
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    router.setUpLocationChangeListener();
    harness = await RouterTestingHarness.create('/settings/access');
    harness.detectChanges();
  });

  it('reads only the two members the page publishes for route-leave protection', () => {
    const contract: Pick<AccessAdminPageComponent, 'hasUnsavedChanges' | 'confirmRouteLeave'> =
      activePage;

    expect(typeof contract.hasUnsavedChanges).toBe('function');
    expect(typeof contract.confirmRouteLeave).toBe('function');
  });

  it('leaves without asking when the permission draft matches the stored grants', async () => {
    activePage.dirty = false;

    expect(await router.navigateByUrl('/elsewhere')).toBe(true);
    expect(router.url).toBe('/elsewhere');
    expect(activePage.decisions).toBe(0);
  });

  it('holds the route while the decision is open and stays on the page when it is declined', async () => {
    const page = activePage;

    const navigation = router.navigateByUrl('/elsewhere');
    await Promise.resolve();

    expect(page.decisions).toBe(1);
    expect(router.url).toBe('/settings/access');

    page.settle(false);

    expect(await navigation).toBe(false);
    expect(router.url).toBe('/settings/access');
  });

  it('leaves once the decision confirms', async () => {
    const page = activePage;

    const navigation = router.navigateByUrl('/elsewhere');
    await Promise.resolve();
    page.settle(true);

    expect(await navigation).toBe(true);
    expect(router.url).toBe('/elsewhere');
  });

  it('shares one pending decision across repeated guard calls', async () => {
    const page = activePage;

    const navigation = router.navigateByUrl('/elsewhere');
    await Promise.resolve();
    void guard(page);
    void guard(page);

    expect(page.decisions).toBe(1);

    page.settle(false);
    await navigation;

    expect(router.url).toBe('/settings/access');
  });

  it('protects a browser back move exactly as it protects a forward navigation', async () => {
    activePage.dirty = false;
    await router.navigateByUrl('/elsewhere');
    harness.detectChanges();
    await router.navigateByUrl('/settings/access');
    harness.detectChanges();
    const page = activePage;
    page.dirty = true;

    location.back();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(page.decisions).toBe(1);
    expect(router.url).toBe('/settings/access');

    page.settle(false);
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(router.url).toBe('/settings/access');
    expect(location.path()).toBe('/settings/access');
  });
});

describe('accessAdminUnsavedChangesGuard on the real Access page', () => {
  const ACCESS_BASE_URL = `${environment.apiBaseUrl}/api/access`;

  const OWNER: CurrentUserResponse = {
    sub: 'owner-subject',
    email: 'owner@example.test',
    displayName: 'المالك',
    status: 'active',
    isOwner: true,
    permissions: [],
  };

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

  const MEMBER: AccessUserDetail = {
    id: 17,
    sub: 'subject-17',
    email: 'member@example.test',
    normalizedEmail: 'member@example.test',
    userName: null,
    displayName: 'عضو',
    title: null,
    status: 'active',
    isOwner: false,
    permissionCodes: ['abwab.doors.create'],
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version: 4,
  };

  function success<T>(data: T) {
    return { isSuccess: true, message: 'تم', data };
  }

  let router: Router;
  let location: Location;
  let httpTesting: HttpTestingController;
  let harness: RouterTestingHarness;

  function node(testId: string): HTMLElement {
    const found = harness.routeNativeElement?.querySelector(
      `[data-testid="${testId}"]`,
    ) as HTMLElement | null;
    if (!found) {
      throw new Error(`Missing ${testId}`);
    }
    return found;
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        AccessAdminApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideLocationMocks(),
        provideRouter([
          {
            path: 'settings/access',
            component: AccessAdminPageComponent,
            canDeactivate: [accessAdminUnsavedChangesGuard],
          },
          { path: 'elsewhere', component: ElsewhereStub },
        ]),
        {
          provide: OidcSecurityService,
          useValue: {
            isAuthenticated$: NEVER,
            getIdToken: () => of('signed.id.token'),
            authorize: vi.fn(),
          },
        },
      ],
    });

    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    httpTesting = TestBed.inject(HttpTestingController);

    const owner = TestBed.inject(CurrentUserStore).refresh();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/me`).flush(success(OWNER));
    await owner;

    harness = await RouterTestingHarness.create('/settings/access');
    harness.detectChanges();

    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/users`)
      .flush(
        success({
          items: [
            {
              id: MEMBER.id,
              email: MEMBER.email,
              displayName: MEMBER.displayName,
              status: MEMBER.status,
              isOwner: MEMBER.isOwner,
              permissionCount: MEMBER.permissionCodes.length,
              createdAtUtc: MEMBER.createdAtUtc,
              updatedAtUtc: MEMBER.updatedAtUtc,
              version: MEMBER.version,
            },
          ],
          page: 1,
          pageSize: 25,
          totalCount: 1,
        }),
      );
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady: true }));
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/audit-events`)
      .flush(success({ items: [], nextCursor: null }));
    httpTesting.expectOne(`${ACCESS_BASE_URL}/owner-reconciliation/status`).flush(
      success({
        canApply: false,
        candidates: [],
        configurationFingerprint: 'a1b2c3d4e5f6',
        isReady: true,
        lastReconciliation: null,
      }),
    );
    await harness.fixture.whenStable();
    harness.detectChanges();
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function stageADirtyDraft(): Promise<void> {
    node(`access-user-${MEMBER.id}`).click();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${MEMBER.id}`).flush(success(MEMBER));
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${MEMBER.id}/permissions`).flush(
      success({
        userId: MEMBER.id,
        status: MEMBER.status,
        isOwner: MEMBER.isOwner,
        version: MEMBER.version,
        permissionCodes: MEMBER.permissionCodes,
      }),
    );
    await new Promise((resolve) => setTimeout(resolve, 0));
    await harness.fixture.whenStable();
    harness.detectChanges();

    const box = node('access-permission-abwab.doors.edit') as HTMLInputElement;
    box.checked = true;
    box.dispatchEvent(new Event('change'));
    harness.detectChanges();
  }

  it('raises the page-owned confirmation through real navigation and keeps the address bar on Access when declined', async () => {
    await stageADirtyDraft();

    const navigation = router.navigateByUrl('/elsewhere');
    await Promise.resolve();
    harness.detectChanges();

    expect(node('access-leave-page-confirm')).toBeTruthy();

    node('access-leave-page-confirm-cancel').click();
    harness.detectChanges();

    expect(await navigation).toBe(false);
    expect(router.url).toBe('/settings/access');
    expect(location.path()).toBe('/settings/access');
    expect((node('access-permission-abwab.doors.edit') as HTMLInputElement).checked).toBe(true);
    expect(node('access-user-summary-email').textContent).toContain('member@example.test');
    httpTesting.expectNone(() => true);
  });

  it('leaves without discarding the draft eagerly once the confirmation is accepted', async () => {
    await stageADirtyDraft();

    const navigation = router.navigateByUrl('/elsewhere');
    await Promise.resolve();
    harness.detectChanges();
    node('access-leave-page-confirm-confirm').click();
    harness.detectChanges();

    expect(await navigation).toBe(true);
    expect(router.url).toBe('/elsewhere');
    expect(location.path()).toBe('/elsewhere');
    httpTesting.expectNone(() => true);
  });

  it('lets a clean draft leave without mounting the confirmation at all', async () => {
    expect(await router.navigateByUrl('/elsewhere')).toBe(true);
    expect(router.url).toBe('/elsewhere');
    expect(location.path()).toBe('/elsewhere');
  });
});
