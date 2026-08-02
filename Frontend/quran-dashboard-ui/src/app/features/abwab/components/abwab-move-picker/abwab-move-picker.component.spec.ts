import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabMovePickerComponent } from './abwab-move-picker.component';
import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';

function node(overrides: Partial<AbwabNode> & { id: number; name: string }): AbwabNode {
  return {
    description: null,
    representativeAyahText: null,
    aliases: [],
    sectionId: 1,
    sectionRetired: false,
    parentId: null,
    orderValue: overrides.id,
    globalOrderValue: overrides.id,
    version: 1,
    isArchived: false,
    depth: 0,
    liveChildCount: 0,
    liveDescendantCount: 0,
    maxRelativeDepth: 0,
    relationCount: 0,
    children: [],
    ...overrides,
  };
}

const SECTIONS: AbwabTreeSectionDto[] = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 2 },
  { id: 2, name: 'العقيدة', orderValue: 2, version: 1, doorsInScopeCount: 1 },
];

// section 1: root(1) -> child(2); section 2: root(3)
const CHILD = node({ id: 2, name: 'الفرع', parentId: 1, sectionId: 1, depth: 1 });
const ROOT = node({ id: 1, name: 'الأصل', sectionId: 1, children: [CHILD] });
const OTHER_SECTION_ROOT = node({ id: 3, name: 'باب في قسم آخر', sectionId: 2 });
const LIVE_ROOTS = [ROOT, OTHER_SECTION_ROOT];

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabMovePickerComponent] });
  const fixture = TestBed.createComponent(AbwabMovePickerComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('sections', SECTIONS);
  fixture.componentRef.setInput('liveRoots', LIVE_ROOTS);
  fixture.componentRef.setInput('titleText', 'نقل «الأصل»');
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabMovePickerComponent — M30', () => {
  // No «بلا قسم» row: every door belongs to a section, so "no section" is not a destination and
  // offering it would only produce a 400.
  it('stage one lists the real sections and nothing else', () => {
    const root = render().nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')?.textContent).toContain(
      'اللغة العربية',
    );
    expect(root.querySelector('[data-testid="abwab-move-picker-section-2"]')?.textContent).toContain('العقيدة');
    expect(root.querySelector('[data-testid="abwab-move-picker-section-none"]')).toBeNull();
  });

  it('stage two, after picking a section, offers «كباب رئيسي» plus that section’s doors, indented by depth', () => {
    const fixture = render();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')?.textContent).toContain('الأصل');
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')?.textContent).toContain('الفرع');
  });

  it('excludes the moved door and its descendants from the destination list', () => {
    const fixture = render({ excludedIds: new Set([1, 2]) });
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
  });

  it('confirms «كباب رئيسي» scoped to the picked section as {targetParentId: null, targetSectionId}', () => {
    const fixture = render();
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

    expect(confirmed).toEqual([{ targetParentId: null, targetSectionId: 1 }]);
  });

  it('confirms nesting under a picked destination door as {targetParentId: <id>, targetSectionId}', () => {
    const fixture = render();
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-1"]') as HTMLElement).click();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

    expect(confirmed).toEqual([{ targetParentId: 1, targetSectionId: 1 }]);
  });

  describe('stage-one auto-selection', () => {
    // A single move already knows the answer — the door's own section — so asking would be a step
    // that only ever has one right response.
    it('skips stage one for a single door, landing on its own section', () => {
      const fixture = render({ movedSectionIds: [1], excludedIds: new Set([1, 2]) });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    });

    it('skips stage one for a bulk selection that shares one section', () => {
      const fixture = render({ movedSectionIds: [1, 1, 1] });

      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-move-picker-dest-asmain"]'),
      ).toBeTruthy();
    });

    // A selection spanning sections has no shared answer, so it is asked rather than guessed at.
    it('asks stage one for a bulk selection spanning sections', () => {
      const fixture = render({ movedSectionIds: [1, 2] });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeNull();
    });

    // Auto-selection is a starting point, not a commitment.
    it('lets an auto-selected section be changed', () => {
      const fixture = render({ movedSectionIds: [1] });
      (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-change-section"]') as HTMLElement).click();
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-section-2"]')).toBeTruthy();
    });

    it('confirms the auto-selected section without a stage-one click', () => {
      const fixture = render({ movedSectionIds: [2] });
      const confirmed: unknown[] = [];
      fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

      (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
      (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

      expect(confirmed).toEqual([{ targetParentId: null, targetSectionId: 2 }]);
    });

    // `movedSectionIds` is a fresh array on every snapshot rebuild, so a refresh landing while the
    // picker is open must not read as "a new selection" and reset a stage-two pick already made.
    it('survives a new movedSectionIds identity while open, keeping the pick already made', () => {
      const fixture = render({ movedSectionIds: [1] });
      (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
      fixture.detectChanges();

      fixture.componentRef.setInput('movedSectionIds', [1]);
      fixture.detectChanges();

      const confirmed: unknown[] = [];
      fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));
      (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

      expect(confirmed).toEqual([{ targetParentId: null, targetSectionId: 1 }]);
    });
  });

  it('T402 — the destination order follows liveRoots’ given order (the superset’s global order), not a per-section re-sort by orderValue', () => {
    // orderValue says door 6 ("أ") comes before door 5 ("ب") in section 1's own order — but
    // liveRoots (the global order this component is handed) lists 5 before 6, so the
    // destination list must too, per the T402 decision recorded on the component.
    const doorFive = node({ id: 5, name: 'ب', sectionId: 1, orderValue: 2 });
    const doorSix = node({ id: 6, name: 'أ', sectionId: 1, orderValue: 1 });
    const fixture = render({ liveRoots: [doorFive, doorSix] });

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();

    const destIds = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid^="abwab-move-picker-dest-"]'),
    ).map((el) => el.getAttribute('data-testid'));
    expect(destIds).toEqual(['abwab-move-picker-dest-asmain', 'abwab-move-picker-dest-5', 'abwab-move-picker-dest-6']);
  });

  it('emits closed on cancel without confirming', () => {
    const fixture = render();
    const closed: void[] = [];
    fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-cancel"]') as HTMLElement).click();

    expect(closed).toHaveLength(1);
  });

  describe('dialog semantics', () => {
    it('names itself as a dialog for assistive technology', () => {
      const fixture = render({ titleText: 'نقل الباب' });
      const dialog = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-move-picker"]')!;
      const titleId = dialog.getAttribute('aria-labelledby');

      expect(dialog.getAttribute('role')).toBe('dialog');
      expect(dialog.getAttribute('aria-modal')).toBe('true');
      expect(dialog.querySelector(`#${titleId}`)?.textContent).toContain('نقل الباب');
    });

    it('closes on Escape — a picker selection is not a draft to guard', () => {
      const fixture = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="abwab-move-picker"]')!
        .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

      expect(closed).toHaveLength(1);
    });

    // Where focus actually lands is a browser fact: jsdom gives every element a zero box, so the
    // CDK's focusable check rejects the target and auto-capture never fires. What this asserts is
    // the contract that produces it — the trap is attached and set to capture, and the first
    // tabbable control is a destination rather than something outside the picker.
    it('traps focus and captures it, with a destination control first in tab order', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const dialog = root.querySelector('[data-testid="abwab-move-picker"]')!;

      expect(dialog.hasAttribute('cdkTrapFocus')).toBe(true);
      expect(dialog.hasAttribute('cdkTrapFocusAutoCapture')).toBe(true);
      expect(dialog.querySelector('button')).toBe(root.querySelector('[data-testid="abwab-move-picker-section-1"]'));
    });

    it('keeps the actions out of the scrolling body', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const foot = root.querySelector('.qd-modal__foot')!;
      expect(foot.querySelector('[data-testid="abwab-move-picker-confirm"]')).toBeTruthy();
      expect(root.querySelector('.qd-modal__body')!.contains(foot)).toBe(false);
    });
  });
});
