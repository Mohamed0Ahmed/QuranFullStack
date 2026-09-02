import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  output,
} from '@angular/core';

import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdResultListDirective } from '../../../../shared/ui/result-list/result-list.directive';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingAccessService } from '../../../linking/state/linking-access.service';
import {
  AyahCoreDto,
  AyahNavigationTarget,
  ResourceLoadState,
  SIMILAR_AYAHS_EMPTY_MESSAGE,
  SIMILAR_AYAHS_LOADING_MESSAGE,
  SimilarAyahItemDto,
  SimilarAyahsDto,
} from '../../models/mushaf.models';
import { parseQuranVerseKey, type QuranVerseKey } from '../../../../shared/quran/quran-location';
import { createSimilarAyahsLinkingLaunch } from '../../utils/mushaf-related-linking-source';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';
import { StudyAyahResultComponent } from '../study-ayah-result/study-ayah-result.component';

type CanonicalSimilarAyahItem = SimilarAyahItemDto & { targetVerseKey: QuranVerseKey };
type SimilarAyahDisplayItem = CanonicalSimilarAyahItem & {
  displayText: string;
  linkingSelected: boolean;
  linkingSelectionLabel: string;
  navigateLabel: string;
};

const FALLBACK_PLACEHOLDER_COUNT = 3;
const MAX_PLACEHOLDER_COUNT = 8;

@Component({
  selector: 'qd-similar-ayahs-card',
  standalone: true,
  imports: [
    AyahCardComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QuranSourceLinkingActionsComponent,
    QdResultListDirective,
    StudyAyahResultComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './similar-ayahs-card.component.html',
  styleUrls: ['./similar-ayahs-card.component.scss'],
})
export class SimilarAyahsCardComponent {
  private readonly access = inject(LinkingAccessService);

  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly expectedItemCount = input<number | null>(null);
  readonly selectedAyah = input<AyahCoreDto | null>(null);

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = SIMILAR_AYAHS_EMPTY_MESSAGE;
  protected readonly loadingMessage = SIMILAR_AYAHS_LOADING_MESSAGE;
  protected readonly canUseLinking = this.access.canUseLinking;

  private readonly selectedRelatedVerseKeys = linkedSignal<
    {
      selectedVerseKey: string | null;
      similarAyahs: SimilarAyahsDto | null;
      canUseLinking: boolean;
      isAvailable: boolean;
    },
    ReadonlySet<QuranVerseKey>
  >({
    source: () => {
      const selectedAyah = this.selectedAyah();
      const similarAyahs = this.similarAyahs();
      const loadState = this.loadState();
      return {
        selectedVerseKey: selectedAyah?.verseKey ?? null,
        similarAyahs,
        canUseLinking: this.canUseLinking(),
        isAvailable:
          selectedAyah !== null &&
          similarAyahs !== null &&
          !loadState.isLoading &&
          !loadState.isEmpty &&
          loadState.errorMessage === null,
      };
    },
    computation: () => new Set(),
  });

  protected readonly loadingPlaceholders = computed<readonly number[]>(() => {
    const expected = this.expectedItemCount();
    const count =
      expected === null
        ? FALLBACK_PLACEHOLDER_COUNT
        : Math.min(Math.max(expected, 0), MAX_PLACEHOLDER_COUNT);
    return Array.from({ length: count }, (_, index) => index);
  });

  protected readonly displayItems = computed<SimilarAyahDisplayItem[]>(
    () => {
      const selectedRelatedVerseKeys = this.selectedRelatedVerseKeys();
      const canUseLinking = this.canUseLinking();
      return this.availableRelatedAyahs().map((item) => ({
        ...item,
        displayText: toStudyAyahDisplayText(item.textUthmani),
        linkingSelected: canUseLinking && selectedRelatedVerseKeys.has(item.targetVerseKey),
        linkingSelectionLabel: selectionLabel(item, selectedRelatedVerseKeys, canUseLinking),
        navigateLabel: `فتح ${item.surahNameArabic} — ${item.ayahNumber} في المصحف`,
      }));
    },
  );

  protected readonly selectedRelatedAyahs = computed(() => {
    const selectedRelatedVerseKeys = this.selectedRelatedVerseKeys();
    return this.availableRelatedAyahs().filter((item) =>
      selectedRelatedVerseKeys.has(item.targetVerseKey),
    );
  });

  protected readonly selectedRelatedCount = computed(() => this.selectedRelatedAyahs().length);
  protected readonly hasAvailableRelatedAyahs = computed(
    () => this.availableRelatedAyahs().length > 0,
  );
  protected readonly allRelatedSelected = computed(() => {
    const availableRelatedAyahs = this.availableRelatedAyahs();
    return availableRelatedAyahs.length > 0 &&
      availableRelatedAyahs.every((item) =>
        this.selectedRelatedVerseKeys().has(item.targetVerseKey),
      );
  });

  protected readonly linkingLaunch = computed(() => {
    const selectedAyah = this.selectedAyah();
    return !this.canUseLinking() || selectedAyah === null
      ? null
      : createSimilarAyahsLinkingLaunch(selectedAyah, this.selectedRelatedAyahs());
  });

  private readonly availableRelatedAyahs = computed<readonly CanonicalSimilarAyahItem[]>(() => {
    const selectedAyah = this.selectedAyah();
    const similarAyahs = this.similarAyahs();
    const loadState = this.loadState();
    if (
      selectedAyah === null ||
      similarAyahs === null ||
      loadState.isLoading ||
      loadState.isEmpty ||
      loadState.errorMessage !== null
    ) {
      return [];
    }

    const selectedVerse = parseQuranVerseKey(selectedAyah.verseKey);
    if (!selectedVerse) {
      return [];
    }
    const verseKeys = new Set<QuranVerseKey>();
    return similarAyahs.items.flatMap((item) => {
      const targetVerse = parseQuranVerseKey(item.targetVerseKey);
      if (
        !targetVerse ||
        targetVerse.key === selectedVerse.key ||
        verseKeys.has(targetVerse.key)
      ) {
        return [];
      }
      verseKeys.add(targetVerse.key);
      return [{ ...item, targetVerseKey: targetVerse.key }];
    });
  });

  protected selectAllRelatedAyahs(): void {
    if (!this.canUseLinking()) {
      return;
    }
    this.selectedRelatedVerseKeys.set(
      new Set(this.availableRelatedAyahs().map((item) => item.targetVerseKey)),
    );
  }

  protected clearRelatedAyahs(): void {
    if (this.canUseLinking()) {
      this.selectedRelatedVerseKeys.set(new Set());
    }
  }

  protected toggleRelatedAyah(verseKey: QuranVerseKey): void {
    if (
      !this.canUseLinking() ||
      !this.availableRelatedAyahs().some((item) => item.targetVerseKey === verseKey)
    ) {
      return;
    }

    const selectedRelatedVerseKeys = new Set(this.selectedRelatedVerseKeys());
    selectedRelatedVerseKeys.has(verseKey)
      ? selectedRelatedVerseKeys.delete(verseKey)
      : selectedRelatedVerseKeys.add(verseKey);
    this.selectedRelatedVerseKeys.set(selectedRelatedVerseKeys);
  }

  protected onAyahNavigate(item: SimilarAyahItemDto): void {
    const verse = parseQuranVerseKey(item.targetVerseKey);
    if (!verse) {
      return;
    }
    this.ayahNavigate.emit({
      verseKey: verse.key,
      pageNumber: item.pageNumber,
    });
  }
}

function selectionLabel(
  item: CanonicalSimilarAyahItem,
  selectedVerseKeys: ReadonlySet<QuranVerseKey>,
  canUseLinking: boolean,
): string {
  const action = canUseLinking && selectedVerseKeys.has(item.targetVerseKey)
    ? 'إلغاء تحديد'
    : 'تحديد';
  return `${action} الآية ${item.surahNameArabic} — ${item.ayahNumber} للربط`;
}
