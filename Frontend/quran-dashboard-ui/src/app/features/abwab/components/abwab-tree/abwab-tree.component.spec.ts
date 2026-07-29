import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabTreeComponent, AbwabTreeMenuRequest } from './abwab-tree.component';
import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { buildAbwabTreeSnapshot } from '../../state/abwab-tree.builder';

function door(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    aliases: [],
    description: null,
    directChildCount: 0,
    isArchived: false,
    orderValue: overrides.id,
    parentId: null,
    representativeAyahText: null,
    sectionId: null,
    version: 1,
    ...overrides,
  };
}

function tree(doors: AbwabTreeDoorDto[]) {
  return buildAbwabTreeSnapshot({ doors, sections: [], version: 'v1' } as AbwabTreeDto);
}

const SAMPLE = tree([
  door({ id: 1, name: 'جذر-1', orderValue: 1 }),
  door({ id: 2, name: 'ابن', parentId: 1, orderValue: 1 }),
  door({ id: 3, name: 'جذر-2', orderValue: 2 }),
]);

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabTreeComponent] });
  const fixture = TestBed.createComponent(AbwabTreeComponent);
  fixture.componentRef.setInput('roots', SAMPLE.liveRoots);
  fixture.componentRef.setInput('ariaLabel', 'شجرة الأبواب');
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabTreeComponent', () => {
  it('M5 — renders role="tree"/treeitem with aria-level, aria-expanded (branches only), aria-selected', () => {
    const fixture = render({ selectedId: 1 });
    const root = fixture.nativeElement as HTMLElement;

    const treeEl = root.querySelector('[role="tree"]');
    expect(treeEl?.getAttribute('aria-label')).toBe('شجرة الأبواب');

    const rootRow = root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement;
    expect(rootRow.getAttribute('role')).toBe('treeitem');
    expect(rootRow.getAttribute('aria-level')).toBe('1');
    expect(rootRow.getAttribute('aria-expanded')).toBe('false'); // has a child, collapsed by default
    expect(rootRow.getAttribute('aria-selected')).toBe('true');

    const leafRow = root.querySelector('[data-testid="abwab-tree-row-3"]') as HTMLElement;
    expect(leafRow.getAttribute('aria-expanded')).toBeNull(); // leaves never carry aria-expanded
    expect(leafRow.getAttribute('aria-selected')).toBe('false');
  });

  it('M6 — roving tabindex leaves exactly one tabbable row', () => {
    const fixture = render({ selectedId: 3 });
    const root = fixture.nativeElement as HTMLElement;

    const rows = Array.from(root.querySelectorAll('[role="treeitem"]'));
    const tabbable = rows.filter((row) => row.getAttribute('tabindex') === '0');
    expect(tabbable).toHaveLength(1);
    expect(tabbable[0].getAttribute('data-testid')).toBe('abwab-tree-row-3');

    const others = rows.filter((row) => row !== tabbable[0]);
    expect(others.every((row) => row.getAttribute('tabindex') === '-1')).toBe(true);
  });

  it('M9 — Enter selects the focused row; dblclick expands as an extra affordance, not the only path', () => {
    const fixture = render();
    const selected: number[] = [];
    fixture.componentInstance.selected.subscribe((id: number) => selected.push(id));

    const rootRow = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="abwab-tree-row-1"]',
    ) as HTMLElement;
    rootRow.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    expect(selected).toEqual([1]);

    // dblclick expands the branch (an extra affordance) without being required for expand —
    // the chevron already covers that; this just proves dblclick still works.
    expect(rootRow.getAttribute('aria-expanded')).toBe('false');
    rootRow.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    fixture.detectChanges();
    expect(rootRow.getAttribute('aria-expanded')).toBe('true');
  });

  it('the chevron toggles expand/collapse without selecting the row', () => {
    const fixture = render();
    const selected: number[] = [];
    fixture.componentInstance.selected.subscribe((id: number) => selected.push(id));

    const chevron = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="abwab-tree-chevron-1"]',
    ) as HTMLElement;
    chevron.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    const rootRow = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-tree-row-1"]');
    expect(rootRow?.getAttribute('aria-expanded')).toBe('true');
    expect(selected).toEqual([]);
  });

  describe('T511 — right-click opens the row menu (the mouse path; ContextMenu/Shift+F10 already covers keyboard)', () => {
    it('selects the row and emits menuRequested at the pointer, preventing the native context menu', () => {
      const fixture = render();
      const selected: number[] = [];
      const menuRequested: AbwabTreeMenuRequest[] = [];
      fixture.componentInstance.selected.subscribe((id: number) => selected.push(id));
      fixture.componentInstance.menuRequested.subscribe((r: AbwabTreeMenuRequest) => menuRequested.push(r));

      const rootRow = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="abwab-tree-row-1"]',
      ) as HTMLElement;
      const event = new MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 120, clientY: 40 });
      rootRow.dispatchEvent(event);

      expect(event.defaultPrevented).toBe(true);
      expect(selected).toEqual([1]);
      expect(menuRequested).toEqual([{ id: 1, x: 120, y: 40 }]);
    });

    it('does nothing in bulk mode (bulk rows have no context menu)', () => {
      const fixture = render({ bulkMode: true });
      const menuRequested: unknown[] = [];
      fixture.componentInstance.menuRequested.subscribe((r: unknown) => menuRequested.push(r));

      const rootRow = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="abwab-tree-row-1"]',
      ) as HTMLElement;
      rootRow.dispatchEvent(new MouseEvent('contextmenu', { bubbles: true, cancelable: true }));

      expect(menuRequested).toEqual([]);
    });

    it('the keyboard path anchors the menu to the focused row, not the viewport origin', () => {
      const fixture = render();
      const menuRequested: AbwabTreeMenuRequest[] = [];
      fixture.componentInstance.menuRequested.subscribe((r: AbwabTreeMenuRequest) => menuRequested.push(r));

      const rootRow = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="abwab-tree-row-1"]',
      ) as HTMLElement;
      rootRow.getBoundingClientRect = () => ({ left: 200, bottom: 88 }) as DOMRect;
      rootRow.dispatchEvent(new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true }));

      expect(menuRequested).toEqual([{ id: 1, x: 200, y: 88 }]);
    });
  });

  describe('the design contract’s row actions (abwab-tree-concept.html:114, 436-443)', () => {
    it('renders ＋ and ⋯ on every row, each with an Arabic aria-label naming the door', () => {
      const root = render().nativeElement as HTMLElement;

      const add = root.querySelector('[data-testid="abwab-tree-add-child-1"]') as HTMLButtonElement;
      const more = root.querySelector('[data-testid="abwab-tree-more-1"]') as HTMLButtonElement;

      expect(add.getAttribute('aria-label')).toBe('إضافة باب فرعي تحت «جذر-1»');
      expect(more.getAttribute('aria-label')).toBe('عمليات «جذر-1»');
    });

    it('＋ selects the row and emits addChildRequested, without also toggling expand', () => {
      const fixture = render();
      const selected: number[] = [];
      const addChild: number[] = [];
      fixture.componentInstance.selected.subscribe((id: number) => selected.push(id));
      fixture.componentInstance.addChildRequested.subscribe((id: number) => addChild.push(id));

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-add-child-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      expect(selected).toEqual([1]);
      expect(addChild).toEqual([1]);
      expect(root.querySelector('[data-testid="abwab-tree-row-1"]')?.getAttribute('aria-expanded')).toBe('false');
    });

    it('⋯ is a second mouse path to the row menu, without right-click', () => {
      const fixture = render();
      const menuRequested: AbwabTreeMenuRequest[] = [];
      fixture.componentInstance.menuRequested.subscribe((r: AbwabTreeMenuRequest) => menuRequested.push(r));

      (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="abwab-tree-more-1"]')!
        .dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: 70, clientY: 30 }));

      expect(menuRequested).toEqual([{ id: 1, x: 70, y: 30 }]);
    });

    it('hides both actions in bulk mode (rows carry checkboxes, not per-row operations)', () => {
      const root = render({ bulkMode: true }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-add-child-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-more-1"]')).toBeNull();
    });

    it('keeps the roving-tabindex invariant: the actions are never extra tab stops', () => {
      const root = render().nativeElement as HTMLElement;

      const actionStops = Array.from(root.querySelectorAll('.abwab-tree__act')).filter(
        (el) => el.getAttribute('tabindex') !== '-1',
      );
      expect(actionStops).toHaveLength(0);
      expect(root.querySelectorAll('[data-testid^="abwab-tree-row-"][tabindex="0"]')).toHaveLength(1);
    });
  });

  describe('M29 — inline order editing commits on Enter and reverts on Escape', () => {
    it('commits a new position on Enter', () => {
      const fixture = render();
      const committed: Array<{ id: number; position: number }> = [];
      fixture.componentInstance.orderCommitted.subscribe((event: { id: number; position: number }) =>
        committed.push(event),
      );

      const root = fixture.nativeElement as HTMLElement;
      const numberEl = root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement;
      numberEl.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();

      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      expect(input).toBeTruthy();
      input.value = '5';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      fixture.detectChanges();

      expect(committed).toEqual([{ id: 1, position: 5 }]);
      expect(root.querySelector('[data-testid="abwab-tree-order-input-1"]')).toBeNull();
    });

    it('reverts without emitting on Escape', () => {
      const fixture = render();
      const committed: unknown[] = [];
      fixture.componentInstance.orderCommitted.subscribe((event: unknown) => committed.push(event));

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      input.value = '9';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();

      expect(committed).toHaveLength(0);
      expect(root.querySelector('[data-testid="abwab-tree-order-input-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.textContent?.trim()).toBe('1');
    });
  });
});
