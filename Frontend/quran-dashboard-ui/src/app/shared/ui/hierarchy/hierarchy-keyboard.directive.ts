import { Directive, ElementRef, inject, input } from '@angular/core';

export interface QdHierarchyRow {
  readonly id: number;
  readonly parentId: number | null;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

export type QdHierarchyDirection = 'ltr' | 'rtl';

export type QdHierarchyIntent =
  | { readonly type: 'focus'; readonly id: number }
  | { readonly type: 'expand'; readonly id: number }
  | { readonly type: 'collapse'; readonly id: number }
  | { readonly type: 'none' };

export interface QdHierarchyKeyboardInput {
  readonly key: string;
  readonly rows: readonly QdHierarchyRow[];
  readonly focusedId: number;
  readonly direction: QdHierarchyDirection;
}

const NONE: QdHierarchyIntent = { type: 'none' };

export const QD_HIERARCHY_ROW_ATTRIBUTE = 'data-qd-hierarchy-id';

export function flattenQdHierarchyRows<TNode extends { readonly id: number }>(
  roots: readonly TNode[],
  childrenOf: (node: TNode) => readonly TNode[],
  expandedIds: ReadonlySet<number>,
): QdHierarchyRow[] {
  const rows: QdHierarchyRow[] = [];

  const walk = (node: TNode, parentId: number | null): void => {
    const children = childrenOf(node);
    const hasChildren = children.length > 0;
    const isExpanded = hasChildren && expandedIds.has(node.id);
    rows.push({ id: node.id, parentId, hasChildren, isExpanded });
    if (isExpanded) {
      for (const child of children) {
        walk(child, node.id);
      }
    }
  };

  for (const root of roots) {
    walk(root, null);
  }
  return rows;
}

function moveBy(
  rows: readonly QdHierarchyRow[],
  index: number,
  delta: number,
): QdHierarchyIntent {
  const nextIndex = index + delta;
  if (nextIndex < 0 || nextIndex >= rows.length) {
    return NONE;
  }
  return { type: 'focus', id: rows[nextIndex].id };
}

function intoChildOrExpand(
  row: QdHierarchyRow,
  rows: readonly QdHierarchyRow[],
  index: number,
): QdHierarchyIntent {
  if (!row.hasChildren) {
    return NONE;
  }
  if (!row.isExpanded) {
    return { type: 'expand', id: row.id };
  }
  const next = rows[index + 1];
  return next ? { type: 'focus', id: next.id } : NONE;
}

function outOfChildOrCollapse(row: QdHierarchyRow): QdHierarchyIntent {
  if (row.hasChildren && row.isExpanded) {
    return { type: 'collapse', id: row.id };
  }
  return row.parentId !== null ? { type: 'focus', id: row.parentId } : NONE;
}

export function resolveQdHierarchyIntent(input: QdHierarchyKeyboardInput): QdHierarchyIntent {
  const { key, rows, focusedId, direction } = input;
  const index = rows.findIndex((row) => row.id === focusedId);
  if (index === -1) {
    return NONE;
  }
  const row = rows[index];

  switch (key) {
    case 'ArrowDown':
      return moveBy(rows, index, 1);
    case 'ArrowUp':
      return moveBy(rows, index, -1);
    case 'Home':
      return rows.length > 0 ? { type: 'focus', id: rows[0].id } : NONE;
    case 'End':
      return rows.length > 0 ? { type: 'focus', id: rows[rows.length - 1].id } : NONE;
    case 'ArrowLeft':
      return direction === 'rtl' ? intoChildOrExpand(row, rows, index) : outOfChildOrCollapse(row);
    case 'ArrowRight':
      return direction === 'rtl' ? outOfChildOrCollapse(row) : intoChildOrExpand(row, rows, index);
    default:
      return NONE;
  }
}

export function isQdNativeActivationKey(key: string): boolean {
  return key === 'Enter' || key === ' ' || key === 'Spacebar';
}

@Directive({
  selector: '[qdHierarchyKeyboard]',
  standalone: true,
})
export class QdHierarchyKeyboardDirective {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly rows = input<readonly QdHierarchyRow[]>([], { alias: 'qdHierarchyRows' });

  direction(): QdHierarchyDirection {
    const host = this.elementRef.nativeElement.closest('[dir]');
    return host?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }

  resolve(key: string, focusedId: number): QdHierarchyIntent {
    return resolveQdHierarchyIntent({
      key,
      rows: this.rows(),
      focusedId,
      direction: this.direction(),
    });
  }

  rowElement(id: number): HTMLElement | null {
    return this.elementRef.nativeElement.querySelector<HTMLElement>(
      `[${QD_HIERARCHY_ROW_ATTRIBUTE}="${id}"]`,
    );
  }

  focusRow(id: number): void {
    queueMicrotask(() => this.rowElement(id)?.focus());
  }
}
