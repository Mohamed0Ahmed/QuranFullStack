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
  sectionId: 1,
  orderValue: 1,
  globalOrderValue: 1,
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

  it('emits addChild/editRequested/archiveRequested and never renders a protection entry', () => {
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

    expect(root.textContent).not.toContain('الحماية');
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

  it('emits moveRequested for the move-to op', () => {
    const fixture = render({ selectedDoor: DOOR });
    const moved: void[] = [];
    fixture.componentInstance.moveRequested.subscribe(() => moved.push(undefined));

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLElement>('[data-testid="abwab-side-panel-op-move"]')
      ?.click();

    expect(moved).toHaveLength(1);
  });

  describe('T503 — bulk mode', () => {
    it('toggles bulk mode and reflects the .on state via a tint class, never a solid fill', () => {
      const fixture = render({ bulkMode: false });
      const toggled: boolean[] = [];
      fixture.componentInstance.bulkModeToggled.subscribe((on: boolean) => toggled.push(on));
      const root = fixture.nativeElement as HTMLElement;
      const toggleBtn = root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]');
      expect(toggleBtn?.classList).not.toContain('abwab-side-panel__op--on');

      (toggleBtn as HTMLElement).click();

      expect(toggled).toEqual([true]);
    });

    it('marks the toggle as on and disables single-door ops while bulk mode is active', () => {
      const fixture = render({ selectedDoor: DOOR, bulkMode: true });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]')?.classList).toContain(
        'abwab-side-panel__op--on',
      );
    });

    it('disables the bulk toggle while the archive view is active (M23)', () => {
      const fixture = render({ archiveViewActive: true });
      const root = fixture.nativeElement as HTMLElement;

      expect(
        (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLButtonElement).disabled,
      ).toBe(true);
    });

    it('shows the bulk bar with the count and names, and emits bulk move/archive/clear', () => {
      const fixture = render({ bulkMode: true, bulkCount: 2, bulkNames: ['الألوهية', 'الربوبية'] });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-count"]')?.textContent).toContain('2');
      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-names"]')?.textContent).toContain('الألوهية');

      const bulkMoved: void[] = [];
      const bulkArchived: void[] = [];
      const bulkCleared: void[] = [];
      fixture.componentInstance.bulkMoveRequested.subscribe(() => bulkMoved.push(undefined));
      fixture.componentInstance.bulkArchiveRequested.subscribe(() => bulkArchived.push(undefined));
      fixture.componentInstance.bulkClearRequested.subscribe(() => bulkCleared.push(undefined));

      (root.querySelector('[data-testid="abwab-side-panel-bulk-move"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-side-panel-bulk-archive"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-side-panel-bulk-clear"]') as HTMLElement).click();

      expect(bulkMoved).toHaveLength(1);
      expect(bulkArchived).toHaveLength(1);
      expect(bulkCleared).toHaveLength(1);
    });

    it('hides the bulk bar entirely while bulk mode is off', () => {
      const fixture = render({ bulkMode: false });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-bar"]')).toBeNull();
    });
  });
});
