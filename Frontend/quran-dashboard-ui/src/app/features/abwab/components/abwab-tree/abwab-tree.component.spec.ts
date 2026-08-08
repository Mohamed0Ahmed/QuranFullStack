import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabTreeComponent, AbwabTreeMenuRequest } from './abwab-tree.component';
import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { buildAbwabTreeSnapshot, searchAbwabNodes } from '../../state/abwab-tree.builder';
import { ABWAB_LABELS } from '../../models/abwab.labels';

function door(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    aliases: [],
    description: null,
    directChildCount: 0,
    relationCount: 0,
    globalOrderValue: overrides.parentId == null ? overrides.id : null,
    isArchived: false,
    orderValue: overrides.id,
    parentId: null,
    representativeAyahText: null,
    sectionId: 1,
    sectionRetired: false,
    version: 1,
    ...overrides,
  };
}

function tree(doors: AbwabTreeDoorDto[]) {
  return buildAbwabTreeSnapshot({ doors, sections: [], version: 'v1' } as AbwabTreeDto);
}

const SAMPLE = tree([
  door({ id: 1, name: 'جذر-1', orderValue: 1, relationCount: 3 }),
  door({ id: 2, name: 'ابن', parentId: 1, orderValue: 1 }),
  door({ id: 3, name: 'جذر-2', orderValue: 2 }),
]);

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabTreeComponent] });
  const fixture = TestBed.createComponent(AbwabTreeComponent);
  fixture.componentRef.setInput('roots', SAMPLE.liveRoots);
  fixture.componentRef.setInput('ariaLabel', 'شجرة الأبواب');
  fixture.componentRef.setInput('canCreateDoor', true);
  fixture.componentRef.setInput('canReorderDoor', true);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabTreeComponent', () => {
  describe('Phase 9 permission-aware controls', () => {
    it('keeps the read tree and relations menu available while create and reorder dispatches are inert', () => {
      const fixture = render({ canCreateDoor: false, canReorderDoor: false });
      const root = fixture.nativeElement as HTMLElement;
      const added: number[] = [];
      const reordered: unknown[] = [];
      fixture.componentInstance.addChildRequested.subscribe((id) => added.push(id));
      fixture.componentInstance.orderCommitted.subscribe((event) => reordered.push(event));

      expect(root.querySelector('[data-testid="abwab-tree-add-child-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-more-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.tagName).toBe('SPAN');

      const internals = fixture.componentInstance as unknown as {
        onAddChildClick(event: Event, id: number): void;
        onOrderClick(event: Event, id: number): void;
        commitOrderEdit(id: number, target: EventTarget | null): void;
      };
      internals.onAddChildClick(new Event('click'), 1);
      internals.onOrderClick(new Event('click'), 1);
      internals.commitOrderEdit(1, { value: '2' } as HTMLInputElement);

      expect(added).toEqual([]);
      expect(reordered).toEqual([]);
    });
  });

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

  describe('M29 — inline order editing commits on Enter; blur and Escape both cancel', () => {
    it('commits a new position on Enter, tagged with the section scope by default', () => {
      const fixture = render();
      const committed: Array<{ id: number; position: number; scope: string }> = [];
      fixture.componentInstance.orderCommitted.subscribe((event: { id: number; position: number; scope: string }) =>
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

      expect(committed).toEqual([{ id: 1, position: 5, scope: 'section' }]);
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

    it('cancels without emitting when the input loses focus after typing', () => {
      const fixture = render();
      const committed: unknown[] = [];
      fixture.componentInstance.orderCommitted.subscribe((event: unknown) => committed.push(event));

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      input.value = '99';
      input.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(committed).toHaveLength(0);
      expect(root.querySelector('[data-testid="abwab-tree-order-input-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.textContent?.trim()).toBe('1');
    });

    // The real browser sequence: Enter commits, the input unmounts under the focused element,
    // and a blur fires on the way out. Exactly one emission, and no cancel undoing it.
    it('emits exactly once when the blur follows an Enter commit', () => {
      const fixture = render();
      const committed: unknown[] = [];
      fixture.componentInstance.orderCommitted.subscribe((event: unknown) => committed.push(event));

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      input.value = '5';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      input.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(committed).toEqual([{ id: 1, position: 5, scope: 'section' }]);
    });
  });

  describe('T403 — orderScope controls which order space depth-0 rows show/edit', () => {
    // Root 3 (جذر-2): orderValue 2, globalOrderValue 3 — deliberately different, so a test that
    // reads the wrong field is caught rather than passing by coincidence.
    it('renders globalOrderValue at depth 0 under orderScope=global, leaving depth-1 rows on orderValue', () => {
      const fixture = render({ orderScope: 'global' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-tree-order-3"]')?.textContent?.trim()).toBe('3');

      // Expand root 1 to reveal its depth-1 child row.
      (root.querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-tree-order-2"]')?.textContent?.trim()).toBe('1'); // depth 1, untouched
    });

    it('renders orderValue at depth 0 under orderScope=section (the default)', () => {
      const root = render({ orderScope: 'section' }).nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-tree-order-3"]')?.textContent?.trim()).toBe('2');
    });

    it('emits scope="global" for a depth-0 commit under orderScope=global', () => {
      const fixture = render({ orderScope: 'global' });
      const committed: Array<{ id: number; position: number; scope: string }> = [];
      fixture.componentInstance.orderCommitted.subscribe((event: { id: number; position: number; scope: string }) =>
        committed.push(event),
      );

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-order-3"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      const input = root.querySelector('[data-testid="abwab-tree-order-input-3"]') as HTMLInputElement;
      input.value = '7';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

      expect(committed).toEqual([{ id: 3, position: 7, scope: 'global' }]);
    });

    it('still emits scope="section" for a depth-1 row even under orderScope=global — globalOrderValue is NULL past the root', () => {
      const fixture = render({ orderScope: 'global' });
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      const committed: Array<{ id: number; position: number; scope: string }> = [];
      fixture.componentInstance.orderCommitted.subscribe((event: { id: number; position: number; scope: string }) =>
        committed.push(event),
      );

      (root.querySelector('[data-testid="abwab-tree-order-2"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      const input = root.querySelector('[data-testid="abwab-tree-order-input-2"]') as HTMLInputElement;
      input.value = '9';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

      expect(committed).toEqual([{ id: 2, position: 9, scope: 'section' }]);
    });
  });

  describe('audit item 13 — the relations flag is on every row and is a control', () => {
    it('renders on a row with no relations, dimmed, and names the count for a screen reader', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const empty = root.querySelector('[data-testid="abwab-tree-flag-rel-3"]') as HTMLElement;
      expect(empty).toBeTruthy();
      expect(empty.classList.contains('abwab-tree__flag--empty')).toBe(true);
      expect(empty.getAttribute('aria-label')).toBe(ABWAB_LABELS.rowRelationsAriaLabel('جذر-2', 0));
      expect(empty.getAttribute('aria-label')).toContain('لا علاقات');
    });

    it('renders undimmed with the count in its name on a row that has relations', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const flag = root.querySelector('[data-testid="abwab-tree-flag-rel-1"]') as HTMLElement;
      expect(flag.classList.contains('abwab-tree__flag--empty')).toBe(false);
      expect(flag.getAttribute('aria-label')).toBe(ABWAB_LABELS.rowRelationsAriaLabel('جذر-1', 3));
      expect(flag.getAttribute('aria-label')).toContain('3 علاقات');
    });

    it('emits relationsRequested with the row id, and does not select or open the row menu', () => {
      const fixture = render();
      const requested: number[] = [];
      const selected: number[] = [];
      const menus: AbwabTreeMenuRequest[] = [];
      fixture.componentInstance.relationsRequested.subscribe((id: number) => requested.push(id));
      fixture.componentInstance.selected.subscribe((id: number) => selected.push(id));
      fixture.componentInstance.menuRequested.subscribe((r: AbwabTreeMenuRequest) => menus.push(r));

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-flag-rel-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );

      expect(requested).toEqual([1]);
      // The page does the selecting, off `relationsRequested` — the tree must not also emit
      // `selected`, or the click would run the select-then-act sequence twice.
      expect(selected).toHaveLength(0);
      expect(menus).toHaveLength(0);
    });

    // F-87: in bulk mode the strip stops being a relations control and joins the row — a click
    // anywhere on a bulk row means "toggle this door", so the flag must toggle it exactly once
    // (it swallows the event rather than also letting the row handler run).
    it('toggles the row’s bulk selection in bulk mode instead of opening relations', () => {
      const fixture = render({ bulkMode: true });
      const requested: number[] = [];
      const toggled: number[] = [];
      fixture.componentInstance.relationsRequested.subscribe((id: number) => requested.push(id));
      fixture.componentInstance.bulkToggled.subscribe((id: number) => toggled.push(id));

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-flag-rel-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );

      expect(requested).toHaveLength(0);
      expect(toggled).toEqual([1]);
    });

    // F-88: the two states differed by hue only — the visible count is the non-chromatic
    // differentiator, so it has to render on both the empty and the populated flag.
    it('renders the relation count as text in both states, not colour alone', () => {
      const root = render().nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-relations-count-1"]')?.textContent?.trim()).toBe('3');
      expect(root.querySelector('[data-testid="abwab-tree-relations-count-3"]')?.textContent?.trim()).toBe('0');
      expect(
        (root.querySelector('[data-testid="abwab-tree-flag-rel-3"]') as HTMLElement).textContent,
      ).toContain('0');
    });

    it('adds no tab stop to the row', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const flags = [...root.querySelectorAll('[data-testid^="abwab-tree-flag-rel-"]')];
      expect(flags.length).toBeGreaterThan(0);
      expect(flags.every((el) => el.getAttribute('tabindex') === '-1')).toBe(true);
    });
  });

  describe('§17 — the bulk checkbox composes the shared class and names its door', () => {
    it('carries qd-checkbox and an aria-label naming the row’s door', () => {
      const fixture = render({ bulkMode: true });
      const root = fixture.nativeElement as HTMLElement;

      const checkbox = root.querySelector('[data-testid="abwab-tree-checkbox-1"]');
      expect(checkbox?.classList.contains('qd-checkbox')).toBe(true);
      expect(checkbox?.getAttribute('aria-label')).toBe('جذر-1');
    });

    // F-86: the row owns the roving tabindex, so no in-row control may be a second tab stop —
    // Space on the focused row already toggles it.
    it('is not a tab stop inside the roving row', () => {
      const root = render({ bulkMode: true }).nativeElement as HTMLElement;

      const checkboxes = [...root.querySelectorAll('[data-testid^="abwab-tree-checkbox-"]')];
      expect(checkboxes.length).toBeGreaterThan(0);
      expect(checkboxes.every((el) => el.getAttribute('tabindex') === '-1')).toBe(true);
    });
  });

  describe('F-58 — the inline order chip is a real button with a keyboard path', () => {
    it('renders a <button> with an Arabic accessible name naming the door and its order', () => {
      const root = render().nativeElement as HTMLElement;

      const chip = root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement;
      expect(chip.tagName).toBe('BUTTON');
      expect(chip.getAttribute('type')).toBe('button');
      expect(chip.getAttribute('aria-label')).toBe(ABWAB_LABELS.rowOrderEditAriaLabel('جذر-1', 1));
    });

    // A native button fires `click` on Enter and Space, so this is the keyboard path: no
    // key handler of our own, and none needed.
    it('opens the existing inline editor on activation and puts focus in it', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLButtonElement).click();
      fixture.detectChanges();
      await new Promise((resolve) => setTimeout(resolve));

      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      expect(input).toBeTruthy();
      expect(document.activeElement).toBe(input);
    });

    it('is reachable from the roving row only, so the tree keeps one tab stop per control', () => {
      const root = render({ selectedId: 1 }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.getAttribute('tabindex')).toBe('0');
      expect(root.querySelector('[data-testid="abwab-tree-order-3"]')?.getAttribute('tabindex')).toBe('-1');
    });

    // The row-level key model must not swallow the button's own activation.
    it('leaves the row key model alone: Enter on the chip does not also select the row', () => {
      const fixture = render();
      const selected: number[] = [];
      fixture.componentInstance.selected.subscribe((id: number) => selected.push(id));

      const root = fixture.nativeElement as HTMLElement;
      const chip = root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement;
      const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
      chip.dispatchEvent(event);

      expect(selected).toHaveLength(0);
      expect(event.defaultPrevented).toBe(false);
    });

    // Only the activation keys are withheld from the row model — everything else must keep
    // bubbling, or focus parked on an in-row control (after a mouse click, or after the row
    // menu returns focus to `⋯`) would kill arrow navigation.
    it('still lets ArrowDown from an in-row control move the roving row', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement).dispatchEvent(
        new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }),
      );
      fixture.detectChanges();

      expect(
        root.querySelector('[data-testid="abwab-tree-row-3"]')?.getAttribute('tabindex'),
      ).toBe('0');
    });

    it('returns focus to the chip when the editor closes on a key, so the path does not dead-end', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLButtonElement).click();
      fixture.detectChanges();

      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();
      await new Promise((resolve) => setTimeout(resolve));

      expect(document.activeElement).toBe(root.querySelector('[data-testid="abwab-tree-order-1"]'));
    });
  });

  describe('F-50 — the chevron is named, and inert on a leaf', () => {
    it('names the branch chevron in Arabic and flips the name with the expanded state', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const chevron = root.querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement;
      expect(chevron.getAttribute('aria-label')).toBe(ABWAB_LABELS.relationPickerExpandAriaLabel('جذر-1'));
      expect(chevron.getAttribute('aria-hidden')).toBeNull();

      chevron.click();
      fixture.detectChanges();

      expect(
        (root.querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement).getAttribute('aria-label'),
      ).toBe(ABWAB_LABELS.relationPickerCollapseAriaLabel('جذر-1'));
    });

    it('hides the empty leaf chevron from the accessible layer instead of leaving it unnamed', () => {
      const root = render().nativeElement as HTMLElement;

      const leaf = root.querySelector('[data-testid="abwab-tree-chevron-3"]') as HTMLElement;
      expect(leaf.getAttribute('aria-hidden')).toBe('true');
      expect(leaf.getAttribute('aria-label')).toBeNull();
      expect(leaf.getAttribute('tabindex')).toBe('-1');
    });
  });

  describe('the badge column header (slice J)', () => {
    it('renders the three labels outside the tree, hidden from the accessible layer', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const header = root.querySelector('[data-testid="abwab-tree-header"]')!;

      expect(header.getAttribute('aria-hidden')).toBe('true');
      // It must never enter the tree's ARIA subtree — a presentational row inside role="tree"
      // would be an unlabelled treeitem to a screen reader.
      expect(root.querySelector('[data-testid="abwab-tree"]')!.contains(header)).toBe(false);

      const labels = Array.from(header.querySelectorAll('.abwab-tree__header-cell')).map((el) =>
        el.textContent?.trim(),
      );
      expect(labels).toEqual([
        ABWAB_LABELS.rowHeaderDirect,
        ABWAB_LABELS.rowHeaderTotal,
        ABWAB_LABELS.rowHeaderDepth,
      ]);
    });

    it('drops the same two labels its badges drop below the tablet breakpoint', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const cells = Array.from(
        root.querySelectorAll('[data-testid="abwab-tree-header"] .abwab-tree__header-cell'),
      );

      expect(cells[0].classList.contains('abwab-tree__count--wide')).toBe(false);
      expect(cells[1].classList.contains('abwab-tree__count--wide')).toBe(true);
      expect(cells[2].classList.contains('abwab-tree__count--wide')).toBe(true);
    });

    // The visible labels were shortened to buy back the name column; the accessible layer is
    // where the meaning actually lives and it must not have moved with them.
    it('shortens only the visible labels — every badge keeps its full accessible name', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-count-1"]')?.getAttribute('aria-label')).toBe(
        ABWAB_LABELS.rowChildCountAriaLabel(1),
      );
      expect(
        root.querySelector('[data-testid="abwab-tree-descendants-1"]')?.getAttribute('aria-label'),
      ).toBe(ABWAB_LABELS.rowDescendantCountAriaLabel(1));
      expect(root.querySelector('[data-testid="abwab-tree-depth-1"]')?.getAttribute('aria-label')).toBe(
        ABWAB_LABELS.rowDepthAriaLabel(1),
      );

      // Each accessible name still carries the full word the visible label abbreviates.
      expect(ABWAB_LABELS.rowChildCountAriaLabel(1)).toContain('مباشرة');
      expect(ABWAB_LABELS.rowDescendantCountAriaLabel(1)).toContain('كل المستويات');
      expect(ABWAB_LABELS.rowDepthAriaLabel(1)).toContain('أعمق تفرّع');
    });

    it('shows the depth as a bare numeral now that a column names it', () => {
      expect(ABWAB_LABELS.rowDepthBadge(3)).toBe('3');
    });
  });

  describe('audit item 14 — the row’s three count badges', () => {
    it('renders all three on a branch row, reading the builder’s values', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-count-1"]')?.textContent?.trim()).toBe('1');
      expect(root.querySelector('[data-testid="abwab-tree-descendants-1"]')?.textContent?.trim()).toBe('1');
      expect(root.querySelector('[data-testid="abwab-tree-depth-1"]')?.textContent?.trim()).toBe(
        ABWAB_LABELS.rowDepthBadge(1),
      );
    });

    it('renders none of them on a leaf row, but keeps their cells so the columns hold', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-count-3"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-descendants-3"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-depth-3"]')).toBeNull();

      // Subgrid auto-placement is positional: a leaf that skipped these cells would put its
      // flags in the first count track and break the header's alignment for every row below it.
      const leaf = root.querySelector('[data-testid="abwab-tree-row-3"]')!;
      const cells = leaf.querySelectorAll('.abwab-tree__count-cell');
      expect(cells).toHaveLength(3);
      for (const cell of Array.from(cells)) {
        expect(cell.textContent?.trim()).toBe('');
      }
    });

    // Bare numerals on screen, so the accessible name is the only place the meaning lives —
    // and the two counts must not read as the same thing to a screen reader.
    it('names each badge distinctly, pinning "levels below this door" for the depth one', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-count-1"]')?.getAttribute('aria-label')).toBe(
        ABWAB_LABELS.rowChildCountAriaLabel(1),
      );
      expect(root.querySelector('[data-testid="abwab-tree-descendants-1"]')?.getAttribute('aria-label')).toBe(
        ABWAB_LABELS.rowDescendantCountAriaLabel(1),
      );
      expect(root.querySelector('[data-testid="abwab-tree-depth-1"]')?.getAttribute('aria-label')).toBe(
        ABWAB_LABELS.rowDepthAriaLabel(1),
      );
      expect(ABWAB_LABELS.rowChildCountAriaLabel(1)).not.toBe(ABWAB_LABELS.rowDescendantCountAriaLabel(1));
    });

    // The media query itself is untestable in jsdom; what a spec can pin is that exactly the
    // two badges this slice added carry the class the query drops, and the contract's own
    // direct-children count does not.
    it('marks only the two new badges as the ones that drop below the tablet breakpoint', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-count-1"]')?.classList.contains('abwab-tree__count--wide')).toBe(false);
      expect(root.querySelector('[data-testid="abwab-tree-descendants-1"]')?.classList.contains('abwab-tree__count--wide')).toBe(true);
      expect(root.querySelector('[data-testid="abwab-tree-depth-1"]')?.classList.contains('abwab-tree__count--wide')).toBe(true);
    });
  });

  // Phase 3 of the feature-034 follow-up (supersedes ux-slice-l's accumulation decision):
  // search expansion arrives on the derived `searchExpandedIds` input, replaced wholesale per
  // query and unioned with the manual set at render time, while `expandSeedIds` keeps the
  // reveal's merge-once semantics. Two branches so a stale one has somewhere to linger.
  describe('search-derived expansion is replaced wholesale per query', () => {
    const NESTED = tree([
      door({ id: 1, name: 'الرحمن', orderValue: 1 }),
      door({ id: 2, name: 'الرحيم', parentId: 1, orderValue: 1 }),
      door({ id: 3, name: 'الملك', orderValue: 2 }),
      door({ id: 4, name: 'المالك', parentId: 3, orderValue: 1 }),
    ]);

    const derivedFor = (query: string) => searchAbwabNodes(NESTED.liveRoots, query).autoExpandedIds;

    const rowsPresent = (fixture: ReturnType<typeof render>, ids: readonly number[]) =>
      ids.map((id) => !!(fixture.nativeElement as HTMLElement).querySelector(`[data-testid="abwab-tree-row-${id}"]`));

    it('narrowing «ال» → «الرح» closes the branch the broader query opened — no accumulation', () => {
      const fixture = render({ roots: NESTED.liveRoots, searchExpandedIds: derivedFor('ال') });
      expect(rowsPresent(fixture, [2, 4])).toEqual([true, true]);

      fixture.componentRef.setInput('searchExpandedIds', derivedFor('الرح'));
      fixture.detectChanges();

      expect(rowsPresent(fixture, [2, 4])).toEqual([true, false]);
    });

    it('a zero-match query closes every search-derived branch', () => {
      const fixture = render({ roots: NESTED.liveRoots, searchExpandedIds: derivedFor('ال') });
      expect(rowsPresent(fixture, [2, 4])).toEqual([true, true]);

      fixture.componentRef.setInput('searchExpandedIds', derivedFor('لا يوجد باب بهذا الاسم'));
      fixture.detectChanges();

      expect(rowsPresent(fixture, [2, 4])).toEqual([false, false]);
    });

    it('reveal seeds still merge into the manual state and stay collapsible', () => {
      const fixture = render({ roots: NESTED.liveRoots, expandSeedIds: new Set([1]) });
      expect(rowsPresent(fixture, [2])).toEqual([true]);

      // Merged, not derived: emptying the seed input does not close the branch it opened.
      fixture.componentRef.setInput('expandSeedIds', new Set<number>());
      fixture.detectChanges();
      expect(rowsPresent(fixture, [2])).toEqual([true]);

      (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="abwab-tree-chevron-1"]')!
        .dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();
      expect(rowsPresent(fixture, [2])).toEqual([false]);
    });
  });

  describe('the search match mark (ux-slice-l)', () => {
    const row = (fixture: ReturnType<typeof render>, id: number) =>
      (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="abwab-tree-row-${id}"]`)!;

    it('marks a matched row and leaves a non-matching one on screen, unmarked', () => {
      const fixture = render({ matchedIds: new Set([3]) });

      expect(row(fixture, 3).classList).toContain('abwab-tree__row--match');
      // Not "absent" — the tree stopped hiding non-matches; that is the point of the mark.
      expect(row(fixture, 1)).toBeTruthy();
      expect(row(fixture, 1).classList).not.toContain('abwab-tree__row--match');
    });

    it('a revealed match carries both marks, and the two live on different CSS properties', () => {
      const fixture = render({ matchedIds: new Set([3]), revealedId: 3 });

      expect(row(fixture, 3).classList).toContain('abwab-tree__row--match');
      expect(row(fixture, 3).classList).toContain('abwab-tree__row--revealed');

      // The composition rule, asserted at its source rather than by eye: the match mark claims
      // `box-shadow` and the reveal ring claims `outline`, so which one shows can never come
      // down to SCSS declaration order. If someone "unifies" them onto one property, this fails.
      const styles = Array.from(document.styleSheets)
        .flatMap((sheet) => {
          try {
            return Array.from(sheet.cssRules);
          } catch {
            return [];
          }
        })
        .filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule);
      const matchRule = styles.find((rule) => rule.selectorText?.includes('abwab-tree__row--match'));
      const revealRule = styles.find(
        (rule) => rule.selectorText?.includes('abwab-tree__row--revealed') && rule.style.outline !== '',
      );

      // Found-ness asserted first: without this the property checks below pass vacuously on
      // `undefined` if the stylesheet ever stops being reachable from the spec.
      expect(matchRule).toBeDefined();
      expect(revealRule).toBeDefined();
      expect(matchRule!.style.boxShadow).not.toBe('');
      expect(matchRule!.style.outline).toBe('');
      expect(revealRule!.style.boxShadow).toBe('');
    });
  });
});
