import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

import { OwnerReconciliationStatus } from '../../../../core/api/generated/models/owner-reconciliation-status';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../shared/ui/result-list/result-list.directive';
import { ACCESS_ADMIN_LABELS } from '../../models/access-admin.labels';

@Component({
  selector: 'qd-access-owner-reconciliation',
  standalone: true,
  imports: [
    ExplorerPanelSkeletonComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdResultItemDirective,
    QdResultListDirective,
  ],
  templateUrl: './access-owner-reconciliation.component.html',
  styleUrl: './access-owner-reconciliation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessOwnerReconciliationComponent {
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly status = input<OwnerReconciliationStatus | null>(null);

  protected readonly fingerprintVisible = signal(false);

  protected toggleFingerprint(): void {
    this.fingerprintVisible.update((visible) => !visible);
  }

  protected candidateStateLabel(state: string): string {
    return ACCESS_ADMIN_LABELS.reconciliationCandidateState(state);
  }
}
