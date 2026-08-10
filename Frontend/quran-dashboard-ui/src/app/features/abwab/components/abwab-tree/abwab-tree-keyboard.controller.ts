import {
  QdHierarchyDirection,
  QdHierarchyRow,
  flattenQdHierarchyRows,
  isQdNativeActivationKey,
  resolveQdHierarchyIntent,
} from '../../../../shared/ui/hierarchy/hierarchy-keyboard.directive';
import { AbwabNode } from '../../models/abwab.models';

export type AbwabTreeRow = QdHierarchyRow;

export function flattenVisibleAbwabRows(
  roots: readonly AbwabNode[],
  expandedIds: ReadonlySet<number>,
): AbwabTreeRow[] {
  return flattenQdHierarchyRows(roots, (node) => node.children, expandedIds);
}

export type AbwabTreeDirection = QdHierarchyDirection;

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

export function resolveAbwabTreeKeyboardIntent(
  input: AbwabTreeKeyboardInput,
): AbwabTreeKeyboardIntent {
  const { key, visibleRows, focusedId, direction, bulkMode, shiftKey } = input;
  const row = visibleRows.find((candidate) => candidate.id === focusedId);
  if (!row) {
    return NONE;
  }

  switch (key) {
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
      return resolveQdHierarchyIntent({ key, rows: visibleRows, focusedId, direction });
  }
}

export function isNativeButtonActivation(key: string): boolean {
  return isQdNativeActivationKey(key);
}
