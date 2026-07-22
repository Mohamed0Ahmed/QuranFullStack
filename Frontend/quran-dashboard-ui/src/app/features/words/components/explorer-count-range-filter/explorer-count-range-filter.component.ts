import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output, signal } from '@angular/core';

import {
  CountRange,
  RangeChip,
  buildRangeChips,
  countRangesEqual,
  isRangeActive,
} from '../../models/words-filter-presets';
import { WORDS_RANGE_FILTER_LABELS } from '../../models/words-shared.labels';
import { RangeFilters, RangeMetric, hasActiveRanges } from '../../state/words-range-filters';

interface RangeDraft {
  readonly min: string;
  readonly max: string;
}

const EMPTY_DRAFT: RangeDraft = { min: '', max: '' };

function toDraft(range: CountRange | null): RangeDraft {
  return { min: boundText(range?.min ?? null), max: boundText(range?.max ?? null) };
}

function boundText(bound: number | null): string {
  return bound === null ? '' : String(bound);
}

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

  // TDZ-safe getter: a readonly field reading this labels const resolves to undefined in the bundled test build.
  protected get labels() { return WORDS_RANGE_FILTER_LABELS; }

  private readonly manualCustom = signal<ReadonlySet<string>>(new Set());

  private readonly chipsByMetric = computed(() => {
    const chips = new Map<string, readonly RangeChip[]>();
    for (const metric of this.metrics()) {
      chips.set(metric.key, buildRangeChips(metric.family, metric.threshold));
    }
    return chips;
  });

  // Re-sync per metric on the committed range's CONTENT, not the `ranges` object identity: pages re-set
  // a fresh object every navigation, so an identity check would wipe a sibling's uncommitted draft.
  private readonly drafts = linkedSignal<RangeFilters, ReadonlyMap<string, RangeDraft>>({
    source: () => this.ranges(),
    computation: (ranges, previous) => {
      const drafts = new Map<string, RangeDraft>();
      for (const metric of this.metrics()) {
        const next = ranges[metric.key] ?? null;
        const kept = previous?.value.get(metric.key);
        const committedUnchanged =
          previous !== undefined && countRangesEqual(previous.source[metric.key] ?? null, next);
        drafts.set(metric.key, kept !== undefined && committedUnchanged ? kept : toDraft(next));
      }
      return drafts;
    },
  });

  protected readonly activeCount = computed(() =>
    this.metrics().filter((metric) => isRangeActive(this.ranges()[metric.key])).length,
  );

  protected chipsFor(metric: RangeMetric): readonly RangeChip[] {
    return this.chipsByMetric().get(metric.key) ?? [];
  }

  protected chipLabel(chip: RangeChip): string {
    const prefix = chip.kind === 'gt' ? this.labels.greaterThan : this.labels.lessThan;
    return `${prefix} ${chip.threshold}`;
  }

  protected rangeFor(metric: RangeMetric): CountRange | null {
    return this.ranges()[metric.key] ?? null;
  }

  protected isChipActive(metric: RangeMetric, chip: RangeChip): boolean {
    return countRangesEqual(this.rangeFor(metric), chip);
  }

  protected isCustomActive(metric: RangeMetric): boolean {
    const range = this.rangeFor(metric);
    const matchesChip = this.chipsFor(metric).some((chip) => countRangesEqual(range, chip));
    return this.manualCustom().has(metric.key) || (isRangeActive(range) && !matchesChip);
  }

  protected draftMin(metric: RangeMetric): string {
    return this.draftFor(metric).min;
  }

  protected draftMax(metric: RangeMetric): string {
    return this.draftFor(metric).max;
  }

  protected onChipToggle(metric: RangeMetric, chip: RangeChip): void {
    if (this.disabled()) {
      return;
    }
    this.closeCustom(metric.key);
    const next = this.isChipActive(metric, chip) ? null : { min: chip.min, max: chip.max };
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

  protected onMinDraft(metric: RangeMetric, raw: string): void {
    this.setDraft(metric.key, { min: raw, max: this.draftFor(metric).max });
  }

  protected onMaxDraft(metric: RangeMetric, raw: string): void {
    this.setDraft(metric.key, { min: this.draftFor(metric).min, max: raw });
  }

  protected onCustomCommit(metric: RangeMetric, event?: Event): void {
    event?.preventDefault();
    if (this.disabled()) {
      return;
    }
    const draft = this.draftFor(metric);
    this.emit(metric.key, this.normalize({ min: this.parseBound(draft.min), max: this.parseBound(draft.max) }));
  }

  protected onCustomRevert(metric: RangeMetric): void {
    if (this.disabled()) {
      return;
    }
    this.setDraft(metric.key, toDraft(this.rangeFor(metric)));
  }

  protected onClearAll(): void {
    if (this.disabled() || !hasActiveRanges(this.ranges())) {
      return;
    }
    this.manualCustom.set(new Set());
    this.rangesChange.emit({});
  }

  private draftFor(metric: RangeMetric): RangeDraft {
    return this.drafts().get(metric.key) ?? EMPTY_DRAFT;
  }

  private setDraft(key: string, draft: RangeDraft): void {
    const next = new Map(this.drafts());
    next.set(key, draft);
    this.drafts.set(next);
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

  private parseBound(raw: string): number | null {
    const trimmed = raw.trim();
    if (trimmed.length === 0 || !/^\d+$/.test(trimmed)) {
      return null;
    }
    return Number.parseInt(trimmed, 10);
  }

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
