import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabTreeComponent } from './abwab-tree.component';
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
