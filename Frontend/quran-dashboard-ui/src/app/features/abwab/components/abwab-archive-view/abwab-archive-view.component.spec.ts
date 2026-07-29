import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabArchiveViewComponent } from './abwab-archive-view.component';
import { AbwabNode } from '../../models/abwab.models';

function node(overrides: Partial<AbwabNode> & { id: number; name: string }): AbwabNode {
  return {
    description: null,
    representativeAyahText: null,
    aliases: [],
    sectionId: null,
    parentId: null,
    orderValue: overrides.id,
    version: 1,
    isArchived: true,
    depth: 0,
    liveChildCount: 0,
    children: [],
    ...overrides,
  };
}

// root-archived(1, A-live) -> child-archived(2, A-arch)
const CHILD = node({ id: 2, name: 'فرع مؤرشف', parentId: 1, depth: 1 });
const ROOT = node({ id: 1, name: 'باب مؤرشف', depth: 0, children: [CHILD] });
const ROOTS = [ROOT];

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabArchiveViewComponent] });
  const fixture = TestBed.createComponent(AbwabArchiveViewComponent);
  fixture.componentRef.setInput('roots', ROOTS);
  fixture.componentRef.setInput('ariaLabel', 'شجرة الأبواب المؤرشفة');
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabArchiveViewComponent', () => {
  describe('M20 — renders archived doors in their hierarchy', () => {
    it('renders role="tree" with a treeitem row per archived door, nested by aria-level', () => {
      const root = render().nativeElement as HTMLElement;
      expect(root.querySelector('[role="tree"]')).toBeTruthy();
      const rootRow = root.querySelector('[data-testid="abwab-archive-row-1"]');
      expect(rootRow?.getAttribute('role')).toBe('treeitem');
      expect(rootRow?.getAttribute('aria-level')).toBe('1');
    });

    it('expanding the branch reveals the nested archived child', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-archive-row-2"]')).toBeNull();

      (root.querySelector('[data-testid="abwab-archive-chevron-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-archive-row-2"]')).toBeTruthy();
    });

    it('never renders a child-count badge (every archived child count is a meaningless 0, R12-adjacent)', () => {
      const root = render().nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-archive-row-1"]')?.textContent).not.toContain('0');
    });
  });

  describe('M21 — a door whose parent is archived shows a disabled restore with the parent-first hint', () => {
    it('enables restore on a root-level archived door (A-live)', () => {
      const root = render().nativeElement as HTMLElement;
      const restoreBtn = root.querySelector<HTMLButtonElement>('[data-testid="abwab-archive-restore-1"]');
      expect(restoreBtn?.disabled).toBe(false);
      expect(root.querySelector('[data-testid="abwab-archive-restore-hint-1"]')).toBeNull();
    });

    it('disables restore with the hint on a nested archived door (A-arch)', () => {
      const fixture = render();
      (fixture.nativeElement.querySelector('[data-testid="abwab-archive-chevron-1"]') as HTMLElement).click();
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      const restoreBtn = root.querySelector<HTMLButtonElement>('[data-testid="abwab-archive-restore-2"]');
      expect(restoreBtn?.disabled).toBe(true);
      expect(root.querySelector('[data-testid="abwab-archive-restore-hint-2"]')?.textContent?.trim()).toBe(
        'استرجع الأب أولًا',
      );
    });
  });

  describe('M20 — keyboard focus (roving), within the archive tree', () => {
    it('leaves exactly one tabbable row, defaulting to the first', () => {
      const root = render().nativeElement as HTMLElement;
      const rows = Array.from(root.querySelectorAll('[role="treeitem"]'));
      const tabbable = rows.filter((row) => row.getAttribute('tabindex') === '0');
      expect(tabbable).toHaveLength(1);
      expect(tabbable[0].getAttribute('data-testid')).toBe('abwab-archive-row-1');
    });

    it('ArrowDown moves the roving row to the next visible row', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const rootRow = root.querySelector('[data-testid="abwab-archive-row-1"]') as HTMLElement;

      rootRow.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
      fixture.detectChanges();

      // Row 1 is collapsed by default, so ArrowDown has nothing else to move to yet —
      // expand it first, then it should be able to reach the child.
      (root.querySelector('[data-testid="abwab-archive-chevron-1"]') as HTMLElement).click();
      fixture.detectChanges();
      const afterExpand = root.querySelector('[data-testid="abwab-archive-row-1"]') as HTMLElement;
      afterExpand.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
      fixture.detectChanges();

      const tabbable = root.querySelector('[tabindex="0"]');
      expect(tabbable?.getAttribute('data-testid')).toBe('abwab-archive-row-2');
    });
  });

  describe('M22 — restore is the only action offered on an archived door', () => {
    it('renders no edit/add-child/move/archive/bulk controls anywhere in the archive view', () => {
      const root = render().nativeElement as HTMLElement;
      expect(root.querySelector('input[type="checkbox"]')).toBeNull();
      expect(root.querySelector('[data-testid^="abwab-archive-edit-"]')).toBeNull();
      expect(root.querySelector('[data-testid^="abwab-archive-add-child-"]')).toBeNull();
      expect(root.querySelector('[data-testid^="abwab-archive-move-"]')).toBeNull();
    });

    it('emits restoreRequested with the door id when its restore button is enabled and clicked', () => {
      const fixture = render();
      const requested: number[] = [];
      fixture.componentInstance.restoreRequested.subscribe((id) => requested.push(id));

      (fixture.nativeElement.querySelector('[data-testid="abwab-archive-restore-1"]') as HTMLElement).click();

      expect(requested).toEqual([1]);
    });
  });
});
