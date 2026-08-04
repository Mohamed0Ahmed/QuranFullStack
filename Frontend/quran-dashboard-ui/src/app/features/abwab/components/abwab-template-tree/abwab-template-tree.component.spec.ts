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
});
