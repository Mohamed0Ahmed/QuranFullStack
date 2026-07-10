export function pageRelativeRowNumber(page: number, pageSize: number, index: number): number {
  return (page - 1) * pageSize + index + 1;
}
