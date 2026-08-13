import { ScrollingModule, VIRTUAL_SCROLL_STRATEGY } from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { MergedAyahSelection } from '../../models/linking-merge.models';
import {
  LinkingAyahClassification,
  LinkingAyahPreflight,
  LinkingDoorWordImpact,
  LinkingPreflightAyahFilter,
  LinkingPreflightResult,
} from '../../models/linking-preflight.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { MeasuredRowVirtualScrollStrategy } from '../../utils/measured-row-virtual-scroll.strategy';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';
import {
  LinkingPreflightAyahGroupComponent,
  LinkingPreflightGroupedAyahView,
} from '../linking-preflight-ayah-group/linking-preflight-ayah-group.component';

interface LinkingMergedPreflightAyahView {
  selection: MergedAyahSelection;
  preflight: LinkingAyahPreflight | null;
  classification: LinkingAyahClassification | null;
  wordImpact: LinkingDoorWordImpact;
  invalidReason: string | null;
}

interface LinkingMergedPreflightFilterOption {
  value: LinkingPreflightAyahFilter;
  label: string;
  count: number;
}

const ESTIMATED_AYAH_ROW_SIZE = 156;
const AYAH_ROW_BUFFER = 720;
const AYAH_FILTERS: readonly LinkingPreflightAyahFilter[] = [
  'ALL',
  'NEW_AYAH',
  'UNCHANGED',
  'UPDATE',
];
const CLASSIFICATION_PRIORITY: Readonly<Record<LinkingAyahClassification, number>> = {
  UNCHANGED: 0,
  NEW_AYAH: 1,
  OVERLAP_OTHER_SOURCE: 2,
  UPDATE: 3,
  REMOVE: 4,
  INVALID: 5,
};

@Component({
  selector: 'qd-linking-preflight-merged-ayah-viewer',
  standalone: true,
  imports: [
    ScrollingModule,
    QdChipComponent,
    QdEmptyStateComponent,
    LinkingAyahCardComponent,
    LinkingPreflightAyahGroupComponent,
  ],
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (): MeasuredRowVirtualScrollStrategy =>
        new MeasuredRowVirtualScrollStrategy(ESTIMATED_AYAH_ROW_SIZE, AYAH_ROW_BUFFER),
    },
  ],
  templateUrl: './linking-preflight-merged-ayah-viewer.component.html',
  styleUrl: './linking-preflight-merged-ayah-viewer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightMergedAyahViewerComponent {
  readonly preflight = input.required<LinkingPreflightResult>();
  readonly ayahs = input.required<readonly MergedAyahSelection[]>();

  protected readonly labels = LINKING_LABELS;
  protected readonly selectedFilter = signal<LinkingPreflightAyahFilter>('ALL');
  protected readonly allViews = computed(() => mergePreflightAyahs(this.ayahs(), this.preflight()));
  protected readonly visibleViews = computed(() =>
    this.allViews().filter((view) => matchesFilter(view.classification, this.selectedFilter())),
  );
  protected readonly groupedSource = computed(() => {
    const sources = this.preflight().sources;
    return sources.length === 1 && sources[0]?.contributionMode === 'manual_grouped'
      ? sources[0]
      : null;
  });
  protected readonly groupedItems = computed<readonly LinkingPreflightGroupedAyahView[]>(() =>
    this.visibleViews().flatMap((view) =>
      view.preflight === null
        ? []
        : [{ ayah: view.selection.ayah, preflight: view.preflight }],
    ),
  );
  protected readonly filters = computed<readonly LinkingMergedPreflightFilterOption[]>(() =>
    AYAH_FILTERS
      .map((filter) => ({
        value: filter,
        label: filter === 'ALL' ? this.labels.preflightAyahFilterAll : this.classificationLabel(filter),
        count: this.allViews().filter((view) => matchesFilter(view.classification, filter)).length,
      }))
      .filter((filter) => filter.count > 0),
  );
  protected readonly trackAyah = (_index: number, view: LinkingMergedPreflightAyahView): string =>
    view.selection.verseKey;

  protected selectFilter(filter: LinkingPreflightAyahFilter): void {
    this.selectedFilter.set(filter);
  }

  private classificationLabel(classification: LinkingAyahClassification): string {
    return this.labels.ayahClassifications[classification];
  }
}

function mergePreflightAyahs(
  selections: readonly MergedAyahSelection[],
  preflight: LinkingPreflightResult,
): readonly LinkingMergedPreflightAyahView[] {
  const preflightByVerseKey = new Map<string, LinkingAyahPreflight[]>();
  for (const ayah of preflight.sources.flatMap((source) => source.ayahs)) {
    const matches = preflightByVerseKey.get(ayah.verseKey) ?? [];
    matches.push(ayah);
    preflightByVerseKey.set(ayah.verseKey, matches);
  }

  return selections.map((selection) => {
    const matches = preflightByVerseKey.get(selection.verseKey) ?? [];
    return {
      selection,
      preflight: matches.at(0) ?? null,
      classification: strongestClassification(matches),
      wordImpact: mergeWordImpact(matches),
      invalidReason: matches.find((match) => match.invalidReason !== null)?.invalidReason ?? null,
    };
  });
}

function strongestClassification(
  matches: readonly LinkingAyahPreflight[],
): LinkingAyahClassification | null {
  return matches.reduce<LinkingAyahClassification | null>((strongest, match) => {
    if (
      strongest === null ||
      CLASSIFICATION_PRIORITY[match.classification] > CLASSIFICATION_PRIORITY[strongest]
    ) {
      return match.classification;
    }
    return strongest;
  }, null);
}

function mergeWordImpact(matches: readonly LinkingAyahPreflight[]): LinkingDoorWordImpact {
  return {
    added: uniqueWordIds(matches, 'added'),
    existing: uniqueWordIds(matches, 'existing'),
    removed: uniqueWordIds(matches, 'removed'),
  };
}

function uniqueWordIds(
  matches: readonly LinkingAyahPreflight[],
  kind: keyof LinkingDoorWordImpact,
): readonly number[] {
  return [...new Set(matches.flatMap((match) => match.doorWordImpact[kind]))];
}

function matchesFilter(
  classification: LinkingAyahClassification | null,
  filter: LinkingPreflightAyahFilter,
): boolean {
  if (filter === 'ALL') {
    return true;
  }
  if (filter === 'UPDATE') {
    return classification === 'UPDATE' || classification === 'OVERLAP_OTHER_SOURCE';
  }
  return classification === filter;
}
