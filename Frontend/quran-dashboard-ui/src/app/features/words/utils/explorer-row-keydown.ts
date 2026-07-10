export type ExplorerRowDirection = 'up' | 'down';

export interface AdjacentRowResult<T> {
  index: number;
  row: T;
}

export function resolveAdjacentRow<T extends { id: number }>(
  rows: readonly T[],
  currentId: number,
  direction: ExplorerRowDirection,
): AdjacentRowResult<T> | null {
  const currentIndex = rows.findIndex((row) => row.id === currentId);
  if (currentIndex === -1) {
    return null;
  }

  const nextIndex = direction === 'down' ? currentIndex + 1 : currentIndex - 1;
  if (nextIndex < 0 || nextIndex >= rows.length) {
    return null;
  }

  return {
    index: nextIndex,
    row: rows[nextIndex],
  };
}
