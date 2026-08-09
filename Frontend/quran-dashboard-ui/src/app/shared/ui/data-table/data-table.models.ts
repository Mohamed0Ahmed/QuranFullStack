export type QdDataTableRenderer = 'standard' | 'wide-columns' | 'grouped-rows';

export type QdDataTableState = 'loading' | 'refreshing' | 'ready' | 'empty' | 'error';

export type QdSortableHeaderState = 'ascending' | 'descending' | 'none';
export type QdDataTableRowDirection = 'up' | 'down';

export interface QdDataTableRowContext<T> {
  $implicit: T;
  row: T;
  index: number;
}
