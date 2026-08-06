import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { AccessUserListQuery } from '../../models/access-admin.models';
import { AccessUserListComponent } from './access-user-list.component';

const USER: AccessUserSummary = {
  id: 17,
  email: 'member@example.test',
  displayName: 'عضو',
  status: 'pending',
  isOwner: false,
  permissionCount: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
  version: 4,
};

const QUERY: AccessUserListQuery = { page: 1, pageSize: 25 };

function setup(): ComponentFixture<AccessUserListComponent> {
  TestBed.configureTestingModule({ imports: [AccessUserListComponent] });
  const fixture = TestBed.createComponent(AccessUserListComponent);
  fixture.componentRef.setInput('users', [USER]);
  fixture.componentRef.setInput('selectedUserId', null);
  fixture.componentRef.setInput('query', QUERY);
  fixture.componentRef.setInput('page', 1);
  fixture.componentRef.setInput('pageSize', 25);
  fixture.componentRef.setInput('totalCount', 1);
  fixture.componentRef.setInput('loading', false);
  fixture.componentRef.setInput('error', null);
  fixture.detectChanges();
  return fixture;
}

function element(fixture: ComponentFixture<AccessUserListComponent>, testId: string): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

describe('AccessUserListComponent', () => {
  it('emits the selected filter values and a trimmed search term', () => {
    const fixture = setup();
    const filters: unknown[] = [];
    fixture.componentInstance.filtersChange.subscribe((value) => filters.push(value));

    const search = element(fixture, 'access-users-search') as HTMLInputElement;
    search.value = '  معلّم  ';
    search.dispatchEvent(new Event('input'));
    const status = element(fixture, 'access-users-status') as HTMLSelectElement;
    status.value = 'pending';
    status.dispatchEvent(new Event('change'));
    const owner = element(fixture, 'access-users-owner') as HTMLSelectElement;
    owner.value = 'non-owner';
    owner.dispatchEvent(new Event('change'));
    element(fixture, 'access-users-filter-form').dispatchEvent(new Event('submit'));

    expect(filters).toEqual([{ status: 'pending', isOwner: false, search: 'معلّم' }]);
  });

  it('emits the chosen user id from the rendered list', () => {
    const fixture = setup();
    const selected: number[] = [];
    fixture.componentInstance.userSelected.subscribe((userId) => selected.push(userId));

    element(fixture, 'access-user-17').click();

    expect(selected).toEqual([17]);
  });
});
