import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';

import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import {
  AyahMutashabihatDto,
  AyahNavigationTarget,
  MutashabihatGroupDto,
  MutashabihatOccurrenceDto,
  MUTASHABIHAT_EMPTY_MESSAGE,
  MUTASHABIHAT_LOADING_MESSAGE,
  ResourceLoadState,
} from '../../models/mushaf.models';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';
import { buildCollapsedOccurrencePreview } from './mutashabihat-occurrence-preview';

const OCCURRENCE_PREVIEW_COUNT = 5;

/**
 * N3 row 12 — loading layout reservation. The placeholder groups mirror the shape
 * of the groups that are about to arrive, so the tab body does not grow on settle.
 * Both counts come from the ayah study's similarity summary, which is already
 * loaded before this tab can be opened. `null` means "unknown" (no summary yet,
 * e.g. a deep link still resolving) and falls back to the counts below; a known `0`
 * is an empty result and reserves no groups, so a genuinely empty result no longer
 * paints tall shimmer and then collapses. Occurrences per placeholder group are the
 * summary's own average — never an invented depth — and are clamped to the preview
 * count each loaded group collapses to.
 */
const FALLBACK_GROUP_PLACEHOLDER_COUNT = 2;
const FALLBACK_OCCURRENCES_PER_GROUP = 2;
const MAX_GROUP_PLACEHOLDER_COUNT = 4;

type MutashabihatOccurrenceView = MutashabihatOccurrenceDto & {
  displayText: string;
  navigateLabel: string;
};

type MutashabihatGroupView = MutashabihatGroupDto & {
  hiddenOccurrenceCount: number;
  isExpanded: boolean;
  occurrenceListId: string;
  showExpandToggle: boolean;
  visibleOccurrences: MutashabihatOccurrenceView[];
  wordRangeLabel: string;
};

@Component({
  selector: 'qd-mutashabihat-groups-card',
  standalone: true,
  imports: [AyahCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mutashabihat-groups-card.component.html',
  styleUrls: ['./mutashabihat-groups-card.component.scss'],
})
export class MutashabihatGroupsCardComponent {
  readonly mutashabihat = input<AyahMutashabihatDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly expectedGroupCount = input<number | null>(null);
  readonly expectedOccurrenceCount = input<number | null>(null);

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = MUTASHABIHAT_EMPTY_MESSAGE;
  protected readonly loadingMessage = MUTASHABIHAT_LOADING_MESSAGE;

  protected readonly loadingGroupPlaceholders = computed<readonly number[]>(() => {
    const expected = this.expectedGroupCount();
    const count =
      expected === null
        ? FALLBACK_GROUP_PLACEHOLDER_COUNT
        : Math.min(Math.max(expected, 0), MAX_GROUP_PLACEHOLDER_COUNT);
    return Array.from({ length: count }, (_, index) => index);
  });

  protected readonly loadingOccurrencePlaceholders = computed<readonly number[]>(() => {
    const groups = this.expectedGroupCount();
    const occurrences = this.expectedOccurrenceCount();
    const averagePerGroup =
      groups !== null && groups > 0 && occurrences !== null && occurrences > 0
        ? Math.round(occurrences / groups)
        : FALLBACK_OCCURRENCES_PER_GROUP;
    const count = Math.min(Math.max(averagePerGroup, 1), OCCURRENCE_PREVIEW_COUNT);
    return Array.from({ length: count }, (_, index) => index);
  });

  private readonly expandedGroupKeys = linkedSignal<
    AyahMutashabihatDto | null,
    ReadonlySet<string>
  >({
    source: () => this.mutashabihat(),
    computation: () => new Set(),
  });

  protected readonly displayGroups = computed<MutashabihatGroupView[]>(() => {
    const mutashabihat = this.mutashabihat();
    if (!mutashabihat) {
      return [];
    }

    const expandedGroupKeys = this.expandedGroupKeys();
    return mutashabihat.groups.map((group) => {
      const isExpanded = expandedGroupKeys.has(group.groupKey);
      const visibleOccurrences = isExpanded
        ? group.occurrences
        : buildCollapsedOccurrencePreview(group.occurrences, OCCURRENCE_PREVIEW_COUNT);
      return {
        ...group,
        hiddenOccurrenceCount: isExpanded
          ? 0
          : Math.max(0, group.occurrences.length - visibleOccurrences.length),
        isExpanded,
        occurrenceListId: `mutashabihat-occurrences-${group.groupKey}`,
        showExpandToggle: group.occurrences.length > OCCURRENCE_PREVIEW_COUNT,
        visibleOccurrences: visibleOccurrences.map((occurrence) => ({
          ...occurrence,
          displayText: toStudyAyahDisplayText(occurrence.textUthmani),
          navigateLabel: `فتح ${occurrence.surahNameArabic} — ${occurrence.ayahNumber} في المصحف`,
        })),
        wordRangeLabel:
          group.representativeWordFrom === group.representativeWordTo
            ? `كلمة ${group.representativeWordFrom}`
            : `كلمات ${group.representativeWordFrom}–${group.representativeWordTo}`,
      };
    });
  });

  protected toggleGroupExpanded(groupKey: string): void {
    const next = new Set(this.expandedGroupKeys());

    if (next.has(groupKey)) {
      next.delete(groupKey);
    } else {
      next.add(groupKey);
    }

    this.expandedGroupKeys.set(next);
  }

  protected onAyahNavigate(occurrence: MutashabihatOccurrenceDto): void {
    this.ayahNavigate.emit({
      verseKey: occurrence.verseKey,
      pageNumber: occurrence.pageNumber,
    });
  }
}
