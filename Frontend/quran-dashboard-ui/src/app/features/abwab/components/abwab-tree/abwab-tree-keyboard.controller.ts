import { AbwabNode } from '../../models/abwab.models';

export interface AbwabTreeRow {
  readonly id: number;
  readonly parentId: number | null;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

/** Flattens a tree into document order, skipping the children of any node not in
 * `expandedIds` (M8 — collapsed subtrees are skipped, never rendered-and-hidden). */
export function flattenVisibleAbwabRows(
  roots: readonly AbwabNode[],
  expandedIds: ReadonlySet<number>,
): AbwabTreeRow[] {
  const rows: AbwabTreeRow[] = [];

  function walk(node: AbwabNode, parentId: number | null): void {
    const hasChildren = node.children.length > 0;
    const isExpanded = hasChildren && expandedIds.has(node.id);
    rows.push({ id: node.id, parentId, hasChildren, isExpanded });
    if (isExpanded) {
      for (const child of node.children) {
        walk(child, node.id);
      }
    }
  }

  for (const root of roots) {
    walk(root, null);
  }
  return rows;
}

export type AbwabTreeDirection = 'ltr' | 'rtl';

export type AbwabTreeKeyboardIntent =
  | { readonly type: 'focus'; readonly id: number }
  | { readonly type: 'expand'; readonly id: number }
  | { readonly type: 'collapse'; readonly id: number }
  | { readonly type: 'select'; readonly id: number }
  | { readonly type: 'toggleBulk'; readonly id: number }
  | { readonly type: 'openMenu'; readonly id: number }
  | { readonly type: 'none' };

export interface AbwabTreeKeyboardInput {
  readonly key: string;
  readonly visibleRows: readonly AbwabTreeRow[];
  readonly focusedId: number;
  readonly direction: AbwabTreeDirection;
  readonly bulkMode: boolean;
  readonly shiftKey: boolean;
}

const NONE: AbwabTreeKeyboardIntent = { type: 'none' };

function moveBy(rows: readonly AbwabTreeRow[], index: number, delta: number): AbwabTreeKeyboardIntent {
  const nextIndex = index + delta;
  if (nextIndex < 0 || nextIndex >= rows.length) {
    return NONE;
  }
  return { type: 'focus', id: rows[nextIndex].id };
}

/** ArrowLeft in RTL / ArrowRight in LTR: expand a collapsed branch, or step into its
 * already-visible first child (the next row in document order). */
function intoChildOrExpand(row: AbwabTreeRow, rows: readonly AbwabTreeRow[], index: number): AbwabTreeKeyboardIntent {
  if (!row.hasChildren) {
    return NONE;
  }
  if (!row.isExpanded) {
    return { type: 'expand', id: row.id };
  }
  const next = rows[index + 1];
  return next ? { type: 'focus', id: next.id } : NONE;
}

/** ArrowRight in RTL / ArrowLeft in LTR: collapse an expanded branch, or step out to the
 * parent (the R11-mirrored `qd-tabs` precedent, `tabs.component.ts:82-93`). */
function outOfChildOrCollapse(row: AbwabTreeRow): AbwabTreeKeyboardIntent {
  if (row.hasChildren && row.isExpanded) {
    return { type: 'collapse', id: row.id };
  }
  return row.parentId !== null ? { type: 'focus', id: row.parentId } : NONE;
}

/** Pure key model for the Abwab tree (plan-slice-b.md T413). No DOM: direction is an
 * explicit input, never resolved from `closest('[dir]')` — that stays the presentational
 * component's job. */
export function resolveAbwabTreeKeyboardIntent(input: AbwabTreeKeyboardInput): AbwabTreeKeyboardIntent {
  const { key, visibleRows, focusedId, direction, bulkMode, shiftKey } = input;
  const index = visibleRows.findIndex((row) => row.id === focusedId);
  if (index === -1) {
    return NONE;
  }
  const row = visibleRows[index];

  switch (key) {
    case 'ArrowDown':
      return moveBy(visibleRows, index, 1);
    case 'ArrowUp':
      return moveBy(visibleRows, index, -1);
    case 'Home':
      return visibleRows.length > 0 ? { type: 'focus', id: visibleRows[0].id } : NONE;
    case 'End':
      return visibleRows.length > 0 ? { type: 'focus', id: visibleRows[visibleRows.length - 1].id } : NONE;
    case 'ArrowLeft':
      return direction === 'rtl' ? intoChildOrExpand(row, visibleRows, index) : outOfChildOrCollapse(row);
    case 'ArrowRight':
      return direction === 'rtl' ? outOfChildOrCollapse(row) : intoChildOrExpand(row, visibleRows, index);
    case 'Enter':
      return { type: 'select', id: row.id };
    case ' ':
    case 'Spacebar':
      return bulkMode ? { type: 'toggleBulk', id: row.id } : NONE;
    case 'ContextMenu':
      return { type: 'openMenu', id: row.id };
    case 'F10':
      return shiftKey ? { type: 'openMenu', id: row.id } : NONE;
    default:
      return NONE;
  }
}
