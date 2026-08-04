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

    it('still opens the menu from the keyboard, anchored on the row rather than the viewport origin', () => {
      const fixture = render();
      const requests: Array<{ nodeId: number; x: number; y: number }> = [];
      fixture.componentInstance.menuRequested.subscribe(
        (request: { nodeId: number; x: number; y: number }) => requests.push(request),
      );

      const root = fixture.nativeElement as HTMLElement;
      const row = root.querySelector('[data-testid="abwab-template-tree-row-2"]') as HTMLElement;
      row.getBoundingClientRect = () => ({ left: 120, right: 480, bottom: 64 }) as DOMRect;
      row.dispatchEvent(new KeyboardEvent('keydown', { key: 'ContextMenu', bubbles: true }));

      expect(requests).toEqual([{ nodeId: 2, x: 120, y: 64 }]);
    });
  });
});
