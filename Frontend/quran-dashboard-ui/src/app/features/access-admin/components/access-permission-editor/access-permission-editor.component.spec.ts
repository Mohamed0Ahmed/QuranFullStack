import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PermissionCode } from '../../../../core/auth/permission-code';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessPermissionEditorComponent } from './access-permission-editor.component';

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

function checkbox(fixture: ComponentFixture<AccessPermissionEditorComponent>, testId: string): HTMLInputElement {
  const element = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLInputElement | null;
  if (!element) {
    throw new Error(`Missing checkbox ${testId}`);
  }
  return element;
}

describe('AccessPermissionEditorComponent', () => {
  it('expands select-all into individual codes and leaves no group identity in the emitted selection', () => {
    TestBed.configureTestingModule({ imports: [AccessPermissionEditorComponent] });
    const fixture = TestBed.createComponent(AccessPermissionEditorComponent);
    const emitted: PermissionCode[][] = [];
    fixture.componentRef.setInput('groups', GROUPS);
    fixture.componentRef.setInput('selectedCodes', new Set<PermissionCode>());
    fixture.componentRef.setInput('disabled', false);
    fixture.componentInstance.selectionChange.subscribe((codes) => emitted.push(codes));
    fixture.detectChanges();

    const selectAll = checkbox(fixture, 'access-permission-group-doors');
    selectAll.checked = true;
    selectAll.dispatchEvent(new Event('change'));

    expect(emitted).toEqual([['abwab.doors.create', 'abwab.doors.edit']]);
  });

  it('marks a partly selected group indeterminate and a fully selected group checked', () => {
    TestBed.configureTestingModule({ imports: [AccessPermissionEditorComponent] });
    const fixture = TestBed.createComponent(AccessPermissionEditorComponent);
    fixture.componentRef.setInput('groups', GROUPS);
    fixture.componentRef.setInput('selectedCodes', new Set<PermissionCode>(['abwab.doors.create']));
    fixture.componentRef.setInput('disabled', false);
    fixture.detectChanges();

    const selectAll = checkbox(fixture, 'access-permission-group-doors');
    expect(selectAll.checked).toBe(false);
    expect(selectAll.indeterminate).toBe(true);

    fixture.componentRef.setInput(
      'selectedCodes',
      new Set<PermissionCode>(['abwab.doors.create', 'abwab.doors.edit']),
    );
    fixture.detectChanges();

    expect(selectAll.checked).toBe(true);
    expect(selectAll.indeterminate).toBe(false);
  });

  it('allows an individual code to be unchecked after its group was selected', () => {
    TestBed.configureTestingModule({ imports: [AccessPermissionEditorComponent] });
    const fixture = TestBed.createComponent(AccessPermissionEditorComponent);
    const emitted: PermissionCode[][] = [];
    fixture.componentRef.setInput('groups', GROUPS);
    fixture.componentRef.setInput(
      'selectedCodes',
      new Set<PermissionCode>(['abwab.doors.create', 'abwab.doors.edit']),
    );
    fixture.componentRef.setInput('disabled', false);
    fixture.componentInstance.selectionChange.subscribe((codes) => emitted.push(codes));
    fixture.detectChanges();

    const edit = checkbox(fixture, 'access-permission-abwab.doors.edit');
    edit.checked = false;
    edit.dispatchEvent(new Event('change'));

    expect(emitted).toEqual([['abwab.doors.create']]);
  });
});
