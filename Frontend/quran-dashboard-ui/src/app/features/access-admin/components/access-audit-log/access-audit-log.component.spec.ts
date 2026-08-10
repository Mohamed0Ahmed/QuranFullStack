import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessUserSearchState, EMPTY_ACCESS_USER_SEARCH } from '../../models/access-admin.models';
import { AccessAuditFilters, AccessAuditLogComponent } from './access-audit-log.component';

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

const EVENTS: readonly AccessAuditEventItem[] = [
  {
    id: 1,
    occurredAtUtc: '2026-08-07T10:00:00Z',
    actionType: 'PermissionGranted',
    actorType: 'User',
    actorUserId: 9,
    targetUserId: 17,
    actorDisplayName: 'المالك',
    actorEmail: 'owner@example.test',
    targetDisplayName: '   ',
    targetEmail: 'member@example.test',
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: 'abwab.doors.edit',
    beforeState: {},
    afterState: {},
    reason: 'تكليف مراجعة',
    metadata: {},
  },
  {
    id: 2,
    occurredAtUtc: '2026-08-07T09:00:00Z',
    actionType: 'OwnerGrantedByReconciliation',
    actorType: 'System',
    actorUserId: null,
    targetUserId: 17,
    actorDisplayName: null,
    actorEmail: null,
    targetDisplayName: 'عضو',
    targetEmail: 'member@example.test',
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: null,
    beforeState: {},
    afterState: {},
    reason: null,
    metadata: {},
  },
];

const CANDIDATES: readonly AccessUserSummary[] = [
  {
    id: 17,
    email: 'member@example.test',
    displayName: 'عضو',
    status: 'active',
    isOwner: false,
    permissionCount: 1,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version: 4,
  },
  {
    id: 9,
    email: 'owner@example.test',
    displayName: 'المالك',
    status: 'active',
    isOwner: true,
    permissionCount: 0,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version: 2,
  },
];

function found(users: readonly AccessUserSummary[]): AccessUserSearchState {
  return { users, error: null, loading: false };
}

function setup(
  options: {
    events?: readonly AccessAuditEventItem[];
    loading?: boolean;
    error?: string | null;
    hasNextPage?: boolean;
    targetSearch?: AccessUserSearchState;
    actorSearch?: AccessUserSearchState;
    appending?: boolean;
    appendError?: string | null;
    appendedCount?: number;
  } = {},
): {
  fixture: ComponentFixture<AccessAuditLogComponent>;
  filters: AccessAuditFilters[];
  targetSearches: string[];
  actorSearches: string[];
  nextPages: number;
} {
  TestBed.configureTestingModule({ imports: [AccessAuditLogComponent] });
  const fixture = TestBed.createComponent(AccessAuditLogComponent);
  fixture.componentRef.setInput('events', options.events ?? EVENTS);
  fixture.componentRef.setInput('permissionGroups', GROUPS);
  fixture.componentRef.setInput('loading', options.loading ?? false);
  fixture.componentRef.setInput('error', options.error ?? null);
  fixture.componentRef.setInput('hasNextPage', options.hasNextPage ?? false);
  fixture.componentRef.setInput('appending', options.appending ?? false);
  fixture.componentRef.setInput('appendError', options.appendError ?? null);
  fixture.componentRef.setInput('appendedCount', options.appendedCount ?? 0);
  fixture.componentRef.setInput('targetSearch', options.targetSearch ?? EMPTY_ACCESS_USER_SEARCH);
  fixture.componentRef.setInput('actorSearch', options.actorSearch ?? EMPTY_ACCESS_USER_SEARCH);
  const filters: AccessAuditFilters[] = [];
  const targetSearches: string[] = [];
  const actorSearches: string[] = [];
  const state = { fixture, filters, targetSearches, actorSearches, nextPages: 0 };
  fixture.componentInstance.filtersApplied.subscribe((value) => filters.push(value));
  fixture.componentInstance.targetSearchRequested.subscribe((value) => targetSearches.push(value));
  fixture.componentInstance.actorSearchRequested.subscribe((value) => actorSearches.push(value));
  fixture.componentInstance.nextPageRequested.subscribe(() => (state.nextPages += 1));
  fixture.detectChanges();
  return state;
}

function element(fixture: ComponentFixture<AccessAuditLogComponent>, testId: string): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function typeInto(
  fixture: ComponentFixture<AccessAuditLogComponent>,
  testId: string,
  value: string,
): void {
  const input = element(fixture, testId) as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

function rowTextWithoutTimestamps(fixture: ComponentFixture<AccessAuditLogComponent>): string {
  const rows = Array.from(
    (fixture.nativeElement as HTMLElement).querySelectorAll('.access-audit-log__row'),
  );
  return rows
    .map((row) => {
      const withoutTime = row.cloneNode(true) as HTMLElement;
      withoutTime.querySelectorAll('time').forEach((stamp) => stamp.remove());
      return withoutTime.textContent ?? '';
    })
    .join(' ');
}

function submitFilters(fixture: ComponentFixture<AccessAuditLogComponent>): void {
  element(fixture, 'access-audit-filters').dispatchEvent(
    new Event('submit', { bubbles: true, cancelable: true }),
  );
}

describe('AccessAuditLogComponent', () => {
  it('names both participants by human identity and never prints their numeric ids', () => {
    const { fixture } = setup();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('المنفّذ: المالك');
    expect(text).toContain('المنفّذ: النظام');
    expect(text).toContain('الحساب: عضو');
    expect(text).toContain('الحساب: member@example.test');
    expect(text).not.toContain('معرّف المستخدم');
    expect(rowTextWithoutTimestamps(fixture)).not.toMatch(/\d/);
  });

  it('says an account is unavailable rather than leaving the attribution blank', () => {
    const { fixture } = setup({
      events: [
        {
          ...EVENTS[0],
          actorDisplayName: null,
          actorEmail: null,
          targetDisplayName: null,
          targetEmail: null,
        },
      ],
    });
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('الحساب: حساب غير متاح');
    expect(text).toContain('المنفّذ: حساب غير متاح');
  });

  it('reads every action type in Arabic and leaves an unmodelled one legible', () => {
    const { fixture } = setup({
      events: [{ ...EVENTS[0], actionType: 'SomethingNewer' }, EVENTS[1]],
    });
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('منح عضوية مالك بالمطابقة');
    expect(text).toContain('SomethingNewer');
  });

  it('renders the moment in local time rather than the raw UTC string', () => {
    const { fixture } = setup();
    const stamp = fixture.nativeElement.querySelector('time') as HTMLElement;

    expect(stamp.textContent?.trim()).toMatch(/^\d{4}\/\d{2}\/\d{2} \d{2}:\d{2}$/);
    expect(stamp.getAttribute('datetime')).toBe('2026-08-07T10:00:00Z');
    expect(fixture.nativeElement.textContent).not.toContain('2026-08-07T10:00:00Z');
  });

  it('offers the modelled event types as a closed list instead of free text', () => {
    const { fixture } = setup();
    const select = element(fixture, 'access-audit-action') as HTMLSelectElement;

    expect(select.tagName).toBe('SELECT');
    expect(Array.from(select.options, (option) => option.value)).toEqual([
      '',
      'UserAccepted',
      'UserActivated',
      'UserDisabled',
      'UserReactivated',
      'PermissionGranted',
      'PermissionRevoked',
      'LogtoSubjectRelinked',
      'OwnerGrantedByReconciliation',
      'OwnerRemovedByReconciliation',
      'LegacyRoleRemoved',
    ]);
    expect(select.options[5].textContent).toBe('منح صلاحية');
  });

  it('offers no numeric identifier input at all', () => {
    const { fixture } = setup();

    expect((fixture.nativeElement as HTMLElement).querySelector('input[type="number"]')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('[inputmode="numeric"]')).toBeNull();
  });

  it('asks for candidates by name and filters by the account the operator picked', () => {
    const state = setup();

    typeInto(state.fixture, 'access-audit-target-search', ' عضو ');
    element(state.fixture, 'access-audit-target-find').click();

    expect(state.targetSearches).toEqual(['عضو']);

    state.fixture.componentRef.setInput('targetSearch', found([CANDIDATES[1], CANDIDATES[0]]));
    state.fixture.detectChanges();

    expect(
      (state.fixture.nativeElement as HTMLElement).querySelectorAll(
        '[data-testid^="access-audit-target-candidate-"]',
      ),
    ).toHaveLength(2);

    element(state.fixture, 'access-audit-target-candidate-17').click();
    state.fixture.detectChanges();

    expect(element(state.fixture, 'access-audit-target-selected').textContent).toContain('عضو');
    submitFilters(state.fixture);

    expect(state.filters).toEqual([
      { targetUserId: 17, actorUserId: undefined, actionType: undefined, permissionCode: undefined },
    ]);
  });

  it('keeps the two pickers independent and lets a chosen account be cleared', () => {
    const state = setup();
    typeInto(state.fixture, 'access-audit-actor-search', 'المالك');
    element(state.fixture, 'access-audit-actor-find').click();
    state.fixture.componentRef.setInput('actorSearch', found([CANDIDATES[1]]));
    state.fixture.detectChanges();

    expect(
      (state.fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid^="access-audit-target-candidate-"]',
      ),
    ).toBeNull();

    element(state.fixture, 'access-audit-actor-candidate-9').click();
    state.fixture.detectChanges();
    submitFilters(state.fixture);

    expect(state.filters[0]).toMatchObject({ actorUserId: 9, targetUserId: undefined });

    element(state.fixture, 'access-audit-actor-clear').click();
    state.fixture.detectChanges();
    submitFilters(state.fixture);

    expect(state.filters[1]).toMatchObject({ actorUserId: undefined });
  });

  it('offers only catalogued permission codes as filter values', () => {
    const { fixture, filters } = setup();
    const select = element(fixture, 'access-audit-permission') as HTMLSelectElement;

    expect(Array.from(select.options, (option) => option.value)).toEqual([
      '',
      'abwab.doors.create',
      'abwab.doors.edit',
    ]);

    select.value = 'abwab.doors.edit';
    select.dispatchEvent(new Event('change'));
    submitFilters(fixture);

    expect(filters[0]?.permissionCode).toBe('abwab.doors.edit');
  });

  it('offers no next page when the server named no cursor', () => {
    const { fixture } = setup();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="access-audit-next-page"]',
      ),
    ).toBeNull();
  });

  it('asks for the next page once the server offered a cursor', () => {
    const state = setup({ hasNextPage: true });

    element(state.fixture, 'access-audit-next-page').click();

    expect(state.nextPages).toBe(1);
  });

  it.each([
    ['loading', { loading: true }, 'qd-skeleton-rows'],
    ['error', { error: 'تعذر تحميل السجل.' }, 'access-audit-error'],
    ['empty', { events: [] }, 'access-audit-empty'],
  ] as const)('renders the shared %s state instead of a list', (_name, options, testId) => {
    const { fixture } = setup(options);

    expect(element(fixture, testId)).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).querySelector('ul.access-audit-log__list')).toBeNull();
  });

  it('keeps the loaded events mounted while an append is in flight', () => {
    const { fixture } = setup({ hasNextPage: true, appending: true });
    const list = (fixture.nativeElement as HTMLElement).querySelector('ul.access-audit-log__list');

    expect(list).toBeTruthy();
    expect(list?.querySelectorAll('[role="listitem"]')).toHaveLength(EVENTS.length);
    expect(list?.getAttribute('aria-busy')).toBe('true');
    expect((element(fixture, 'access-audit-next-page') as HTMLButtonElement).disabled).toBe(true);
    expect(element(fixture, 'access-audit-next-page').getAttribute('aria-busy')).toBe('true');
  });

  it('reports an append failure beside the events it did not replace', () => {
    const { fixture } = setup({ hasNextPage: true, appendError: 'تعذر تحميل المزيد.' });

    expect(element(fixture, 'access-audit-append-error').textContent).toContain(
      'تعذر تحميل المزيد.',
    );
    expect(
      (fixture.nativeElement as HTMLElement).querySelectorAll('ul.access-audit-log__list [role="listitem"]'),
    ).toHaveLength(EVENTS.length);
  });

  it('announces the appended count through a permanently mounted polite region', () => {
    const { fixture } = setup({ hasNextPage: true });
    const announcement = element(fixture, 'access-audit-append-announcement');

    expect(announcement.getAttribute('role')).toBe('status');
    expect(announcement.getAttribute('aria-live')).toBe('polite');
    expect(announcement.textContent?.trim()).toBe('');

    fixture.componentRef.setInput('appendedCount', 25);
    fixture.detectChanges();

    expect(element(fixture, 'access-audit-append-announcement').textContent).toContain('25');
  });

  it('never offers numeric pagination beside the cursor append action', () => {
    const { fixture } = setup({ hasNextPage: true });

    expect((fixture.nativeElement as HTMLElement).querySelector('qd-pagination')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('nav[aria-label]')).toBeNull();
  });
});
