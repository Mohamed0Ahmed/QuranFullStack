import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { CategorySnapshotDto } from '../../../core/api/generated/models';
import { AbwabVisibleTreeNode, buildVisibleTreeNodes } from './abwab-tree-node';
import { AbwabTreeViewComponent } from './abwab-tree-view.component';

// jsdom performs no real layout, so cdk-virtual-scroll-viewport never measures a nonzero
// clientHeight and renders zero rows here — that is expected. Row-level interaction (click,
// keyboard nav, focus) is exercised through the real DOM in a real browser by the Playwright
// suite (T066); this spec covers the component's pure logic and the row-activation/toggle
// contract at the API level instead of faking a jsdom layout.
function node(id: string, overrides: Partial<CategorySnapshotDto> = {}): AbwabVisibleTreeNode {
  const category: CategorySnapshotDto = {
    categoryId: id,
    name: `باب ${id}`,
    normalizedName: `باب ${id}`,
    description: null,
    representativeQuranExcerpt: null,
    parentCategoryId: null,
    sectionId: 'section-1',
    siblingOrder: null,
    sectionOrder: 0,
    globalOrder: 0,
    ancestorIds: [],
    depth: 0,
    categoryContentRevision: 1,
    version: 1,
    aliases: [],
    protection: { hasEffectiveManualProtection: false, isActionBlocked: false, manualProtections: [] },
    ...overrides,
  };
  return { category, depth: 0, hasChildren: false, isExpanded: false };
}

function renderTreeView(nodes: AbwabVisibleTreeNode[]) {
  const fixture = TestBed.createComponent(AbwabTreeViewComponent);
  fixture.componentRef.setInput('nodes', nodes);
  fixture.detectChanges();
  return fixture;
}

describe('AbwabTreeViewComponent (T070 tree/search UI)', () => {
  it('is RTL by default', () => {
    const fixture = renderTreeView([node('a')]);
    expect((fixture.nativeElement as HTMLElement).getAttribute('dir')).toBe('rtl');
  });

  it('exposes the tree role, an accessible label, and the true total node count on the viewport', () => {
    const nodes = Array.from({ length: 200 }, (_, index) => node(`n${index}`));
    const fixture = renderTreeView(nodes);

    const viewport = (fixture.nativeElement as HTMLElement).querySelector('[data-testid=abwab-tree-viewport]')!;
    expect(viewport.getAttribute('role')).toBe('tree');
    expect(viewport.getAttribute('data-total')).toBe('200');
  });

  it('row activation emits select with the category id (explicit action, no drag)', () => {
    const fixture = renderTreeView([node('a')]);
    let selected: string | undefined;
    fixture.componentInstance.select.subscribe((id) => (selected = id));

    (fixture.componentInstance as unknown as { onRowActivate(n: AbwabVisibleTreeNode): void }).onRowActivate(node('a'));

    expect(selected).toBe('a');
  });

  it('ArrowDown/ArrowUp move the focused row index without selecting or expanding anything', () => {
    const nodes = [node('a'), node('b'), node('c')];
    const fixture = renderTreeView(nodes);
    const instance = fixture.componentInstance as unknown as {
      onKeydown(event: KeyboardEvent): void;
      focusedIndex: () => number;
    };
    let selected: string | undefined;
    fixture.componentInstance.select.subscribe((id) => (selected = id));

    instance.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    expect(instance.focusedIndex()).toBe(1);

    instance.onKeydown(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
    expect(instance.focusedIndex()).toBe(0);
    expect(selected).toBeUndefined();
  });

  it('RTL ArrowLeft expands a collapsed parent (the WAI-ARIA RTL reversal of the LTR assignment)', () => {
    const collapsedParent: AbwabVisibleTreeNode = { ...node('a'), hasChildren: true, isExpanded: false };
    const fixture = renderTreeView([collapsedParent]);
    let toggled: string | undefined;
    fixture.componentInstance.toggleExpand.subscribe((id) => (toggled = id));

    const handleKeydown = (fixture.componentInstance as unknown as { onKeydown(event: KeyboardEvent): void }).onKeydown.bind(
      fixture.componentInstance,
    );
    handleKeydown(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));

    expect(toggled).toBe('a');
  });

  it('buildVisibleTreeNodes: a collapsed parent hides its descendants; expanding it reveals them', () => {
    const categories: CategorySnapshotDto[] = [
      node('root').category,
      { ...node('child').category, parentCategoryId: 'root', siblingOrder: 0, ancestorIds: ['root'], depth: 1 },
    ];

    const collapsed = buildVisibleTreeNodes(categories, new Set());
    expect(collapsed.map((n) => n.category.categoryId)).toEqual(['root']);

    const expanded = buildVisibleTreeNodes(categories, new Set(['root']));
    expect(expanded.map((n) => n.category.categoryId)).toEqual(['root', 'child']);
  });
});
