import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { AccessUserSearchState, EMPTY_ACCESS_USER_SEARCH } from '../../models/access-admin.models';
import { AccessUserPickerComponent } from './access-user-picker.component';

const NAMED: AccessUserSummary = {
  id: 17,
  email: 'member@example.test',
  displayName: 'عضو',
  status: 'active',
  isOwner: false,
  permissionCount: 1,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
  version: 4,
};

const UNNAMED: AccessUserSummary = { ...NAMED, id: 18, displayName: '  ', email: 'blank@example.test' };

function setup(state: AccessUserSearchState = EMPTY_ACCESS_USER_SEARCH): {
  fixture: ComponentFixture<AccessUserPickerComponent>;
  searches: string[];
  selections: (AccessUserSummary | null)[];
} {
  TestBed.configureTestingModule({ imports: [AccessUserPickerComponent] });
  const fixture = TestBed.createComponent(AccessUserPickerComponent);
  fixture.componentRef.setInput('label', 'الحساب المعني');
  fixture.componentRef.setInput('controlId', 'access-picker');
  fixture.componentRef.setInput('testIdPrefix', 'access-picker');
  fixture.componentRef.setInput('state', state);
  const searches: string[] = [];
  const selections: (AccessUserSummary | null)[] = [];
  fixture.componentInstance.searchRequested.subscribe((value) => searches.push(value));
  fixture.componentInstance.selectionChange.subscribe((value) => selections.push(value));
  fixture.detectChanges();
  return { fixture, searches, selections };
}

function element(fixture: ComponentFixture<AccessUserPickerComponent>, testId: string): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function type(fixture: ComponentFixture<AccessUserPickerComponent>, value: string): void {
  const input = element(fixture, 'access-picker-search') as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

describe('AccessUserPickerComponent', () => {
  it('asks for a trimmed search term and refuses an empty one', () => {
    const { fixture, searches } = setup();

    expect((element(fixture, 'access-picker-find') as HTMLButtonElement).disabled).toBe(true);

    type(fixture, '   ');
    expect((element(fixture, 'access-picker-find') as HTMLButtonElement).disabled).toBe(true);

    type(fixture, '  بريد  ');
    element(fixture, 'access-picker-find').click();

    expect(searches).toEqual(['بريد']);
  });

  it('searches on Enter without submitting the surrounding filter form', () => {
    const { fixture, searches } = setup();
    type(fixture, 'عضو');
    const enter = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });

    element(fixture, 'access-picker-search').dispatchEvent(enter);

    expect(searches).toEqual(['عضو']);
    expect(enter.defaultPrevented).toBe(true);
  });

  it('names each candidate by display name, falling back to the email when it is blank', () => {
    const { fixture } = setup({ users: [NAMED, UNNAMED], error: null, loading: false });
    const candidates = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll(
        '[data-testid^="access-picker-candidate-"]',
      ),
    );

    expect(candidates).toHaveLength(2);
    expect(candidates[0].textContent).toContain('عضو');
    expect(candidates[1].textContent).toContain('blank@example.test');
  });

  it('emits the chosen account and then the cleared selection', () => {
    const { fixture, selections } = setup({ users: [UNNAMED, NAMED], error: null, loading: false });

    element(fixture, `access-picker-candidate-${NAMED.id}`).click();
    expect(selections).toEqual([NAMED]);

    fixture.componentRef.setInput('selected', NAMED);
    fixture.detectChanges();
    expect(element(fixture, 'access-picker-selected').textContent).toContain('عضو');
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="access-picker-search"]'),
    ).toBeNull();

    element(fixture, 'access-picker-clear').click();
    expect(selections).toEqual([NAMED, null]);
  });

  it('reports a failed lookup instead of reading as no matches', () => {
    const { fixture } = setup({ users: [], error: 'تعذر تحميل بيانات إدارة الوصول.', loading: false });

    expect(element(fixture, 'qd-state-error').textContent).toContain(
      'تعذر تحميل بيانات إدارة الوصول.',
    );
  });

  it('stays quiet until a search has actually run', () => {
    const { fixture } = setup();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-empty"]'),
    ).toBeNull();

    type(fixture, 'لا أحد');
    element(fixture, 'access-picker-find').click();
    fixture.detectChanges();

    expect(element(fixture, 'qd-state-empty').textContent).toContain('لا توجد حسابات مطابقة.');
  });
});
