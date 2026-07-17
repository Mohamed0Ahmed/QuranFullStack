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

/** The min/max text a metric's مخصّص inputs currently hold, before the user commits it. */
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

/**
 * Shared count-range filter (Feature 026, US5; chips reshaped in Feature 030, N4) for the four normal
 * explorers. Each metric row offers exactly three chips — `أكثر من N` / `أقل من N` / `مخصّص` — where N is
 * the metric's threshold. The chips are presentation-only shortcuts that emit ordinary ranges, so the URL
 * grammar, API params and cache key are untouched and a link carrying any other range simply reopens as
 * an active مخصّص. The مخصّص inputs are draft-local: typing changes nothing outside the component, and only
 * Enter or تطبيق commits, so a range costs one navigation and one fetch instead of one per keystroke.
 * RTL, disabled while the list is loading. It is a controlled component: `ranges` is the source of truth
 * and every commit emits the full, canonical ranges map for the page's url-sync to serialize (with the
 * list page reset).
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
  // matches no chip also opens it implicitly).
  private readonly manualCustom = signal<ReadonlySet<string>>(new Set());

  private readonly chipsByMetric = computed(() => {
    const chips = new Map<string, readonly RangeChip[]>();
    for (const metric of this.metrics()) {
      chips.set(metric.key, buildRangeChips(metric.family, metric.threshold));
    }
    return chips;
  });

  // Drafts hold the user's uncommitted text and re-derive from the committed ranges whenever those
  // change from outside the component (URL restore, Back/Forward, clear-all).
  //
  // Re-sync is decided per metric on the committed range's CONTENT, never on the `ranges` object
  // identity: the pages re-`set` a freshly parsed object on every queryParamMap emission, so an
  // identity check would rebuild every draft on any navigation — committing one metric, paginating,
  // or sorting would wipe a sibling metric's typed-but-uncommitted text out of a panel still open.
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

  // Typing only edits the draft — no emit, so no navigation, no history entry and no fetch until commit.
  protected onMinDraft(metric: RangeMetric, raw: string): void {
    this.setDraft(metric.key, { min: raw, max: this.draftFor(metric).max });
  }

  protected onMaxDraft(metric: RangeMetric, raw: string): void {
    this.setDraft(metric.key, { min: this.draftFor(metric).min, max: raw });
  }

  // Enter (preventing the implicit form submit) and تطبيق share this one commit path.
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
