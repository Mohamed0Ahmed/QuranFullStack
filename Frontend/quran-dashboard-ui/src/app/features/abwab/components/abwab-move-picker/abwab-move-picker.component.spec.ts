import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { By } from '@angular/platform-browser';

import { AbwabMovePickerComponent } from './abwab-move-picker.component';
import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

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
  fixture.componentRef.setInput('canConfirm', true);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabMovePickerComponent — M30', () => {
  it('renders a disabled confirmation and rejects stale confirmation without move permission', () => {
    const fixture = render({ canConfirm: false, movedSectionIds: [1] });
    const root = fixture.nativeElement as HTMLElement;
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((destination) => confirmed.push(destination));

    expect((root.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLButtonElement).disabled).toBe(true);
    (fixture.componentInstance as unknown as { confirm(): void }).confirm();

    expect(confirmed).toEqual([]);
  });

  // No «بلا قسم» cell: every door belongs to a section, so "no section" is not a destination and
  // offering it would only produce a 400.
  it('the strip lists the real sections and nothing else, all of them at once', () => {
    const root = render().nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')?.textContent).toContain(
      'اللغة العربية',
    );
    expect(root.querySelector('[data-testid="abwab-move-picker-section-2"]')?.textContent).toContain('العقيدة');
    expect(root.querySelector('[data-testid="abwab-move-picker-section-none"]')).toBeNull();
  });

  // The whole point of the reshape: sections stay on screen while the doors are chosen, so the
  // mover can always see which section they are aiming at.
  it('keeps the section strip visible alongside the doors, with no navigation step between them', () => {
    const fixture = render({ movedSectionIds: [1] });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-move-picker-section-2"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
  });

  // Rewritten for collapsed-on-open. This case previously asserted that BOTH «الأصل» and its child
  // «الفرع» were listed the moment a section was picked, which was true while the list rendered the
  // whole section expanded. It is now the opposite assertion, and that is the point of the change:
  // a section arriving pre-expanded is a wall of rows the user did not ask to see. What survives
  // unchanged is the part that was really being pinned — picking a section fills the panel in
  // place, «كباب رئيسي» included.
  it('picking a section lists its ROOT doors only, with descendants behind their branch', () => {
    const fixture = render();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')?.textContent).toContain('الأصل');
    expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();
  });

  describe('collapsed by default', () => {
    function openSectionOne(overrides: Record<string, unknown> = {}) {
      const fixture = render({ movedSectionIds: [1], ...overrides });
      fixture.detectChanges();
      return fixture;
    }

    // Not even the moved door's own parent chain is seeded: a move is a choice of a new home, and
    // pre-opening the old one puts the answer the user is moving away from at the top of the list.
    it('opens collapsed even for a single move whose door sits under a branch', () => {
      const root = openSectionOne({ excludedIds: new Set([2]) }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();
    });

    it('reveals a branch’s children when its chevron is used, and hides them again', () => {
      const fixture = openSectionOne();
      const root = fixture.nativeElement as HTMLElement;
      const chevron = root.querySelector('[data-testid="abwab-move-picker-dest-chevron-1"]') as HTMLElement;

      chevron.click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')?.textContent).toContain('الفرع');

      chevron.click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();
    });

    it('announces the branch’s state on the chevron rather than leaving it visual', () => {
      const fixture = openSectionOne();
      const root = fixture.nativeElement as HTMLElement;
      const chevron = root.querySelector('[data-testid="abwab-move-picker-dest-chevron-1"]')!;

      expect(chevron.getAttribute('aria-expanded')).toBe('false');
      expect(chevron.getAttribute('aria-label')).toBe(ABWAB_LABELS.relationPickerExpandAriaLabel('الأصل'));

      (chevron as HTMLElement).click();
      fixture.detectChanges();

      expect(chevron.getAttribute('aria-expanded')).toBe('true');
      expect(chevron.getAttribute('aria-label')).toBe(ABWAB_LABELS.relationPickerCollapseAriaLabel('الأصل'));
    });

    // The element stays for alignment, but a leaf has nothing to expand: no tab stop, nothing to
    // announce. Mirrors `abwab-door-picker`'s contract without sharing its code.
    it('gives a leaf no expand control — element kept, interaction removed', () => {
      const fixture = openSectionOne();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-move-picker-dest-chevron-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const leafChevron = root.querySelector('[data-testid="abwab-move-picker-dest-chevron-2"]')!;
      expect(leafChevron.getAttribute('tabindex')).toBe('-1');
      expect(leafChevron.getAttribute('aria-hidden')).toBe('true');
      expect(leafChevron.hasAttribute('aria-expanded')).toBe(false);
    });

    // A chevron that opens onto nothing is a worse answer than no chevron: the cycle guard has
    // already taken every child, so the branch is a leaf as far as this list is concerned.
    it('treats a branch whose every child is excluded as a leaf', () => {
      const root = openSectionOne({ excludedIds: new Set([2]) }).nativeElement as HTMLElement;
      const chevron = root.querySelector('[data-testid="abwab-move-picker-dest-chevron-1"]')!;

      expect(chevron.hasAttribute('aria-expanded')).toBe(false);
      expect(chevron.getAttribute('tabindex')).toBe('-1');
    });

    it('returns to collapsed roots when the section cell changes', () => {
      const fixture = openSectionOne();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-move-picker-dest-chevron-1"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeTruthy();

      (root.querySelector('[data-testid="abwab-move-picker-section-2"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();
    });

    // The cycle guard can take every root a section has. The panel is still not empty: moving to
    // the top of the section is a destination that no exclusion can remove.
    it('still offers «كباب رئيسي» when the cycle guard excludes every root in the section', () => {
      const root = openSectionOne({ excludedIds: new Set([1, 2]) }).nativeElement as HTMLElement;

      expect(root.querySelectorAll('[data-testid^="abwab-move-picker-dest-chevron-"]')).toHaveLength(0);
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    });
  });

  describe('search', () => {
    function search(fixture: ReturnType<typeof render>, query: string) {
      const input = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="abwab-move-picker-search"]',
      ) as HTMLInputElement;
      input.value = query;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    }

    it('filters to matching doors and drops the rest', () => {
      const fixture = render({ movedSectionIds: [1] });
      search(fixture, 'الأصل');
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-move-picker-no-matches"]')).toBeNull();
    });

    // A match under a collapsed branch would otherwise be filtered in and then hidden by the very
    // collapse the search is meant to see past.
    it('opens the ancestors of a deep match so the match is reachable', () => {
      const fixture = render({ movedSectionIds: [1] });
      expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();

      search(fixture, 'الفرع');
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-dest-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')?.textContent).toContain('الفرع');
    });

    // The contract clearing has to honour, stated as two halves: search-driven expansion is
    // derived, so clearing neither leaves the tree open behind the user NOR collapses what they
    // opened themselves.
    it('clearing returns expansion to exactly what the user opened by hand — no more, no less', () => {
      const fixture = render({ movedSectionIds: [1] });
      search(fixture, 'الفرع');
      search(fixture, '');
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')).toBeNull();

      (root.querySelector('[data-testid="abwab-move-picker-dest-chevron-1"]') as HTMLElement).click();
      fixture.detectChanges();
      search(fixture, 'الأصل');
      search(fixture, '');

      expect(root.querySelector('[data-testid="abwab-move-picker-dest-2"]')?.textContent).toContain('الفرع');
    });

    it('says the query reached nothing rather than showing a bare list', () => {
      const fixture = render({ movedSectionIds: [1] });
      search(fixture, 'لا يوجد باب بهذا الاسم');
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-no-matches"]')?.textContent).toContain(
        ABWAB_LABELS.pickerNoMatches,
      );
      // «كباب رئيسي» is pinned outside the filtered tree: a query that matches no door has not
      // taken away the option of moving to the top of the section.
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    });

    // An empty list in a section the cycle guard emptied is the guard's answer, not the query's —
    // «لا يوجد باب مطابق لبحثك» would blame the wrong thing.
    it('does not blame the query when the cycle guard is what emptied the section', () => {
      const fixture = render({ movedSectionIds: [1], excludedIds: new Set([1, 2]) });
      search(fixture, 'أي شيء');

      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-move-picker-no-matches"]'),
      ).toBeNull();
    });

    // The query is a filter over whichever section is active, so hopping the strip with one typed
    // is how a user finds a door whose section they have forgotten.
    it('survives a section change, unlike expansion', () => {
      const fixture = render({ movedSectionIds: [1] });
      search(fixture, 'باب في قسم آخر');
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-move-picker-no-matches"]')).toBeTruthy();

      (root.querySelector('[data-testid="abwab-move-picker-section-2"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(
        (root.querySelector('[data-testid="abwab-move-picker-search"]') as HTMLInputElement).value,
      ).toBe('باب في قسم آخر');
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-3"]')).toBeTruthy();
    });
  });

  it('marks the active section as a selected tab, and only that one', () => {
    const fixture = render({ movedSectionIds: [1] });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')?.getAttribute('aria-selected')).toBe(
      'true',
    );
    expect(root.querySelector('[data-testid="abwab-move-picker-section-2"]')?.getAttribute('aria-selected')).toBe(
      'false',
    );
  });

  // A truncated name is unreadable without its `title`; §17 makes the attribute part of the
  // contract, not a nicety, and the strip's fixed 150px cells are where names actually overflow.
  //
  // D35 moved where the claim is made rather than whether it is made: `title` on the inner
  // non-interactive span was the only disclosure path and was unreachable by keyboard, so the full
  // name now sits on the cell itself — the focusable control that owns the truncation — as both the
  // hover `title` and the accessible name, which the span alone could never provide.
  it('gives every section cell its full name on the cell itself, since the cell truncates', () => {
    const root = render().nativeElement as HTMLElement;
    const cell = root.querySelector('[data-testid="abwab-move-picker-section-1"]')!;

    expect(cell.getAttribute('title')).toBe('اللغة العربية');
    expect(cell.getAttribute('aria-label')).toBe('اللغة العربية');
    expect(cell.querySelector('.qd-truncate')?.hasAttribute('title')).toBe(false);
  });

  it('switching sections drops a destination picked in the section just left', () => {
    const fixture = render();
    const confirmed: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((v) => confirmed.push(v));

    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-dest-1"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-section-2"]') as HTMLElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

    expect(confirmed).toEqual([{ targetParentId: null, targetSectionId: 2 }]);
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

  describe('auto-selection', () => {
    // A single move already knows the answer — the door's own section — so the strip opens with
    // that cell marked and its doors already listed.
    it('opens on the moved door’s own section for a single door', () => {
      const fixture = render({ movedSectionIds: [1], excludedIds: new Set([1, 2]) });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-move-picker-section-1"]')?.getAttribute('aria-selected')).toBe(
        'true',
      );
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeTruthy();
    });

    it('opens on the shared section for a bulk selection that agrees', () => {
      const fixture = render({ movedSectionIds: [1, 1, 1] });

      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-move-picker-dest-asmain"]'),
      ).toBeTruthy();
    });

    // A selection spanning sections has no shared answer, so nothing is guessed at: the strip opens
    // with no cell marked and the doors area carries a prompt instead of a destination list.
    it('opens with no active cell for a bulk selection spanning sections, prompting instead of listing', () => {
      const fixture = render({ movedSectionIds: [1, 2] });
      const root = fixture.nativeElement as HTMLElement;

      const cells = Array.from(root.querySelectorAll('[data-testid^="abwab-move-picker-section-"]'));
      expect(cells).toHaveLength(2);
      expect(cells.every((cell) => cell.getAttribute('aria-selected') === 'false')).toBe(true);
      expect(root.querySelector('[data-testid="abwab-move-picker-no-section"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]')).toBeNull();
    });

    // With no section there is no `targetSectionId` to send, and `confirm()` would silently do
    // nothing — so the button says so rather than looking live and acting dead.
    it('disables confirm while no section is active, and enables it once one is picked', () => {
      const fixture = render({ movedSectionIds: [1, 2] });
      const root = fixture.nativeElement as HTMLElement;
      const confirm = root.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLButtonElement;

      expect(confirm.disabled).toBe(true);

      (root.querySelector('[data-testid="abwab-move-picker-section-2"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(confirm.disabled).toBe(false);
    });

    // Auto-selection is a starting point, not a commitment — and now it takes one click, not two.
    it('lets an auto-selected section be changed straight from the strip', () => {
      const fixture = render({ movedSectionIds: [1] });
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-move-picker-section-2"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-move-picker-section-2"]')?.getAttribute('aria-selected')).toBe(
        'true',
      );
      expect(root.querySelector('[data-testid="abwab-move-picker-dest-3"]')?.textContent).toContain('باب في قسم آخر');
    });

    it('confirms the auto-selected section without a strip click', () => {
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

    // `:not(...-chevron-...)`: the chevron each row gained for collapse/expand shares the `-dest-`
    // prefix, and this case is about the order of the pick controls, not of every element in a row.
    const destIds = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll(
        '[data-testid^="abwab-move-picker-dest-"]:not([data-testid^="abwab-move-picker-dest-chevron-"])',
      ),
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

    // Rewritten for the section strip (ux-slice-m). The pin it replaces asserted that
    // `dialog.querySelector('button')` — the first button in the DOM — was a destination control,
    // which was a fair reading of tab order while every control was plainly tabbable. It is the
    // wrong assertion now: the strip is a `qd-tabs` tablist, so all but one of its cells carry
    // `tabindex="-1"` and being first in the DOM says nothing about being first in tab order. The
    // decision this records is unchanged in substance — the trap captures into the picker's own
    // destination controls, not something outside it — and gains the thing the strip makes
    // meaningful: the single tabbable cell is the one the move actually starts from.
    //
    // Where focus lands is still a browser fact: jsdom gives every element a zero box, so the CDK's
    // focusable check rejects the target and auto-capture never fires. What is asserted here is the
    // contract that produces it.
    //
    // The trap moved to `qd-modal-shell`, which binds it as a property (`[cdkTrapFocus]`) rather
    // than the static attributes this used to read, and replaces auto-capture with the shell's own
    // initial-focus call. The trap is therefore asserted through the directive instance and its
    // host element, which says the same thing the attributes did: this dialog is the trapped one.
    it('traps focus and captures it, with the moved door’s own section the one tabbable cell', () => {
      const fixture = render({ movedSectionIds: [1] });
      const root = fixture.nativeElement as HTMLElement;
      const dialog = root.querySelector('[data-testid="abwab-move-picker"]')!;

      const trap = fixture.debugElement.query(By.directive(CdkTrapFocus));
      expect(trap.nativeElement).toBe(dialog);
      expect(trap.injector.get(CdkTrapFocus).enabled).toBe(true);

      const tabbableCells = Array.from(dialog.querySelectorAll('[role="tab"]')).filter(
        (cell) => cell.getAttribute('tabindex') === '0',
      );
      expect(tabbableCells).toEqual([root.querySelector('[data-testid="abwab-move-picker-section-1"]')]);
    });

    // The no-active-section case still has to be reachable by keyboard: `qd-tabs` falls back to the
    // first enabled cell when nothing is selected, so the strip never becomes a dead end.
    it('keeps one cell tabbable even when a bulk selection leaves no section active', () => {
      const fixture = render({ movedSectionIds: [1, 2] });
      const root = fixture.nativeElement as HTMLElement;

      const tabbableCells = Array.from(root.querySelectorAll('[role="tab"]')).filter(
        (cell) => cell.getAttribute('tabindex') === '0',
      );
      expect(tabbableCells).toEqual([root.querySelector('[data-testid="abwab-move-picker-section-1"]')]);
    });

    it('names the destination list as the panel the active section controls', () => {
      const fixture = render({ movedSectionIds: [1] });
      const root = fixture.nativeElement as HTMLElement;
      const activeCell = root.querySelector('[data-testid="abwab-move-picker-section-1"]')!;
      const panel = root.querySelector('[role="tabpanel"]')!;

      expect(panel.getAttribute('aria-labelledby')).toBe(activeCell.getAttribute('id'));
      expect(activeCell.getAttribute('aria-controls')).toBe(panel.getAttribute('id'));
    });

    // Sticky footer and single body scroller are `qd-modal-shell`'s now, so the two regions are
    // read from the shell's published test ids instead of the retired `.qd-modal__*` classes.
    it('keeps the actions out of the scrolling body', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const foot = root.querySelector('[data-testid="abwab-move-picker-footer"]')!;
      expect(foot.querySelector('[data-testid="abwab-move-picker-confirm"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-move-picker-body"]')!.contains(foot)).toBe(false);
    });
  });

  // F-64: the picked destination was carried by background + weight alone, so the target of a
  // structural move was not programmatically determinable. Same attribute as the relations
  // modal's direction pill (abwab-relations-modal.component.html:109,118).
  describe('the picked destination is programmatically determinable', () => {
    it('marks the picked destination with aria-pressed and moves it when another is picked', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const pressed = (testId: string) => root.querySelector(`[data-testid="${testId}"]`)!.getAttribute('aria-pressed');

      (root.querySelector('[data-testid="abwab-move-picker-section-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(pressed('abwab-move-picker-dest-asmain')).toBe('true');
      expect(pressed('abwab-move-picker-dest-1')).toBe('false');

      (root.querySelector('[data-testid="abwab-move-picker-dest-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(pressed('abwab-move-picker-dest-asmain')).toBe('false');
      expect(pressed('abwab-move-picker-dest-1')).toBe('true');
    });
  });
});
