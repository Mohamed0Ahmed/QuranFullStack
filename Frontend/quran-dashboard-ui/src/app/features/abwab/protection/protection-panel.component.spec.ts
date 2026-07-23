import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { CategoryProtectionProfileDto } from '../../../core/api/generated/models';
import { ProtectionPanelComponent, ApplyProtectionEvent } from './protection-panel.component';

function profileWith(overrides: Partial<CategoryProtectionProfileDto> = {}): CategoryProtectionProfileDto {
  return {
    categoryId: 'cat-1',
    serverTimeUtc: '2026-07-23T00:00:00.000Z',
    expectedTimelineGeneration: { generation: 1 },
    ordinaryProtection: { isActive: false, actorSubject: null, lastEditedAtUtc: null, expiresAtUtc: null },
    manualProtections: [
      { protectionType: 0, isProtected: false, isDirect: false, scope: null, sourceCategoryId: null, serverTimeUtc: '2026-07-23T00:00:00.000Z', actionClassification: 0, manualProtectionId: null, version: null },
      { protectionType: 3, isProtected: true, isDirect: true, scope: 1, sourceCategoryId: 'cat-1', serverTimeUtc: '2026-07-23T00:00:00.000Z', actionClassification: 2, manualProtectionId: 'mp-1', version: 5 },
    ],
    ...overrides,
  };
}

function render(inputs: { profile?: CategoryProtectionProfileDto | null; canView?: boolean; canApply?: boolean; canLift?: boolean } = {}) {
  const fixture = TestBed.createComponent(ProtectionPanelComponent);
  fixture.componentRef.setInput('profile', 'profile' in inputs ? (inputs.profile ?? null) : profileWith());
  fixture.componentRef.setInput('canView', inputs.canView ?? true);
  fixture.componentRef.setInput('canApply', inputs.canApply ?? true);
  fixture.componentRef.setInput('canLift', inputs.canLift ?? true);
  fixture.detectChanges();
  return fixture;
}

describe('ProtectionPanelComponent', () => {
  it('renders nothing when canView is false, even though a profile was supplied', () => {
    const fixture = render({ canView: false });
    expect(fixture.nativeElement.querySelector('[data-testid=protection-panel]')).toBeNull();
  });

  it('renders nothing when the profile is absent (the redacted composite-read case)', () => {
    const fixture = render({ profile: null, canView: true });
    expect(fixture.nativeElement.querySelector('[data-testid=protection-panel]')).toBeNull();
  });

  it('shows direct/inherited status, scope, and the server-derived ordinary expiry once granted', () => {
    const fixture = render({
      profile: profileWith({ ordinaryProtection: { isActive: true, actorSubject: 'editor-1', lastEditedAtUtc: '2026-07-22T00:00:00.000Z', expiresAtUtc: '2026-07-23T00:00:00.000Z' } }),
    });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid=protection-panel-ordinary]')!.textContent).toContain('editor-1');
    const statuses = root.querySelectorAll('[data-testid=protection-panel-status]');
    expect(statuses[0].textContent).toContain('غير محمي');
    expect(statuses[1].textContent).toContain('محمي مباشرة');
  });

  it('apply emits the selected protectionType and scope explicitly (no implicit default mutation)', () => {
    const fixture = render();
    let emitted: ApplyProtectionEvent | undefined;
    fixture.componentInstance.apply.subscribe((event) => (emitted = event));

    const root = fixture.nativeElement as HTMLElement;
    const select = root.querySelector<HTMLSelectElement>('[data-testid="protection-panel-scope-0"]')!;
    select.value = '1';
    select.dispatchEvent(new Event('change'));
    root.querySelector<HTMLButtonElement>('[data-testid=protection-panel-apply]')!.click();

    expect(emitted).toEqual({ protectionType: 0, scope: 1 });
  });

  it('lift is only offered for a directly-protected type, and only with canLift granted', () => {
    const fixture = render({ canLift: false });
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid=protection-panel-lift]')).toBeNull();
  });

  it('hides apply/lift affordances entirely when the corresponding permission is not granted', () => {
    const fixture = render({ canApply: false, canLift: false });
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid=protection-panel-apply]')).toBeNull();
    expect(root.querySelector('[data-testid=protection-panel-lift]')).toBeNull();
    expect(root.querySelector('[data-testid=protection-panel-apply-preset]')).toBeNull();
  });
});
