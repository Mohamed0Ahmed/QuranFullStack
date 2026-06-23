import {
  formatPaginationRangeLabel,
  PAGINATION_EMPTY_RANGE_LABEL,
} from '../models/unique-words.labels';

export interface PageRowRange {
  start: number;
  end: number;
  count: number;
}

export function pageRowRange(page: number, pageSize: number, totalCount: number): PageRowRange {
  if (totalCount === 0) {
    return { start: 0, end: 0, count: 0 };
  }

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);
  return { start, end, count: end - start + 1 };
}

export function pageRelativeRowNumber(page: number, pageSize: number, index: number): number {
  return (page - 1) * pageSize + index + 1;
}

export function formatPageRowRangeLabel(range: PageRowRange, totalCount: number): string {
  if (totalCount === 0) {
    return PAGINATION_EMPTY_RANGE_LABEL;
  }

  return formatPaginationRangeLabel(range.start, range.end, totalCount);
}
