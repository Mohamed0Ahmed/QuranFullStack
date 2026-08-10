import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AccessAuditEventItem } from '../../../core/api/generated/models/access-audit-event-item';
import { AccessAdminApi } from '../data-access/access-admin.api';
import { AccessAuditStore } from './access-audit.store';

const AUDIT_EVENTS_URL = `${environment.apiBaseUrl}/api/access/audit-events`;

function auditEvent(id: number): AccessAuditEventItem {
  return {
    id,
    occurredAtUtc: '2026-08-07T10:00:00Z',
    actionType: 'PermissionGranted',
    actorType: 'User',
    actorUserId: 9,
    targetUserId: 17,
    actorDisplayName: 'المالك',
    actorEmail: 'owner@example.test',
    targetDisplayName: 'عضو',
    targetEmail: 'member@example.test',
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: null,
    beforeState: {},
    afterState: {},
    reason: null,
    metadata: {},
  };
}

function success(items: AccessAuditEventItem[], nextCursor: string | null) {
  return { isSuccess: true, message: 'تم', data: { items, nextCursor } };
}

describe('AccessAuditStore', () => {
  let store: AccessAuditStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AccessAdminApi, provideHttpClient(), provideHttpClientTesting()],
    });

    store = new AccessAuditStore(TestBed.inject(AccessAdminApi));
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  function expectIdleAndEmpty(): void {
    expect(store.events()).toEqual([]);
    expect(store.nextCursor()).toBeNull();
    expect(store.loading()).toBe(false);
    expect(store.error()).toBeNull();
    expect(store.appending()).toBe(false);
    expect(store.appendError()).toBeNull();
    expect(store.appendedCount()).toBe(0);
  }

  describe('clear() while a request is still in flight', () => {
    it('drops a page that arrives after the slice was cleared', async () => {
      const loading = store.load();
      const stale = httpTesting.expectOne((request) => request.url === AUDIT_EVENTS_URL);
      expect(store.loading()).toBe(true);

      store.clear();
      expectIdleAndEmpty();

      stale.flush(success([auditEvent(1)], 'cursor-2'));
      await loading;

      expectIdleAndEmpty();
    });

    it('leaves no error banner behind when the cleared request fails late', async () => {
      const loading = store.load();
      const stale = httpTesting.expectOne((request) => request.url === AUDIT_EVENTS_URL);

      store.clear();

      stale.flush(
        { isSuccess: false, message: 'تعذر تحميل السجل.', data: null },
        { status: 500, statusText: 'Server Error' },
      );
      await loading;

      expectIdleAndEmpty();
    });

    it('drops a cursor append that arrives after the slice was cleared', async () => {
      const loading = store.load();
      httpTesting
        .expectOne((request) => request.url === AUDIT_EVENTS_URL)
        .flush(success([auditEvent(1)], 'cursor-2'));
      await loading;
      expect(store.events()).toHaveLength(1);

      const appending = store.loadNextPage();
      const staleAppend = httpTesting.expectOne(
        (request) => request.params.get('cursor') === 'cursor-2',
      );
      expect(store.appending()).toBe(true);

      store.clear();
      expectIdleAndEmpty();

      staleAppend.flush(success([auditEvent(2), auditEvent(3)], 'cursor-9'));
      await appending;

      expectIdleAndEmpty();
    });
  });
});
