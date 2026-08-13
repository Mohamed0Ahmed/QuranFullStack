import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import {
  LinkingAyahClassification,
  LinkingPreflightAyahFilter,
  LinkingSourcePreflight,
} from '../../models/linking-preflight.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingPreflightPreviewFacade } from '../../state/linking-preflight-preview.facade';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';
import { LinkingPreflightAyahGroupComponent } from '../linking-preflight-ayah-group/linking-preflight-ayah-group.component';

interface LinkingPreflightFilterOption {
  value: LinkingPreflightAyahFilter;
  label: string;
  count: number;
}

const AYAH_FILTERS: readonly LinkingPreflightAyahFilter[] = [
  'ALL',
  'NEW_AYAH',
  'UNCHANGED',
  'UPDATE',
];

@Component({
  selector: 'qd-linking-preflight-ayah-viewer',
  standalone: true,
  imports: [
    QdChipComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    LinkingAyahCardComponent,
    LinkingPreflightAyahGroupComponent,
  ],
  templateUrl: './linking-preflight-ayah-viewer.component.html',
  styleUrl: './linking-preflight-ayah-viewer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightAyahViewerComponent {
  private readonly preview = inject(LinkingPreflightPreviewFacade);

  readonly source = input.required<LinkingSourcePreflight>();

  protected readonly labels = LINKING_LABELS;
  protected readonly status = computed(() => this.preview.statusFor(this.source().sourceIdentity));
  protected readonly errorMessage = computed(() => this.preview.errorFor(this.source().sourceIdentity));
  protected readonly selectedFilter = computed(() => this.preview.filterFor(this.source().sourceIdentity));
  protected readonly ayahs = computed(() => this.preview.viewsFor(this.source()));
  protected readonly grouped = computed(() => this.source().contributionMode === 'manual_grouped');
  protected readonly filters = computed<readonly LinkingPreflightFilterOption[]>(() =>
    AYAH_FILTERS
      .map((filter) => ({
        value: filter,
        label: filter === 'ALL' ? this.labels.preflightAyahFilterAll : this.classificationLabel(filter),
        count: this.filterCount(filter),
      }))
      .filter((filter) => filter.count > 0),
  );

  protected selectFilter(filter: LinkingPreflightAyahFilter): void {
    this.preview.setFilter(this.source().sourceIdentity, filter);
  }

  protected retry(): void {
    this.preview.retry(this.source());
  }

  private filterCount(filter: LinkingPreflightAyahFilter): number {
    switch (filter) {
      case 'ALL':
        return this.source().ayahs.length;
      case 'NEW_AYAH':
        return this.source().counts.new;
      case 'UNCHANGED':
        return this.source().counts.unchanged;
      case 'UPDATE':
        return this.source().counts.updated + this.source().counts.overlapping;
      default:
        return 0;
    }
  }

  private classificationLabel(classification: LinkingAyahClassification): string {
    return this.labels.ayahClassifications[classification];
  }
}
