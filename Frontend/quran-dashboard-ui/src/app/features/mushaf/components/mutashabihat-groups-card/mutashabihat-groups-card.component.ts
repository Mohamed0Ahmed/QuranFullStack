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

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = MUTASHABIHAT_EMPTY_MESSAGE;
  protected readonly loadingMessage = MUTASHABIHAT_LOADING_MESSAGE;

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
