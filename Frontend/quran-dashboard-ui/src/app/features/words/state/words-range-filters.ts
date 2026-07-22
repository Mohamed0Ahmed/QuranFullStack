import { HttpParams } from '@angular/common/http';
import { ParamMap } from '@angular/router';

import {
  CountRange,
  RangeFamily,
  isRangeActive,
  parseCountRange,
  serializeCountRange,
} from '../models/words-filter-presets';

export interface RangeMetric {
  readonly key: string;
  readonly urlKey: string;
  readonly apiKey: string;
  readonly family: RangeFamily;
  readonly labelAr: string;
  readonly threshold?: number;
}

export type RangeFilters = Readonly<Record<string, CountRange>>;

export const EMPTY_RANGE_FILTERS: RangeFilters = {};

export function parseRangeFilters(queryParams: ParamMap, metrics: readonly RangeMetric[]): RangeFilters {
  const result: Record<string, CountRange> = {};
  for (const metric of metrics) {
    const range = parseCountRange(queryParams.get(metric.urlKey));
    if (isRangeActive(range)) {
      result[metric.key] = range;
    }
  }
  return result;
}

export function buildRangeQueryParams(
  ranges: RangeFilters,
  metrics: readonly RangeMetric[],
): Record<string, string | null> {
  const params: Record<string, string | null> = {};
  for (const metric of metrics) {
    params[metric.urlKey] = serializeCountRange(ranges[metric.key] ?? null);
  }
  return params;
}

export function appendRangeApiParams(
  params: HttpParams,
  ranges: RangeFilters,
  metrics: readonly RangeMetric[],
): HttpParams {
  let next = params;
  for (const metric of metrics) {
    const range = ranges[metric.key];
    if (!isRangeActive(range)) {
      continue;
    }
    if (range.min !== null) {
      next = next.set(`${metric.apiKey}Min`, range.min);
    }
    if (range.max !== null) {
      next = next.set(`${metric.apiKey}Max`, range.max);
    }
  }
  return next;
}

export function serializeRangeFiltersKey(ranges: RangeFilters, metrics: readonly RangeMetric[]): string {
  return metrics
    .map((metric) => {
      const serialized = serializeCountRange(ranges[metric.key] ?? null);
      return serialized === null ? '' : `${metric.key}=${serialized}`;
    })
    .filter((part) => part.length > 0)
    .join(',');
}

export function hasActiveRanges(ranges: RangeFilters): boolean {
  return Object.values(ranges).some((range) => isRangeActive(range));
}
