import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { LinkingPreparedPreflightStatusDto } from '../../../../core/api/generated/models/linking-prepared-preflight-status-dto';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingVirtualAyahListComponent } from '../linking-virtual-ayah-list/linking-virtual-ayah-list.component';

@Component({
  selector: 'qd-linking-preflight-merged-ayah-viewer',
  standalone: true,
  imports: [LinkingVirtualAyahListComponent],
  templateUrl: './linking-preflight-merged-ayah-viewer.component.html',
  styleUrl: './linking-preflight-merged-ayah-viewer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightMergedAyahViewerComponent {
  readonly preflight = input.required<LinkingPreparedPreflightStatusDto>();
  readonly generation = input.required<number>();

  protected readonly labels = LINKING_LABELS;
  protected readonly request = computed(() => ({
    linkingDataRevision: this.preflight().linkingDataRevision,
    preflightId: this.preflight().preflightId,
    detailKind: 'merged' as const,
    preparedSourceId: null,
    filter: 'ALL',
    pageSize: 100,
    generation: this.generation(),
  }));
}
