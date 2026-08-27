import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdResultListDirective } from '../../../../shared/ui/result-list/result-list.directive';
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

const FALLBACK_GROUP_PLACEHOLDER_COUNT = 2;
const MAX_GROUP_PLACEHOLDER_COUNT = 4;

type MutashabihatOccurrenceView = MutashabihatOccurrenceDto & {
  displayText: string;
  navigateLabel: string;
};

type MutashabihatGroupView = MutashabihatGroupDto & {
  isExpanded: boolean;
  occurrenceListId: string;
  occurrenceListLabel: string;
  showExpandToggle: boolean;
  visibleOccurrences: MutashabihatOccurrenceView[];
  wordRangeLabel: string;
};

@Component({
  selector: 'qd-mutashabihat-groups-card',
  standalone: true,
  imports: [
    AyahCardComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdResultListDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mutashabihat-groups-card.component.html',
  styleUrls: ['./mutashabihat-groups-card.component.scss'],
})
export class MutashabihatGroupsCardComponent {
  readonly mutashabihat = input<AyahMutashabihatDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly expectedGroupCount = input<number | null>(null);

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
      const visibleOccurrences = isExpanded ? group.occurrences : [];
      return {
        ...group,
        isExpanded,
        occurrenceListId: `mutashabihat-occurrences-${group.groupKey}`,
        occurrenceListLabel: `مواضع ${group.representativeVerseKey}`,
        showExpandToggle: group.occurrences.length > 0,
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
