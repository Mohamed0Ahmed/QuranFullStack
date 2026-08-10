import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabCardsComponent } from './abwab-cards.component';
import { AbwabNode } from '../../models/abwab.models';
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

// جذر(1) -> ابن(2) -> حفيد(3, ورقة)
const GRANDCHILD = node({ id: 3, name: 'حفيد', parentId: 2, depth: 2 });
const CHILD = node({ id: 2, name: 'ابن', parentId: 1, depth: 1, liveChildCount: 1, children: [GRANDCHILD] });
const ROOT = node({ id: 1, name: 'جذر', depth: 0, liveChildCount: 1, children: [CHILD] });
const LEAF_ROOT = node({ id: 4, name: 'ورقة' });
const ARCHIVED = node({ id: 99, name: 'مؤرشف', isArchived: true });

const ROOTS = [ROOT, LEAF_ROOT];
const BY_ID = new Map<number, AbwabNode>([
  [1, ROOT],
  [2, CHILD],
  [3, GRANDCHILD],
  [4, LEAF_ROOT],
  [99, ARCHIVED],
]);

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabCardsComponent] });
  const fixture = TestBed.createComponent(AbwabCardsComponent);
  fixture.componentRef.setInput('roots', ROOTS);
  fixture.componentRef.setInput('byId', BY_ID);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabCardsComponent', () => {
  describe('M25 — drills into live children only and restores the level from card', () => {
    it('renders the root level with «كل الأبواب» as the only crumb when cardId is null', () => {
      const root = render().nativeElement as HTMLElement;
      const crumbs = Array.from(root.querySelectorAll('[data-testid="abwab-cards-crumb"]'));
      expect(crumbs).toHaveLength(1);
      expect(root.querySelector('[data-testid="abwab-card-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-card-4"]')).toBeTruthy();
    });

    it('restores the drilled level and breadcrumb chain from a nested cardId', () => {
      const root = render({ cardId: 2 }).nativeElement as HTMLElement;
      const crumbs = Array.from(root.querySelectorAll('[data-testid="abwab-cards-crumb"]')).map((el) =>
        el.textContent?.trim(),
      );
      expect(crumbs).toEqual(['كل الأبواب', 'جذر', 'ابن']);
      expect(root.querySelector('[data-testid="abwab-card-3"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-card-1"]')).toBeNull();
    });

    it('clicking a branch card emits drilled with its id and selected with its id', () => {
      const fixture = render();
      const drilled: number[] = [];
      const selected: number[] = [];
      fixture.componentInstance.drilled.subscribe((id) => drilled.push(id));
      fixture.componentInstance.selected.subscribe((id) => selected.push(id));

      (fixture.nativeElement.querySelector('[data-testid="abwab-card-1"]') as HTMLElement).click();

      expect(drilled).toEqual([1]);
      expect(selected).toEqual([1]);
    });

    it('clicking a leaf card selects it but never emits drilled', () => {
      const fixture = render();
      const drilled: number[] = [];
      const selected: number[] = [];
      fixture.componentInstance.drilled.subscribe((id) => drilled.push(id));
      fixture.componentInstance.selected.subscribe((id) => selected.push(id));

      (fixture.nativeElement.querySelector('[data-testid="abwab-card-4"]') as HTMLElement).click();

      expect(drilled).toEqual([]);
      expect(selected).toEqual([4]);
    });

    it('marks a leaf card as non-drillable via a modifier class', () => {
      const root = render().nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-card-4"]')?.classList.contains('abwab-cards__card--leaf')).toBe(
        true,
      );
      expect(root.querySelector('[data-testid="abwab-card-1"]')?.classList.contains('abwab-cards__card--leaf')).toBe(
        false,
      );
    });

    it.each([
      ['an archived door id', 99],
      ['an id absent from the snapshot', 12345],
    ])('fails closed to the root level when cardId names %s', (_label, cardId) => {
      const root = render({ cardId }).nativeElement as HTMLElement;
      const crumbs = Array.from(root.querySelectorAll('[data-testid="abwab-cards-crumb"]'));
      expect(crumbs).toHaveLength(1);
      expect(root.querySelector('[data-testid="abwab-card-1"]')).toBeTruthy();
    });

    it('clicking an ancestor breadcrumb emits crumbSelected with that ancestor id, and the root crumb emits null', () => {
      const fixture = render({ cardId: 2 });
      const crumbSelected: (number | null)[] = [];
      fixture.componentInstance.crumbSelected.subscribe((id) => crumbSelected.push(id));
      const crumbs = fixture.nativeElement.querySelectorAll('[data-testid="abwab-cards-crumb"]');

      (crumbs[0] as HTMLElement).click(); // «كل الأبواب»
      (crumbs[1] as HTMLElement).click(); // «جذر»

      expect(crumbSelected).toEqual([null, 1]);
    });
  });

  describe('T404 — the order badge follows orderScope at the top level only', () => {
    const topRoot = node({ id: 1, name: 'جذر', orderValue: 1, globalOrderValue: 42 });

    it('shows globalOrderValue at the top level when the superset is active', () => {
      const root = render({ roots: [topRoot], orderScope: 'global' }).nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-card-1"] .abwab-cards__order')?.textContent?.trim()).toBe('42');
    });

    it('keeps orderValue at the top level when a section is active, even though roots is unchanged', () => {
      const root = render({ roots: [topRoot], orderScope: 'section' }).nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-card-1"] .abwab-cards__order')?.textContent?.trim()).toBe('1');
    });

    it('keeps orderValue on a drilled-in level even under orderScope=global — cardId is no longer null', () => {
      const child = node({ id: 2, name: 'ابن', parentId: 1, depth: 1, orderValue: 1, globalOrderValue: 42 });
      const parent = node({ id: 1, name: 'جذر', orderValue: 1, globalOrderValue: 7, children: [child] });
      const byId = new Map<number, AbwabNode>([
        [1, parent],
        [2, child],
      ]);
      const root = render({ roots: [parent], byId, cardId: 1, orderScope: 'global' }).nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-card-2"] .abwab-cards__order')?.textContent?.trim()).toBe('1');
    });
  });

  describe('bulk mode', () => {
    it('toggles bulk selection instead of drilling or selecting when bulk mode is on', () => {
      const fixture = render({ bulkMode: true });
      const drilled: number[] = [];
      const bulkToggled: number[] = [];
      fixture.componentInstance.drilled.subscribe((id) => drilled.push(id));
      fixture.componentInstance.bulkToggled.subscribe((id) => bulkToggled.push(id));

      (fixture.nativeElement.querySelector('[data-testid="abwab-card-1"]') as HTMLElement).click();

      expect(drilled).toEqual([]);
      expect(bulkToggled).toEqual([1]);
    });

    it('renders a checked checkbox for bulk-selected cards', () => {
      const root = render({ bulkMode: true, bulkSelectedIds: new Set([4]) }).nativeElement as HTMLElement;
      const checkbox = root.querySelector<HTMLInputElement>('[data-testid="abwab-card-checkbox-4"]');
      expect(checkbox?.checked).toBe(true);
    });

    // Several identical «مربع اختيار» in a grid tell a screen-reader user nothing about which
    // door each one selects (§17's accessible-name contract, Slice D's half of the debt).
    it('names each checkbox after its own door', () => {
      const root = render({ bulkMode: true }).nativeElement as HTMLElement;
      const checkbox = root.querySelector('[data-testid="abwab-card-checkbox-4"]');
      expect(checkbox?.getAttribute('aria-label')).toBe('ورقة');
      expect(checkbox?.classList.contains('qd-checkbox')).toBe(true);
    });
  });

  describe('F-56 — every card is a real interactive element carrying an accessible name', () => {
    it('renders each card as a button named after its door, so Tab reaches it and Enter/Space press it', () => {
      const root = render().nativeElement as HTMLElement;
      const card = root.querySelector('[data-testid="abwab-card-4"]');

      expect(card?.tagName).toBe('BUTTON');
      expect(card?.getAttribute('type')).toBe('button');
      expect(card?.getAttribute('aria-label')).toBe('ورقة');
    });

    // A checkbox nested inside the card button would be interactive content inside a button:
    // invalid HTML, and unreachable by keyboard for exactly the reason the div was.
    it('keeps the bulk checkbox outside the card button rather than nested in it', () => {
      const root = render({ bulkMode: true }).nativeElement as HTMLElement;
      const card = root.querySelector('[data-testid="abwab-card-4"]')!;

      expect(card.querySelector('input')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-card-checkbox-4"]')).toBeTruthy();
    });
  });

  describe('F-89 — the card child count declares its scope, matching the tree badge', () => {
    it('labels the count with the shared direct-children phrase from abwab.labels', () => {
      const root = render().nativeElement as HTMLElement;
      const meta = root.querySelector('[data-testid="abwab-card-1"] .abwab-cards__meta');

      expect(meta?.getAttribute('aria-label')).toBe(ABWAB_LABELS.rowChildCountAriaLabel(1));
      expect(ABWAB_LABELS.rowChildCountAriaLabel(1)).toContain('مباشرة');
    });

    it('gates the digit and its label on liveChildCount, so the condition and the number describe one set', () => {
      const orphaned = node({ id: 7, name: 'بلا أحياء', liveChildCount: 0, children: [GRANDCHILD] });
      const root = render({ roots: [orphaned], byId: new Map([[7, orphaned]]) }).nativeElement as HTMLElement;
      const meta = root.querySelector('[data-testid="abwab-card-7"] .abwab-cards__meta');

      expect(meta?.textContent?.trim()).toBe('');
      expect(meta?.getAttribute('aria-label')).toBeNull();
    });
  });

  describe('F-53 — the level that is on screen has an empty state and a distinct no-results state', () => {
    it('states that there are no doors yet when the level is genuinely empty', () => {
      const root = render({ roots: [] }).nativeElement as HTMLElement;
      const state = root.querySelector('[data-testid="abwab-cards-empty"]');

      expect(state?.textContent).toContain(ABWAB_LABELS.emptyTreeMessage);
      expect(root.querySelector('.abwab-cards__grid')).toBeNull();
    });

    it('reads differently when a query filtered every door away — the doors exist, the matches do not', () => {
      const root = render({ isFiltering: true, visibleIds: new Set<number>() }).nativeElement as HTMLElement;
      const state = root.querySelector('[data-testid="abwab-cards-empty"]');

      expect(state?.textContent).toContain(ABWAB_LABELS.noSearchMatchesMessage);
      expect(state?.textContent).not.toContain(ABWAB_LABELS.emptyTreeMessage);
    });

    it('keeps the breadcrumb on screen in the no-results state so a drilled user can get back out', () => {
      const root = render({ cardId: 2, isFiltering: true, visibleIds: new Set([1, 2]) })
        .nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-cards-empty"]')).toBeTruthy();
      expect(Array.from(root.querySelectorAll('[data-testid="abwab-cards-crumb"]'))).toHaveLength(3);
    });
  });

  describe('F-55 — the query filters at every level, and a matching root stays openable', () => {
    it('keeps a matching root drillable with its real child count when no descendant matches', () => {
      const fixture = render({ isFiltering: true, visibleIds: new Set([1]) });
      const root = fixture.nativeElement as HTMLElement;
      const card = root.querySelector('[data-testid="abwab-card-1"]')!;
      const drilled: number[] = [];
      fixture.componentInstance.drilled.subscribe((id) => drilled.push(id));

      expect(card.classList.contains('abwab-cards__card--leaf')).toBe(false);
      expect(card.querySelector('.abwab-cards__meta')?.textContent?.trim()).toBe('1');

      (card as HTMLElement).click();
      expect(drilled).toEqual([1]);
    });

    it('applies the query to a drilled level too, not just the root level', () => {
      const matching = node({ id: 21, name: 'مطابق', parentId: 20, depth: 1 });
      const other = node({ id: 22, name: 'غير مطابق', parentId: 20, depth: 1 });
      const parent = node({ id: 20, name: 'أب', liveChildCount: 2, children: [matching, other] });
      const byId = new Map<number, AbwabNode>([
        [20, parent],
        [21, matching],
        [22, other],
      ]);

      const root = render({
        roots: [parent],
        byId,
        cardId: 20,
        isFiltering: true,
        visibleIds: new Set([20, 21]),
      }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-card-21"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-card-22"]')).toBeNull();
    });
  });
});

// D08: the card grid is bounded by the shared doors primitive (14–20rem, at most four columns).
// A local `repeat(auto-fill, minmax(...))` is exactly the second value truth Phase 1 removed.
describe('AbwabCardsComponent — the grid is the bounded shared primitive', () => {
  it('composes qd-grid--doors instead of declaring its own track sizes', () => {
    const fixture = render();
    const grid = (fixture.nativeElement as HTMLElement).querySelector('.abwab-cards__grid')!;

    expect(grid.classList.contains('qd-grid')).toBe(true);
    expect(grid.classList.contains('qd-grid--doors')).toBe(true);

    const local = Array.from(document.styleSheets)
      .flatMap((sheet) => {
        try {
          return Array.from(sheet.cssRules);
        } catch {
          return [];
        }
      })
      .filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule)
      .filter(
        (rule) =>
          rule.selectorText?.includes('abwab-cards__grid') && rule.style.gridTemplateColumns !== '',
      );
    expect(local).toHaveLength(0);
  });
});
