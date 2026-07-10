export function lastPageNumber(pageSize: number, totalCount: number): number {
  return Math.max(1, Math.ceil(totalCount / pageSize));
}
