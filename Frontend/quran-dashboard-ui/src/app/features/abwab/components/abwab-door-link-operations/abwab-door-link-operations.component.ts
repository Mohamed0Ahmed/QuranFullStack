import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinksFacade } from '../../state/abwab-door-links.facade';
import { ABWAB_DOOR_LINK_OPERATIONS_LABELS } from './abwab-door-link-operations.labels';

@Component({
  selector: 'qd-abwab-door-link-operations',
  standalone: true,
  imports: [ConfirmDialogComponent, QdActionDirective, QdErrorStateComponent],
  templateUrl: './abwab-door-link-operations.component.html',
  styleUrl: './abwab-door-link-operations.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinkOperationsComponent {
  protected readonly facade = inject(AbwabDoorLinksFacade);

  readonly doorOpen = input(false);
  readonly selectedCount = input(0);

  readonly copyRequested = output<void>();

  protected readonly state = this.facade.state;
  protected readonly preparingEdit = computed(() => this.state().edit.status === 'preparing');
  protected readonly deleteBusy = computed(() => this.state().deletion.status === 'writing');
  protected readonly canEdit = computed(() =>
    this.doorOpen()
      && this.facade.selectedRecord() !== null
      && ['idle', 'load-error'].includes(this.state().edit.status),
  );
  protected readonly canDelete = computed(() =>
    this.doorOpen()
      && this.selectedCount() > 0
      && this.state().edit.status === 'idle'
      && !this.deleteBusy(),
  );
  protected readonly canUseSelection = computed(() => this.doorOpen() && this.selectedCount() > 0);
  protected readonly deleteMessage = computed(() => {
    const selectedCount = this.selectedCount();
    const totalCount = this.state().records.totalCount;
    return selectedCount > 0 && selectedCount === totalCount
      ? ABWAB_DOOR_LINK_OPERATIONS_LABELS.deleteAllMessage(selectedCount)
      : ABWAB_DOOR_LINK_OPERATIONS_LABELS.deletePartialMessage(selectedCount);
  });

  protected get heading(): string { return ABWAB_LABELS.doorLinksOperationsHeading; }
  protected get editLabel(): string { return ABWAB_LABELS.doorLinksEdit; }
  protected get deleteLabel(): string { return ABWAB_LABELS.doorLinksDelete; }
  protected get copyLabel(): string { return ABWAB_LABELS.doorLinksCopy; }
  protected get noDoorHint(): string { return ABWAB_LABELS.doorLinksNoDoorHint; }
  protected get deleteTitle(): string { return ABWAB_DOOR_LINK_OPERATIONS_LABELS.deleteTitle; }
  protected get deleteConfirmLabel(): string { return ABWAB_DOOR_LINK_OPERATIONS_LABELS.deleteConfirm; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected readonly selectedLabel = computed(() => ABWAB_LABELS.doorLinksSelectedCount(this.selectedCount()));

  protected requestEdit(): void {
    this.facade.startEdit();
  }

  protected requestDelete(): void {
    this.facade.requestDelete();
  }
}
