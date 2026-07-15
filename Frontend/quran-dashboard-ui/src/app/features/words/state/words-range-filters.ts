import { HttpParams } from '@angular/common/http';
import { ParamMap } from '@angular/router';

import {
  BucketFamily,
  CountRange,
  isRangeActive,
  parseCountRange,
  serializeCountRange,
} from '../models/words-filter-presets';

/**
 * Describes one count-range-filterable metric for an explorer (Feature 026, US5). The URL key is the
 * shareable contract; the API prefix maps to the backend `<prefix>Min`/`<prefix>Max` params; the
 * family selects the preset bucket set; the label names the metric in the filter UI.
 */
export interface RangeMetric {
  readonly key: string;
  readonly urlKey: string;
  readonly apiKey: string;
  readonly family: BucketFamily;
  readonly labelAr: string;
}

/** Active ranges keyed by metric key; absent metrics are omitted (pre-feature identity when empty). */
export type RangeFilters = Readonly<Record<string, CountRange>>;

export const EMPTY_RANGE_FILTERS: RangeFilters = {};

/** Parses every metric's URL value fail-closed; only active ranges are kept. */
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

/**
 * Builds a full set of URL param changes for the given ranges — every metric key is emitted (active
 * ⇒ serialized, absent ⇒ null so a cleared metric drops its key). Callers merge this into the router.
 */
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

/** Appends `<apiKey>Min`/`<apiKey>Max` only for active ranges (absent ⇒ pre-feature request). */
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

/**
 * Deterministic cache-key fragment for the active ranges. Empty ⇒ '' so an unfiltered read keeps its
 * pre-feature cache key byte-identical.
 */
export function serializeRangeFiltersKey(ranges: RangeFilters, metrics: readonly RangeMetric[]): string {
  return metrics
    .map((metric) => {
      const serialized = serializeCountRange(ranges[metric.key] ?? null);
      return serialized === null ? '' : `${metric.key}=${serialized}`;
    })
    .filter((part) => part.length > 0)
    .join(',');
}

/** True when any metric has an active range (drives the "filters active" affordances). */
export function hasActiveRanges(ranges: RangeFilters): boolean {
  return Object.values(ranges).some((range) => isRangeActive(range));
}
