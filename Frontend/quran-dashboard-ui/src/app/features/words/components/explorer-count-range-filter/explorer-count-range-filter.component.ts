import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import {
  CountRange,
  RANGE_BUCKETS,
  RangeBucket,
  countRangesEqual,
  isRangeActive,
} from '../../models/words-filter-presets';
import { WORDS_RANGE_FILTER_LABELS } from '../../models/words-shared.labels';
import { RangeFilters, RangeMetric, hasActiveRanges } from '../../state/words-range-filters';

/**
 * Shared count-range filter (Feature 026, US5) for the four normal explorers and the Word Types
 * flags row. Per metric it renders preset bucket chips (buttons with `aria-pressed`) plus a "مخصّص"
 * disclosure revealing min/max numeric inputs. RTL, disabled while the list is loading. It is a
 * controlled component: `ranges` is the source of truth and every interaction emits the full,
 * canonical ranges map for the page's url-sync to serialize (with the list page reset).
 */
@Component({
  selector: 'qd-explorer-count-range-filter',
  standalone: true,
  templateUrl: './explorer-count-range-filter.component.html',
  styleUrl: './explorer-count-range-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerCountRangeFilterComponent {
  readonly metrics = input.required<readonly RangeMetric[]>();
  readonly ranges = input.required<RangeFilters>();
  readonly disabled = input(false);

  readonly rangesChange = output<RangeFilters>();

  // TDZ-safe getter (see words README): reading the labels const via a readonly field resolves to
  // undefined in the bundled test build.
  protected get labels() { return WORDS_RANGE_FILTER_LABELS; }

  // Metric keys whose custom min/max panel the user opened explicitly (a manually active range that
  // matches no bucket also opens it implicitly).
  private readonly manualCustom = signal<ReadonlySet<string>>(new Set());

  protected readonly activeCount = computed(() =>
    this.metrics().filter((metric) => isRangeActive(this.ranges()[metric.key])).length,
  );

  protected bucketsFor(metric: RangeMetric): readonly RangeBucket[] {
    return RANGE_BUCKETS[metric.family];
  }

  protected rangeFor(metric: RangeMetric): CountRange | null {
    return this.ranges()[metric.key] ?? null;
  }

  protected isBucketActive(metric: RangeMetric, bucket: RangeBucket): boolean {
    return countRangesEqual(this.rangeFor(metric), bucket);
  }

  protected isCustomActive(metric: RangeMetric): boolean {
    const range = this.rangeFor(metric);
    const matchesBucket = this.bucketsFor(metric).some((bucket) => countRangesEqual(range, bucket));
    return this.manualCustom().has(metric.key) || (isRangeActive(range) && !matchesBucket);
  }

  protected minValue(metric: RangeMetric): string {
    const min = this.rangeFor(metric)?.min;
    return min === null || min === undefined ? '' : String(min);
  }

  protected maxValue(metric: RangeMetric): string {
    const max = this.rangeFor(metric)?.max;
    return max === null || max === undefined ? '' : String(max);
  }

  protected onBucketToggle(metric: RangeMetric, bucket: RangeBucket): void {
    if (this.disabled()) {
      return;
    }
    this.closeCustom(metric.key);
    const next = this.isBucketActive(metric, bucket) ? null : { min: bucket.min, max: bucket.max };
    this.emit(metric.key, next);
  }

  protected onCustomToggle(metric: RangeMetric): void {
    if (this.disabled()) {
      return;
    }
    const open = new Set(this.manualCustom());
    if (this.isCustomActive(metric)) {
      open.delete(metric.key);
      this.manualCustom.set(open);
      this.emit(metric.key, null);
      return;
    }
    open.add(metric.key);
    this.manualCustom.set(open);
  }

  protected onMinInput(metric: RangeMetric, raw: string): void {
    const current = this.rangeFor(metric);
    this.emit(metric.key, this.normalize({ min: this.parseBound(raw), max: current?.max ?? null }));
  }

  protected onMaxInput(metric: RangeMetric, raw: string): void {
    const current = this.rangeFor(metric);
    this.emit(metric.key, this.normalize({ min: current?.min ?? null, max: this.parseBound(raw) }));
  }

  protected onClearAll(): void {
    if (this.disabled() || !hasActiveRanges(this.ranges())) {
      return;
    }
    this.manualCustom.set(new Set());
    this.rangesChange.emit({});
  }

  private emit(key: string, range: CountRange | null): void {
    const next: Record<string, CountRange> = { ...this.ranges() };
    if (isRangeActive(range)) {
      next[key] = range;
    } else {
      delete next[key];
    }
    this.rangesChange.emit(next);
  }

  // A negative or non-numeric bound folds to null (open) so the URL/back-end never sees invalid input.
  private parseBound(raw: string): number | null {
    const trimmed = raw.trim();
    if (trimmed.length === 0 || !/^\d+$/.test(trimmed)) {
      return null;
    }
    return Number.parseInt(trimmed, 10);
  }

  // Guards against min > max at the input layer (fail-open: drop the max so the page still loads).
  private normalize(range: CountRange): CountRange | null {
    if (range.min !== null && range.max !== null && range.min > range.max) {
      return { min: range.min, max: null };
    }
    return isRangeActive(range) ? range : null;
  }

  private closeCustom(key: string): void {
    if (!this.manualCustom().has(key)) {
      return;
    }
    const open = new Set(this.manualCustom());
    open.delete(key);
    this.manualCustom.set(open);
  }
}
