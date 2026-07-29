import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabSidePanelComponent } from './abwab-side-panel.component';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';

const DOOR: AbwabDoorDto = {
  id: 1,
  name: 'العلم بالله',
  description: null,
  representativeAyahText: null,
  aliases: [],
  parentId: null,
  sectionId: null,
  orderValue: 1,
  version: 1,
};

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabSidePanelComponent] });
  const fixture = TestBed.createComponent(AbwabSidePanelComponent);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabSidePanelComponent', () => {
  it('shows the empty hint and disables operations when nothing is selected', () => {
    const fixture = render({ selectedDoor: null });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-side-panel-empty"]')).toBeTruthy();
    const buttons = Array.from(root.querySelectorAll('[data-testid^="abwab-side-panel-op-"]')) as HTMLButtonElement[];
    expect(buttons.length).toBeGreaterThan(0);
    expect(buttons.every((b) => b.disabled)).toBe(true);
  });

  it('shows the active door name and enables operations when selected', () => {
    const fixture = render({ selectedDoor: DOOR });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')?.textContent).toContain(
      'العلم بالله',
    );
    const buttons = Array.from(root.querySelectorAll('[data-testid^="abwab-side-panel-op-"]')) as HTMLButtonElement[];
    expect(buttons.every((b) => !b.disabled)).toBe(true);
  });

  it('emits addChild/editRequested/archiveRequested and never renders relations/protection entries', () => {
    const fixture = render({ selectedDoor: DOOR });
    const root = fixture.nativeElement as HTMLElement;

    const addChild: void[] = [];
    const edit: void[] = [];
    const archive: void[] = [];
    fixture.componentInstance.addChildRequested.subscribe(() => addChild.push(undefined));
    fixture.componentInstance.editRequested.subscribe(() => edit.push(undefined));
    fixture.componentInstance.archiveRequested.subscribe(() => archive.push(undefined));

    (root.querySelector('[data-testid="abwab-side-panel-op-add-child"]') as HTMLElement).click();
    (root.querySelector('[data-testid="abwab-side-panel-op-edit"]') as HTMLElement).click();
    (root.querySelector('[data-testid="abwab-side-panel-op-archive"]') as HTMLElement).click();

    expect(addChild).toHaveLength(1);
    expect(edit).toHaveLength(1);
    expect(archive).toHaveLength(1);

    expect(root.textContent).not.toContain('العلاقات');
    expect(root.textContent).not.toContain('الحماية');
    expect(root.querySelector('[data-testid="abwab-side-panel-op-relations"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-side-panel-op-protect"]')).toBeNull();
  });

  it('clears the selection when the clear control is used', () => {
    const fixture = render({ selectedDoor: DOOR });
    const cleared: void[] = [];
    fixture.componentInstance.clearRequested.subscribe(() => cleared.push(undefined));

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLElement>('[data-testid="abwab-side-panel-clear"]')
      ?.click();

    expect(cleared).toHaveLength(1);
  });
});
