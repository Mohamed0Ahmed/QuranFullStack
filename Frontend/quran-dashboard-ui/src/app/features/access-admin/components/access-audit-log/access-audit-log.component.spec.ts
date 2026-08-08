import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
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
    actionType: 'OwnerReconciled',
    actorType: 'System',
    actorUserId: null,
    targetUserId: 17,
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: null,
    beforeState: {},
    afterState: {},
    reason: null,
    metadata: {},
  },
];

function setup(
  options: {
    events?: readonly AccessAuditEventItem[];
    loading?: boolean;
    error?: string | null;
    hasNextPage?: boolean;
  } = {},
): {
  fixture: ComponentFixture<AccessAuditLogComponent>;
  filters: AccessAuditFilters[];
  nextPages: number;
} {
  TestBed.configureTestingModule({ imports: [AccessAuditLogComponent] });
  const fixture = TestBed.createComponent(AccessAuditLogComponent);
  fixture.componentRef.setInput('events', options.events ?? EVENTS);
  fixture.componentRef.setInput('permissionGroups', GROUPS);
  fixture.componentRef.setInput('loading', options.loading ?? false);
  fixture.componentRef.setInput('error', options.error ?? null);
  fixture.componentRef.setInput('hasNextPage', options.hasNextPage ?? false);
  const filters: AccessAuditFilters[] = [];
  const state = { fixture, filters, nextPages: 0 };
  fixture.componentInstance.filtersApplied.subscribe((value) => filters.push(value));
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
  selector: string,
  value: string,
): void {
  const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

describe('AccessAuditLogComponent', () => {
  it('names the actor type in Arabic and reports a missing actor rather than blanking it', () => {
    const { fixture } = setup();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('المنفّذ: مستخدم');
    expect(text).toContain('المنفّذ: النظام');
    expect(text).toMatch(/معرّف المستخدم المنفّذ:\s*غير متاح/);
    expect(text).toContain('تكليف مراجعة');
  });

  it('emits only the filters the operator actually filled in', () => {
    const { fixture, filters } = setup();

    typeInto(fixture, '#access-audit-actor', '9');
    element(fixture, 'access-audit-filters').dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

    expect(filters).toEqual([
      { targetUserId: undefined, actorUserId: 9, actionType: undefined, permissionCode: undefined },
    ]);
  });

  it('drops a non-positive identifier instead of sending it', () => {
    const { fixture, filters } = setup();

    typeInto(fixture, '#access-audit-actor', '0');
    typeInto(fixture, '#access-audit-target', '-3');
    typeInto(fixture, '#access-audit-action', '   ');
    element(fixture, 'access-audit-filters').dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

    expect(filters).toEqual([
      {
        targetUserId: undefined,
        actorUserId: undefined,
        actionType: undefined,
        permissionCode: undefined,
      },
    ]);
  });

  it('offers only catalogued permission codes as filter values', () => {
    const { fixture, filters } = setup();
    const select = fixture.nativeElement.querySelector(
      '#access-audit-permission',
    ) as HTMLSelectElement;

    expect(Array.from(select.options, (option) => option.value)).toEqual([
      '',
      'abwab.doors.create',
      'abwab.doors.edit',
    ]);

    select.value = 'abwab.doors.edit';
    select.dispatchEvent(new Event('change'));
    element(fixture, 'access-audit-filters').dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

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
    ['loading', { loading: true }, 'qd-state-loading'],
    ['error', { error: 'تعذر تحميل السجل.' }, 'qd-state-error'],
    ['empty', { events: [] }, 'qd-state-empty'],
  ] as const)('renders the shared %s state instead of a list', (_name, options, testId) => {
    const { fixture } = setup(options);

    expect(element(fixture, testId)).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).querySelector('ol')).toBeNull();
  });
});
