import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  output,
} from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdResultListDirective } from '../../../../shared/ui/result-list/result-list.directive';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingAccessService } from '../../../linking/state/linking-access.service';
import {
  AyahCoreDto,
  AyahMutashabihatDto,
  AyahNavigationTarget,
  MutashabihatGroupDto,
  MutashabihatOccurrenceDto,
  MUTASHABIHAT_EMPTY_MESSAGE,
  MUTASHABIHAT_LOADING_MESSAGE,
  ResourceLoadState,
} from '../../models/mushaf.models';
import { parseQuranVerseKey } from '../../../../shared/quran/quran-location';
import {
  createMutashabihatLinkingLaunch,
  MutashabihatLinkingOccurrence,
} from '../../utils/mushaf-related-linking-source';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';
import { StudyAyahResultComponent } from '../study-ayah-result/study-ayah-result.component';

const FALLBACK_GROUP_PLACEHOLDER_COUNT = 2;
const MAX_GROUP_PLACEHOLDER_COUNT = 4;

type MutashabihatOccurrenceView = MutashabihatOccurrenceDto & {
  displayText: string;
  linkingSelected: boolean;
  linkingSelectionLabel: string;
  navigateLabel: string;
  selectionKey: string;
};

type MutashabihatGroupView = MutashabihatGroupDto & {
  allOccurrencesSelected: boolean;
  isIndeterminate: boolean;
  isExpanded: boolean;
  occurrenceListId: string;
  occurrenceListLabel: string;
  selectedOccurrenceCount: number;
  selectionLabel: string;
  showExpandToggle: boolean;
  visibleOccurrences: MutashabihatOccurrenceView[];
  wordRangeLabel: string;
};

@Component({
  selector: 'qd-mutashabihat-groups-card',
  standalone: true,
  imports: [
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QuranSourceLinkingActionsComponent,
    QdResultListDirective,
    StudyAyahResultComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mutashabihat-groups-card.component.html',
  styleUrls: ['./mutashabihat-groups-card.component.scss'],
})
export class MutashabihatGroupsCardComponent {
  private readonly access = inject(LinkingAccessService);

  readonly mutashabihat = input<AyahMutashabihatDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly expectedGroupCount = input<number | null>(null);
  readonly selectedAyah = input<AyahCoreDto | null>(null);

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = MUTASHABIHAT_EMPTY_MESSAGE;
  protected readonly loadingMessage = MUTASHABIHAT_LOADING_MESSAGE;
  protected readonly canUseLinking = this.access.canUseLinking;

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

  private readonly selectedOccurrenceKeys = linkedSignal<
    {
      selectedVerseKey: string | null;
      mutashabihat: AyahMutashabihatDto | null;
      canUseLinking: boolean;
      isAvailable: boolean;
    },
    ReadonlySet<string>
  >({
    source: () => {
      const selectedAyah = this.selectedAyah();
      const mutashabihat = this.mutashabihat();
      const loadState = this.loadState();
      return {
        selectedVerseKey: selectedAyah?.verseKey ?? null,
        mutashabihat,
        canUseLinking: this.canUseLinking(),
        isAvailable:
          selectedAyah !== null &&
          mutashabihat !== null &&
          mutashabihat.verseKey === selectedAyah.verseKey &&
          !loadState.isLoading &&
          !loadState.isEmpty &&
          loadState.errorMessage === null,
      };
    },
    computation: () => new Set(),
  });

  protected readonly displayGroups = computed<MutashabihatGroupView[]>(() => {
    const expandedGroupKeys = this.expandedGroupKeys();
    const selectedOccurrenceKeys = this.selectedOccurrenceKeys();
    const canUseLinking = this.canUseLinking();
    return this.availableGroups().map((group) => {
      const isExpanded = expandedGroupKeys.has(group.groupKey);
      const visibleOccurrences = isExpanded ? group.occurrences : [];
      const selectedOccurrenceCount = group.occurrences.filter((occurrence) =>
        selectedOccurrenceKeys.has(occurrenceKey(group.sourceGroupId, occurrence)),
      ).length;
      const allOccurrencesSelected =
        group.occurrences.length > 0 && selectedOccurrenceCount === group.occurrences.length;
      return {
        ...group,
        allOccurrencesSelected,
        isIndeterminate:
          selectedOccurrenceCount > 0 && selectedOccurrenceCount < group.occurrences.length,
        isExpanded,
        occurrenceListId: `mutashabihat-occurrences-${group.groupKey}`,
        occurrenceListLabel: `مواضع ${group.representativeVerseKey}`,
        selectedOccurrenceCount,
        selectionLabel: groupSelectionLabel(group, allOccurrencesSelected),
        showExpandToggle: group.occurrences.length > 0,
        visibleOccurrences: visibleOccurrences.map((occurrence) => ({
          ...occurrence,
          displayText: toStudyAyahDisplayText(occurrence.textUthmani),
          linkingSelected:
            canUseLinking && selectedOccurrenceKeys.has(occurrenceKey(group.sourceGroupId, occurrence)),
          linkingSelectionLabel: occurrenceSelectionLabel(
            occurrence,
            selectedOccurrenceKeys.has(occurrenceKey(group.sourceGroupId, occurrence)),
            canUseLinking,
          ),
          navigateLabel: `فتح ${occurrence.surahNameArabic} — ${occurrence.ayahNumber} في المصحف`,
          selectionKey: occurrenceKey(group.sourceGroupId, occurrence),
        })),
        wordRangeLabel:
          group.representativeWordFrom === group.representativeWordTo
            ? `كلمة ${group.representativeWordFrom}`
            : `كلمات ${group.representativeWordFrom}–${group.representativeWordTo}`,
      };
    });
  });

  protected readonly hasAvailableGroups = computed(() => this.availableGroups().length > 0);
  protected readonly selectedOccurrenceCount = computed(() => this.selectedOccurrences().length);
  protected readonly allOccurrencesSelected = computed(() => {
    const occurrenceKeys = this.availableGroups().flatMap((group) =>
      group.occurrences.map((occurrence) => occurrenceKey(group.sourceGroupId, occurrence)),
    );
    return occurrenceKeys.length > 0 &&
      occurrenceKeys.every((key) => this.selectedOccurrenceKeys().has(key));
  });

  protected readonly linkingLaunch = computed(() => {
    const selectedAyah = this.selectedAyah();
    return !this.canUseLinking() || selectedAyah === null
      ? null
      : createMutashabihatLinkingLaunch(selectedAyah, this.selectedOccurrences());
  });

  private readonly availableGroups = computed<readonly MutashabihatGroupDto[]>(() => {
    const mutashabihat = this.mutashabihat();
    const selectedAyah = this.selectedAyah();
    const loadState = this.loadState();
    return mutashabihat === null ||
      selectedAyah === null ||
      mutashabihat.verseKey !== selectedAyah.verseKey ||
      loadState.isLoading ||
      loadState.isEmpty ||
      loadState.errorMessage !== null
      ? []
      : mutashabihat.groups;
  });

  private readonly selectedOccurrences = computed<readonly MutashabihatLinkingOccurrence[]>(() => {
    const selectedOccurrenceKeys = this.selectedOccurrenceKeys();
    return this.availableGroups().flatMap((group) =>
      group.occurrences
        .filter((occurrence) => selectedOccurrenceKeys.has(occurrenceKey(group.sourceGroupId, occurrence)))
        .map((occurrence) => ({ sourceGroupId: group.sourceGroupId, occurrence })),
    );
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

  protected selectAllOccurrences(): void {
    if (!this.canUseLinking()) {
      return;
    }
    this.selectedOccurrenceKeys.set(
      new Set(
        this.availableGroups().flatMap((group) =>
          group.occurrences.map((occurrence) => occurrenceKey(group.sourceGroupId, occurrence)),
        ),
      ),
    );
  }

  protected clearOccurrences(): void {
    if (this.canUseLinking()) {
      this.selectedOccurrenceKeys.set(new Set());
    }
  }

  protected toggleGroupSelection(group: MutashabihatGroupView): void {
    if (!this.canUseLinking()) {
      return;
    }

    const selectedOccurrenceKeys = new Set(this.selectedOccurrenceKeys());
    group.allOccurrencesSelected
      ? group.occurrences.forEach((occurrence) =>
          selectedOccurrenceKeys.delete(occurrenceKey(group.sourceGroupId, occurrence)),
        )
      : group.occurrences.forEach((occurrence) =>
          selectedOccurrenceKeys.add(occurrenceKey(group.sourceGroupId, occurrence)),
        );
    this.selectedOccurrenceKeys.set(selectedOccurrenceKeys);
  }

  protected toggleOccurrenceSelection(selectionKey: string): void {
    if (!this.canUseLinking() || !this.availableSelectionKeys().has(selectionKey)) {
      return;
    }

    const selectedOccurrenceKeys = new Set(this.selectedOccurrenceKeys());
    selectedOccurrenceKeys.has(selectionKey)
      ? selectedOccurrenceKeys.delete(selectionKey)
      : selectedOccurrenceKeys.add(selectionKey);
    this.selectedOccurrenceKeys.set(selectedOccurrenceKeys);
  }

  protected onAyahNavigate(occurrence: MutashabihatOccurrenceDto): void {
    const verse = parseQuranVerseKey(occurrence.verseKey);
    if (!verse) {
      return;
    }
    this.ayahNavigate.emit({
      verseKey: verse.key,
      pageNumber: occurrence.pageNumber,
    });
  }

  private readonly availableSelectionKeys = computed(() =>
    new Set(
      this.availableGroups().flatMap((group) =>
        group.occurrences.map((occurrence) => occurrenceKey(group.sourceGroupId, occurrence)),
      ),
    ),
  );
}

function occurrenceKey(sourceGroupId: number, occurrence: MutashabihatOccurrenceDto): string {
  return `${sourceGroupId}:${occurrence.ayahId}:${occurrence.wordFrom}:${occurrence.wordTo}`;
}

function groupSelectionLabel(group: MutashabihatGroupDto, selected: boolean): string {
  const action = selected ? 'إلغاء تحديد' : 'تحديد';
  return `${action} كل مواضع ${group.representativeVerseKey} للربط`;
}

function occurrenceSelectionLabel(
  occurrence: MutashabihatOccurrenceDto,
  selected: boolean,
  canUseLinking: boolean,
): string {
  const action = canUseLinking && selected ? 'إلغاء تحديد' : 'تحديد';
  return `${action} الآية ${occurrence.surahNameArabic} — ${occurrence.ayahNumber} للربط`;
}
