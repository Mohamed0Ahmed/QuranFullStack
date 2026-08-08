import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';
import { AccessAdvancedSecurityComponent } from './access-advanced-security.component';

const USER: AccessUserDetail = {
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

const PREVIEW = {
  userId: 17,
  oldSub: 'subject-17',
  newSub: 'new-subject',
  version: 4,
  isOwner: false,
};

function setup(
  user: AccessUserDetail | null = USER,
  hasUnsavedPermissions = false,
): ComponentFixture<AccessAdvancedSecurityComponent> {
  TestBed.configureTestingModule({ imports: [AccessAdvancedSecurityComponent] });
  const fixture = TestBed.createComponent(AccessAdvancedSecurityComponent);
  fixture.componentRef.setInput('user', user);
  fixture.componentRef.setInput('relinkPreview', null);
  fixture.componentRef.setInput('busyAction', null);
  fixture.componentRef.setInput('hasUnsavedPermissions', hasUnsavedPermissions);
  fixture.detectChanges();
  return fixture;
}

function element(
  fixture: ComponentFixture<AccessAdvancedSecurityComponent>,
  testId: string,
): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function fillRelinkForm(fixture: ComponentFixture<AccessAdvancedSecurityComponent>): {
  newSub: HTMLInputElement;
  evidence: HTMLInputElement;
} {
  const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
  newSub.value = 'new-subject';
  newSub.dispatchEvent(new Event('input'));
  const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
  evidence.value = 'verified-evidence';
  evidence.dispatchEvent(new Event('input'));
  fixture.detectChanges();
  return { newSub, evidence };
}

describe('AccessAdvancedSecurityComponent', () => {
  it('presents relink as identity recovery that is separate from permission editing', () => {
    const fixture = setup();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('الأمان المتقدم');
    expect(text).toContain('ليست جزءًا من تحرير الصلاحيات');
    expect(text).toContain('member@example.test');
    expect(fixture.nativeElement.querySelector('[data-testid^="access-permission-"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
  });

  it('carries no accent eyebrow above its heading', () => {
    const fixture = setup();
    const heading = fixture.nativeElement.querySelector('#access-advanced-security-title') as HTMLElement;

    expect(heading.textContent?.trim()).toBe('الأمان المتقدم');
    expect(heading.parentElement?.firstElementChild).toBe(heading);
  });

  it('offers identity recovery for an Owner account without claiming it is uneditable', () => {
    const fixture = setup({ ...USER, isOwner: true, permissionCodes: [] });

    expect(element(fixture, 'access-relink-new-sub')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('بما في ذلك حساب المالك');
  });

  it('warns only an Owner target about the reconciliation precondition its confirmation adds', () => {
    const fixture = setup({ ...USER, isOwner: true, permissionCodes: [] });
    const precondition = element(fixture, 'access-relink-owner-precondition').textContent ?? '';

    expect(precondition).toContain('ضمن إعدادات المالكين');
    expect(precondition).toContain('مطابقة المالكين');
    expect(precondition).toContain(ACCESS_ADMIN_LABELS.reconciliationCandidateState('Unchanged'));
    expect(precondition).not.toContain('Unchanged');

    fixture.componentRef.setInput('user', USER);
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="access-relink-owner-precondition"]'),
    ).toBeNull();
  });

  it('offers no recovery form until a user is selected', () => {
    const fixture = setup(null);

    expect(fixture.nativeElement.querySelector('[data-testid="access-relink-new-sub"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('اختر مستخدمًا لبدء استعادة الهوية');
  });

  it('requires a preview before relink confirmation and never renders an email-only control', () => {
    const fixture = setup();
    const previews: { newSub: string; evidenceToken: string }[] = [];
    const confirmations: string[] = [];
    fixture.componentInstance.relinkPreviewRequested.subscribe((request) => previews.push(request));
    fixture.componentInstance.relinkConfirmed.subscribe((reason) => confirmations.push(reason));

    expect(fixture.nativeElement.querySelector('[data-testid="access-relink-confirmation"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('input[type="email"]')).toBeNull();

    const { evidence } = fillRelinkForm(fixture);
    expect(evidence.type).toBe('password');
    element(fixture, 'access-relink-preview').click();
    fixture.detectChanges();

    expect(previews).toEqual([{ newSub: 'new-subject', evidenceToken: 'verified-evidence' }]);
    expect(evidence.value).toBe('');

    fixture.componentRef.setInput('relinkPreview', PREVIEW);
    fixture.detectChanges();

    const confirmation = element(fixture, 'access-relink-confirmation');
    expect(confirmation.textContent).toContain('المعرّف الحالي');
    expect(confirmation.textContent).toContain('subject-17');
    expect(confirmation.textContent).toContain('المعرّف الجديد');
    expect(confirmation.textContent).toContain('new-subject');
    expect(confirmation.textContent).not.toContain('←');

    const reason = element(fixture, 'access-relink-confirm-reason') as HTMLTextAreaElement;
    reason.value = 'تحديث المعرّف';
    reason.dispatchEvent(new Event('input'));
    const confirmed = element(fixture, 'access-relink-confirmed') as HTMLInputElement;
    confirmed.checked = true;
    confirmed.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    element(fixture, 'access-relink-confirm').click();

    expect(confirmations).toEqual(['تحديث المعرّف']);
  });

  it('clears relink values and emits cancellation when the preview is abandoned', () => {
    const fixture = setup();
    let cancellations = 0;
    fixture.componentInstance.relinkCancelled.subscribe(() => cancellations++);

    const { newSub, evidence } = fillRelinkForm(fixture);
    fixture.componentRef.setInput('relinkPreview', PREVIEW);
    fixture.detectChanges();

    element(fixture, 'access-relink-cancel').click();
    fixture.detectChanges();

    expect(cancellations).toBe(1);
    expect(newSub.value).toBe('');
    expect(evidence.value).toBe('');
  });

  it('holds back relink while permission edits are unsaved', () => {
    const fixture = setup(USER, true);
    const previews: unknown[] = [];
    fixture.componentInstance.relinkPreviewRequested.subscribe((request) => previews.push(request));

    fillRelinkForm(fixture);

    expect(element(fixture, 'access-relink-blocked')).toBeTruthy();
    expect((element(fixture, 'access-relink-preview') as HTMLButtonElement).disabled).toBe(true);
    element(fixture, 'access-relink-preview').click();

    expect(previews).toEqual([]);
  });

  it('holds back a previewed relink when the draft turns dirty before confirmation', () => {
    const fixture = setup();
    const confirmations: string[] = [];
    fixture.componentInstance.relinkConfirmed.subscribe((reason) => confirmations.push(reason));

    fillRelinkForm(fixture);
    fixture.componentRef.setInput('relinkPreview', PREVIEW);
    fixture.detectChanges();
    const reason = element(fixture, 'access-relink-confirm-reason') as HTMLTextAreaElement;
    reason.value = 'تحديث المعرّف';
    reason.dispatchEvent(new Event('input'));
    const confirmed = element(fixture, 'access-relink-confirmed') as HTMLInputElement;
    confirmed.checked = true;
    confirmed.dispatchEvent(new Event('change'));
    fixture.componentRef.setInput('hasUnsavedPermissions', true);
    fixture.detectChanges();

    expect(element(fixture, 'access-relink-blocked')).toBeTruthy();
    expect((element(fixture, 'access-relink-confirm') as HTMLButtonElement).disabled).toBe(true);
    element(fixture, 'access-relink-confirm').click();

    expect(confirmations).toEqual([]);
  });

  it('clears the relink form when the selected target changes', () => {
    const fixture = setup();

    const { newSub } = fillRelinkForm(fixture);
    fixture.componentRef.setInput('user', { ...USER, id: 18, email: 'other@example.test' });
    fixture.detectChanges();

    expect(newSub.value).toBe('');
  });
});
