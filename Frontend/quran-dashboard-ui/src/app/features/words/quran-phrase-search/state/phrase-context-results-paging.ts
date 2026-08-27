import { lastPageNumber } from '../../../../shared/ui/pagination/pagination-range';

export function contextResultsRedirectPage(
  currentPage: number,
  pageSize: number,
  totalCount: number,
): number | null {
  const lastPage = lastPageNumber(pageSize, totalCount);
  return currentPage > lastPage ? lastPage : null;
}
