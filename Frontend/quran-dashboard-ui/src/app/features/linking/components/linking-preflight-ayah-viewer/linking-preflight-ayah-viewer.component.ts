import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { LinkingPreparedPreflightStatusDto } from '../../../../core/api/generated/models/linking-prepared-preflight-status-dto';
import { LinkingPreparedSourceSummaryDto } from '../../../../core/api/generated/models/linking-prepared-source-summary-dto';
import { LinkingVirtualAyahListComponent } from '../linking-virtual-ayah-list/linking-virtual-ayah-list.component';

@Component({
  selector: 'qd-linking-preflight-ayah-viewer',
  standalone: true,
  imports: [LinkingVirtualAyahListComponent],
  templateUrl: './linking-preflight-ayah-viewer.component.html',
  styleUrl: './linking-preflight-ayah-viewer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightAyahViewerComponent {
  readonly preflight = input.required<LinkingPreparedPreflightStatusDto>();
  readonly source = input.required<LinkingPreparedSourceSummaryDto>();
  readonly generation = input.required<number>();

  protected readonly request = computed(() => ({
    linkingDataRevision: this.preflight().linkingDataRevision,
    preflightId: this.preflight().preflightId,
    detailKind: 'source' as const,
    preparedSourceId: this.source().preparedSourceId,
    filter: 'ALL',
    pageSize: 100,
    generation: this.generation(),
  }));
}
