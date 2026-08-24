import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { QdRefreshingIndicatorComponent } from '../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinksFacade } from '../../state/abwab-door-links.facade';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabDoorLinkOperationsComponent } from '../abwab-door-link-operations/abwab-door-link-operations.component';
import { AbwabDoorLinkCopyComponent } from '../abwab-door-link-copy/abwab-door-link-copy.component';
import { AbwabDoorLinksListComponent } from '../abwab-door-links-list/abwab-door-links-list.component';

@Component({
  selector: 'qd-abwab-door-links-panel',
  standalone: true,
  imports: [
    AbwabDoorLinkOperationsComponent,
    AbwabDoorLinkCopyComponent,
    AbwabDoorLinksListComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdRefreshingIndicatorComponent,
    QdSkeletonRowsComponent,
    QdModalShellComponent,
  ],
  templateUrl: './abwab-door-links-panel.component.html',
  styleUrl: './abwab-door-links-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinksPanelComponent {
  protected readonly facade = inject(AbwabDoorLinksFacade);
  protected readonly permissions = inject(AbwabPermissionsController);

  readonly doorId = input.required<number>();
  readonly doorName = input.required<string>();
  readonly dismissed = output<void>();

  protected readonly state = this.facade.state;
  protected readonly modalTitle = computed(() => `${ABWAB_LABELS.doorLinksHeading} — ${this.doorName()}`);
  protected readonly initialLoading = computed(
    () => this.facade.recordViews().length === 0 && this.state().records.status === 'loading',
  );
  protected readonly refreshing = computed(() =>
    this.facade.recordViews().length > 0 && ['loading', 'refreshing'].includes(this.state().records.status),
  );

  protected get selectAllLabel(): string { return ABWAB_LABELS.doorLinksSelectAll; }
  protected get clearSelectionLabel(): string { return ABWAB_LABELS.doorLinksClearSelection; }
  protected get emptyLabel(): string { return ABWAB_LABELS.doorLinksEmpty; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get loadingLabel(): string { return ABWAB_LABELS.doorLinksLoading; }
  protected get dismissLabel(): string { return ABWAB_LABELS.relationsCloseButton; }

  protected totalCountLabel(): string {
    return ABWAB_LABELS.doorLinksRecordsCount(this.state().records.totalCount);
  }

  protected selectedCountLabel(): string {
    return ABWAB_LABELS.doorLinksSelectedCount(this.facade.selectedCount());
  }

}
