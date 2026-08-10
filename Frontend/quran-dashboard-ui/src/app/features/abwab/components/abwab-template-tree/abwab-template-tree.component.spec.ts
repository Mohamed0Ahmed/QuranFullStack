import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabTemplateTreeComponent } from './abwab-template-tree.component';
import { AbwabTemplateNodeDto } from '../../../../core/api/generated/models/abwab-template-node-dto';
import { buildAbwabTemplateTree } from '../../models/abwab-templates.models';

function node(
  overrides: Partial<AbwabTemplateNodeDto> & { id: number; name: string },
): AbwabTemplateNodeDto {
  return {
    aliases: [],
    description: null,
    orderValue: overrides.id,
    parentNodeId: null,
    representativeAyahText: null,
    ...overrides,
  };
}

// Only non-root nodes render the order chip — the root renders the ◆ marker instead — so every
// order test below drives node 2, a child.
const SAMPLE = buildAbwabTemplateTree({
  id: 7,
  name: 'قالب الأبواب',
  nodes: [
    node({ id: 1, name: 'الجذر', orderValue: 1 }),
    node({ id: 2, name: 'العلم بالله', parentNodeId: 1, orderValue: 1 }),
    node({ id: 3, name: 'الرسول', parentNodeId: 1, orderValue: 2 }),
  ],
});

function render() {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabTemplateTreeComponent] });
  const fixture = TestBed.createComponent(AbwabTemplateTreeComponent);
  fixture.componentRef.setInput('root', SAMPLE.root);
  fixture.componentRef.setInput('ariaLabel', 'شجرة القالب');
  fixture.componentRef.setInput('canCreateNode', true);
  fixture.componentRef.setInput('canReorderNode', true);
  fixture.componentRef.setInput('canShowRootContextMenu', true);
  fixture.componentRef.setInput('canShowNodeContextMenu', true);
  fixture.detectChanges();
  return fixture;
}

function openOrderEditor(fixture: ReturnType<typeof render>, nodeId: number): HTMLInputElement {
  const root = fixture.nativeElement as HTMLElement;
  (root.querySelector(`[data-testid="abwab-template-tree-order-${nodeId}"]`) as HTMLElement).dispatchEvent(
    new MouseEvent('click', { bubbles: true }),
  );
  fixture.detectChanges();
  return root.querySelector(`[data-testid="abwab-template-tree-order-input-${nodeId}"]`) as HTMLInputElement;
}

describe('AbwabTemplateTreeComponent', () => {
  describe('Phase 9 permission-aware controls', () => {
    it('renders a read-only tree and rejects plus, context-menu, quick-add, and reorder dispatches', () => {
      const fixture = render();
      fixture.componentRef.setInput('canCreateNode', false);
      fixture.componentRef.setInput('canReorderNode', false);
      fixture.componentRef.setInput('canShowRootContextMenu', false);
      fixture.componentRef.setInput('canShowNodeContextMenu', false);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;
      const added: number[] = [];
      const menus: unknown[] = [];
      const reordered: unknown[] = [];
      const quickAdds: string[] = [];
      fixture.componentInstance.addChildRequested.subscribe((id) => added.push(id));
      fixture.componentInstance.menuRequested.subscribe((event) => menus.push(event));
      fixture.componentInstance.orderCommitted.subscribe((event) => reordered.push(event));
      fixture.componentInstance.quickAddRequested.subscribe((name) => quickAdds.push(name));

      expect(root.querySelector('[data-testid="abwab-template-tree-add-child-2"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-template-tree-more-2"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-template-tree-quick-add"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-template-tree-order-2"]')?.tagName).toBe('SPAN');
      expect(root.querySelector('[data-testid="abwab-template-tree-row-2"]')?.getAttribute('tabindex')).toBeNull();

      const chevron = root.querySelector('[data-testid="abwab-template-tree-chevron-1"]') as HTMLButtonElement;
      chevron.focus();
      expect(document.activeElement).toBe(chevron);
      chevron.dispatchEvent(new KeyboardEvent('keydown', { key: 'ContextMenu', bubbles: true }));
      chevron.dispatchEvent(new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true }));

      const internals = fixture.componentInstance as unknown as {
        onAddChildClick(nodeId: number): void;
        onMoreClick(event: MouseEvent, nodeId: number): void;
        onOrderClick(node: { id: number; parentNodeId: number | null }): void;
        onQuickAddEnter(event: Event): void;
      };
      internals.onAddChildClick(2);
      internals.onMoreClick(new MouseEvent('click'), 2);
      internals.onOrderClick({ id: 2, parentNodeId: 1 });
      internals.onQuickAddEnter(new Event('submit'));

      expect(added).toEqual([]);
      expect(menus).toEqual([]);
      expect(reordered).toEqual([]);
      expect(quickAdds).toEqual([]);
    });
  });

  describe('inline order editor — Enter is the only commit', () => {
    it('commits a new position on Enter', () => {
      const fixture = render();
      const committed: Array<{ nodeId: number; position: number }> = [];
      fixture.componentInstance.orderCommitted.subscribe((event: { nodeId: number; position: number }) =>
        committed.push(event),
      );

      const input = openOrderEditor(fixture, 2);
      expect(input).toBeTruthy();
      input.value = '5';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      fixture.detectChanges();

      expect(committed).toEqual([{ nodeId: 2, position: 5 }]);
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-template-tree-order-input-2"]')).toBeNull();
    });

    it('reverts without emitting on Escape', () => {
      const fixture = render();
      const committed: unknown[] = [];
      fixture.componentInstance.orderCommitted.subscribe((event: unknown) => committed.push(event));

      const input = openOrderEditor(fixture, 2);
      input.value = '9';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();

      expect(committed).toHaveLength(0);
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-template-tree-order-input-2"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-template-tree-order-2"]')?.textContent?.trim()).toBe('1');
    });

    it('cancels without emitting when the input loses focus after typing', () => {
      const fixture = render();
      const committed: unknown[] = [];
      fixture.componentInstance.orderCommitted.subscribe((event: unknown) => committed.push(event));

      const input = openOrderEditor(fixture, 2);
      input.value = '99';
      input.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(committed).toHaveLength(0);
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-template-tree-order-input-2"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-template-tree-order-2"]')?.textContent?.trim()).toBe('1');
    });

    // The real browser sequence: Enter commits, the input unmounts under the focused element, and a
    // blur fires on the way out. Exactly one emission, and no cancel undoing it.
    it('emits exactly once when the blur follows an Enter commit', () => {
      const fixture = render();
      const committed: unknown[] = [];
      fixture.componentInstance.orderCommitted.subscribe((event: unknown) => committed.push(event));

      const input = openOrderEditor(fixture, 2);
      input.value = '3';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      input.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(committed).toEqual([{ nodeId: 2, position: 3 }]);
    });
  });

  // F-58: the chip is the workshop's only reorder affordance, so it has to be a real button —
  // a native one already activates on Enter and Space, which is the whole keyboard path.
  describe('F-58 — the inline order chip is a real button with a keyboard path', () => {
    it('renders a <button> with an Arabic accessible name naming the node and its order', () => {
      const root = render().nativeElement as HTMLElement;

      const chip = root.querySelector('[data-testid="abwab-template-tree-order-2"]') as HTMLElement;
      expect(chip.tagName).toBe('BUTTON');
      expect(chip.getAttribute('type')).toBe('button');
      expect(chip.getAttribute('aria-label')).toBe('تعديل ترتيب «العلم بالله» — الترتيب الحالي 1');
    });

    it('opens the existing inline editor on activation and puts focus in it', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-template-tree-order-2"]') as HTMLButtonElement).click();
      fixture.detectChanges();
      await new Promise((resolve) => setTimeout(resolve));

      const input = root.querySelector('[data-testid="abwab-template-tree-order-input-2"]') as HTMLInputElement;
      expect(input).toBeTruthy();
      expect(document.activeElement).toBe(input);
    });

    it('returns focus to the chip when the editor closes on a key, so the path does not dead-end', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const input = openOrderEditor(fixture, 2);
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();
      await new Promise((resolve) => setTimeout(resolve));

      expect(document.activeElement).toBe(root.querySelector('[data-testid="abwab-template-tree-order-2"]'));
    });
  });

  // F-68: a keyboard activation of `⋯` reports clientX/clientY 0, which the shared menu clamps
  // to the viewport corner. The doors tree already keeps its `⋯` out of the tab order so the
  // only keyboard route is the row-anchored ContextMenu/Shift+F10 one; copy that.
  describe('F-68 — `⋯` is a mouse path only, so no keyboard activation can anchor at 0,0', () => {
    it('keeps `⋯` out of the tab order', () => {
      const root = render().nativeElement as HTMLElement;

      const more = [...root.querySelectorAll('[data-testid^="abwab-template-tree-more-"]')];
      expect(more.length).toBeGreaterThan(0);
      expect(more.every((el) => el.getAttribute('tabindex') === '-1')).toBe(true);
    });

    it('opens the exact-permission node menu from its focused row, anchored away from the viewport origin', () => {
      const fixture = render();
      fixture.componentRef.setInput('canCreateNode', false);
      fixture.componentRef.setInput('canReorderNode', false);
      fixture.componentRef.setInput('canShowRootContextMenu', false);
      fixture.componentRef.setInput('canShowNodeContextMenu', true);
      fixture.detectChanges();
      const requests: Array<{ nodeId: number; x: number; y: number }> = [];
      fixture.componentInstance.menuRequested.subscribe(
        (request: { nodeId: number; x: number; y: number }) => requests.push(request),
      );

      const root = fixture.nativeElement as HTMLElement;
      const row = root.querySelector('[data-testid="abwab-template-tree-row-2"]') as HTMLElement;
      row.getBoundingClientRect = () => ({ left: 120, right: 480, bottom: 64 }) as DOMRect;
      expect(row.getAttribute('tabindex')).toBe('0');
      row.focus();
      expect(document.activeElement).toBe(row);
      row.dispatchEvent(new KeyboardEvent('keydown', { key: 'ContextMenu', bubbles: true }));
      row.dispatchEvent(new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true }));

      expect(requests).toEqual([
        { nodeId: 2, x: 120, y: 64 },
        { nodeId: 2, x: 120, y: 64 },
      ]);
    });
  });
});

// Phase 8 / D46: the workshop's row actions were hover-revealed too. The hierarchy here is a
// deliberate `role="list"` (G20), but the row-action rule is the same one the doors tree follows.
describe('AbwabTemplateTreeComponent — row actions are never hover-only', () => {
  it('renders the add-child and menu actions with shared action geometry and no visibility gate', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    const add = root.querySelector('[data-testid="abwab-template-tree-add-child-1"]')!;
    expect(add.classList.contains('qd-action')).toBe(true);
    // Same `＋`/`⋯` geometry as the doors tree: two boxes expanded in both axes 4px apart would
    // overlap by 20px and eat 8px of each other's face, so the pair grows in the block axis only.
    expect(add.classList.contains('qd-hit-target')).toBe(false);
    expect(root.querySelector('[data-testid="abwab-template-tree-chevron-1"]')!.classList.contains('qd-hit-target')).toBe(true);

    const rules = Array.from(document.styleSheets)
      .flatMap((sheet) => {
        try {
          return Array.from(sheet.cssRules);
        } catch {
          return [];
        }
      })
      .filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule);

    expect(
      rules.filter(
        (rule) =>
          rule.selectorText?.includes('abwab-template-tree__actions') &&
          rule.style.visibility === 'hidden',
      ),
    ).toHaveLength(0);

    // Dropping the utility must not shrink the pair to a 20x20 target — only the inline axis was
    // ever contested, so the block axis still reaches --qd-hit-target-min inside a 44px row.
    expect(
      rules.filter(
        (rule) =>
          rule.selectorText?.includes('abwab-template-tree__act') &&
          rule.style.getPropertyValue('min-block-size').includes('--qd-hit-target-min'),
      ).length,
    ).toBeGreaterThan(0);
  });

  it('keeps the hierarchy a list, not a false tree (G20)', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-template-tree"]')?.getAttribute('role')).toBe('list');
    expect(root.querySelector('[role="tree"]')).toBeNull();
  });
});
