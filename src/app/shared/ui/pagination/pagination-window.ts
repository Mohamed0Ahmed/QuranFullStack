export const DEFAULT_PAGINATION_WINDOW_SIZE = 5;
export const MOBILE_PAGINATION_WINDOW_SIZE = 3;

export function buildPaginationWindow(
  currentPage: number,
  lastPage: number,
  windowSize = DEFAULT_PAGINATION_WINDOW_SIZE,
): number[] {
  if (lastPage <= 0) {
    return [];
  }

  if (lastPage <= windowSize) {
    return Array.from({ length: lastPage }, (_, index) => index + 1);
  }

  let start = Math.max(1, currentPage - Math.floor(windowSize / 2));
  let end = start + windowSize - 1;

  if (end > lastPage) {
    end = lastPage;
    start = Math.max(1, end - windowSize + 1);
  }

  return Array.from({ length: end - start + 1 }, (_, index) => start + index);
}
