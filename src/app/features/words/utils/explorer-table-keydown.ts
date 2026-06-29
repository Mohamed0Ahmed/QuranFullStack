import { resolveAdjacentColumn } from './explorer-table-column-nav';
import { resolveAdjacentRow } from './explorer-row-keydown';
import { ExplorerRowNavDirection } from './explorer-table-scroll';

export type ExplorerInteractionSource = 'immediate' | 'keyboard';
export type ExplorerTableNavDirection = 'left' | 'right' | 'up' | 'down';

export interface ExplorerTableKeydownOptions<Row extends { id: number }, Column extends string> {
  event: KeyboardEvent;
  rows: readonly Row[];
  selectedRowId: number | null;
  currentColumn: Column | null;
  columnOrder: readonly Column[];
  isColumnEnabled: (row: Row, column: Column) => boolean;
  emitColumnTarget: (row: Row, column: Column, source: ExplorerInteractionSource) => void;
  scrollToRow: (index: number, direction: ExplorerRowNavDirection) => void;
}

export function handleExplorerTableKeydown<Row extends { id: number }, Column extends string>(
  options: ExplorerTableKeydownOptions<Row, Column>,
): boolean {
  const direction = resolveExplorerTableNavDirection(options.event.key);
  if (direction === null || isBlockedExplorerKeyboardTarget(options.event.target)) {
    return false;
  }

  const currentRowId = currentExplorerRowId(options.event.target, options.selectedRowId);
  const currentColumn = options.currentColumn;
  if (currentRowId === null || currentColumn === null) {
    return false;
  }

  const currentRow = options.rows.find((row) => row.id === currentRowId);
  if (!currentRow) {
    return false;
  }

  if (direction === 'left' || direction === 'right') {
    const nextColumn = resolveAdjacentColumn(
      options.columnOrder,
      currentColumn,
      direction,
      (column) => options.isColumnEnabled(currentRow, column),
    );
    if (!nextColumn) {
      return false;
    }

    options.event.preventDefault();
    options.emitColumnTarget(currentRow, nextColumn, 'keyboard');
    return true;
  }

  const adjacentRow = resolveAdjacentEnabledRow(
    options.rows,
    currentRowId,
    direction,
    currentColumn,
    options.isColumnEnabled,
  );
  if (!adjacentRow) {
    return false;
  }

  options.event.preventDefault();
  options.scrollToRow(adjacentRow.index, direction);
  options.emitColumnTarget(adjacentRow.row, currentColumn, 'keyboard');
  return true;
}

export function resolveExplorerTableNavDirection(key: string): ExplorerTableNavDirection | null {
  switch (key) {
    case 'ArrowLeft':
      return 'left';
    case 'ArrowRight':
      return 'right';
    case 'ArrowUp':
      return 'up';
    case 'ArrowDown':
      return 'down';
    default:
      return null;
  }
}

export function currentExplorerRowId(
  target: EventTarget | null,
  selectedRowId: number | null,
): number | null {
  if (selectedRowId !== null) {
    return selectedRowId;
  }

  // Table-shell arrowing needs an existing selection; otherwise only in-row focus can seed row context.
  const rowElement = target instanceof HTMLElement ? target.closest<HTMLElement>('[data-row-id]') : null;
  if (!rowElement) {
    return null;
  }

  const rowId = Number(rowElement.dataset['rowId']);
  return Number.isFinite(rowId) ? rowId : null;
}

export function isBlockedExplorerKeyboardTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }

  if (target.closest('.qd-pagination')) {
    return true;
  }

  const tag = target.tagName.toLowerCase();
  return tag === 'input' || tag === 'select' || tag === 'textarea';
}

function resolveAdjacentEnabledRow<Row extends { id: number }, Column extends string>(
  rows: readonly Row[],
  currentRowId: number,
  direction: 'up' | 'down',
  column: Column,
  isColumnEnabled: (row: Row, column: Column) => boolean,
): { row: Row; index: number } | null {
  let adjacentRow = resolveAdjacentRow(rows, currentRowId, direction);

  while (adjacentRow && !isColumnEnabled(adjacentRow.row, column)) {
    adjacentRow = resolveAdjacentRow(rows, adjacentRow.row.id, direction);
  }

  return adjacentRow;
}
