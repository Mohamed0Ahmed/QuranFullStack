import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { LinkingPreflightCountsDto } from '../../../../core/api/generated/models/linking-preflight-counts-dto';
import { LinkingPreparedPreflightStatusDto } from '../../../../core/api/generated/models/linking-prepared-preflight-status-dto';
import { LinkingPreparedSourceSummaryDto } from '../../../../core/api/generated/models/linking-prepared-source-summary-dto';
import {
  LinkingPreflightAyahFilter,
  LinkingPreflightFilterBarComponent,
} from '../linking-preflight-filter-bar/linking-preflight-filter-bar.component';
import { LinkingVirtualAyahListComponent } from '../linking-virtual-ayah-list/linking-virtual-ayah-list.component';

const EMPTY_COUNTS: LinkingPreflightCountsDto = {
  requested: 0,
  new: 0,
  overlapping: 0,
  unchanged: 0,
  updated: 0,
  removed: 0,
  invalid: 0,
};

@Component({
  selector: 'qd-linking-preflight-ayah-viewer',
  standalone: true,
  imports: [LinkingPreflightFilterBarComponent, LinkingVirtualAyahListComponent],
  templateUrl: './linking-preflight-ayah-viewer.component.html',
  styleUrl: './linking-preflight-ayah-viewer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightAyahViewerComponent {
  readonly preflight = input.required<LinkingPreparedPreflightStatusDto>();
  readonly source = input.required<LinkingPreparedSourceSummaryDto>();
  readonly generation = input.required<number>();

  protected readonly selectedFilter = signal<LinkingPreflightAyahFilter>('ALL');
  protected readonly counts = computed(() => this.source().counts ?? EMPTY_COUNTS);
  protected readonly request = computed(() => ({
    linkingDataRevision: this.preflight().linkingDataRevision,
    preflightId: this.preflight().preflightId,
    detailKind: 'source' as const,
    preparedSourceId: this.source().preparedSourceId,
    filter: this.selectedFilter(),
    pageSize: 100,
    generation: this.generation(),
  }));

  protected selectFilter(filter: LinkingPreflightAyahFilter): void {
    this.selectedFilter.set(filter);
  }
}
