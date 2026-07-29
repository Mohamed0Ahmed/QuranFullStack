import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabCardsComponent } from './abwab-cards.component';
import { AbwabNode } from '../../models/abwab.models';

function node(overrides: Partial<AbwabNode> & { id: number; name: string }): AbwabNode {
  return {
    description: null,
    representativeAyahText: null,
    aliases: [],
    sectionId: null,
    parentId: null,
    orderValue: overrides.id,
    globalOrderValue: overrides.id,
    version: 1,
    isArchived: false,
    depth: 0,
    liveChildCount: 0,
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
  });
});
