import { Observable, concatMap, expand, map, reduce, takeWhile } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PagedResultDto } from '../../../core/data-access/paged-result.model';

export interface CompletePagedSourceProgress {
  loaded: number;
  total: number;
}

export function loadCompletePagedSource<T>(
  loadPage: (page: number) => Observable<ApiResponse<PagedResultDto<T>>>,
  onProgress: (progress: CompletePagedSourceProgress) => void,
): Observable<readonly T[]> {
  return loadPage(1).pipe(
    map((response) => validatePage(response, null)),
    expand((page) =>
      page.data.page * page.data.pageSize >= page.data.totalCount
        ? []
        : loadPage(page.data.page + 1).pipe(map((response) => validatePage(response, page.data))),
    ),
    takeWhile((page) => page.data.page * page.data.pageSize < page.data.totalCount, true),
    concatMap((page) => {
      onProgress({ loaded: page.offset + page.data.items.length, total: page.data.totalCount });
      return [page];
    }),
    reduce((items, page) => [...items, ...page.data.items], [] as T[]),
  );
}

interface ValidatedPage<T> {
  data: PagedResultDto<T>;
  offset: number;
}

function validatePage<T>(
  response: ApiResponse<PagedResultDto<T>>,
  previous: PagedResultDto<T> | null,
): ValidatedPage<T> {
  const data = response.data;
  if (!response.isSuccess || !data) {
    throw new Error(response.message || 'تعذر تحميل نتائج المصدر كاملة.');
  }
  if (
    !Number.isSafeInteger(data.page) ||
    !Number.isSafeInteger(data.pageSize) ||
    !Number.isSafeInteger(data.totalCount) ||
    (previous === null && data.page !== 1) ||
    data.page < 1 ||
    data.pageSize < 1 ||
    data.totalCount < 0 ||
    data.items.length > data.pageSize ||
    (previous !== null &&
      (data.page !== previous.page + 1 ||
        data.pageSize !== previous.pageSize ||
        data.totalCount !== previous.totalCount))
  ) {
    throw new Error('بيانات نتائج المصدر غير مكتملة.');
  }

  const offset = (data.page - 1) * data.pageSize;
  const expectedItemCount = Math.min(data.pageSize, Math.max(data.totalCount - offset, 0));
  if (data.items.length !== expectedItemCount) {
    throw new Error('بيانات نتائج المصدر غير مكتملة.');
  }
  return { data, offset };
}
