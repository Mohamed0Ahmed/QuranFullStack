import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

@Component({
  selector: 'qd-abwab-side-panel',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './abwab-side-panel.component.html',
  styleUrl: './abwab-side-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabSidePanelComponent {
  readonly selectedDoor = input<AbwabDoorDto | null>(null);
  readonly bulkMode = input(false);
  readonly bulkCount = input(0);
  readonly bulkNames = input<readonly string[]>([]);
  readonly archiveViewActive = input(false);
  readonly canCreateDoor = input(false);
  readonly canEditDoor = input(false);
  readonly canMoveDoor = input(false);
  readonly canArchiveDoor = input(false);
  readonly canUseBulkMode = input(false);
  readonly canCreateRelation = input(false);

  readonly addChildRequested = output<void>();
  readonly editRequested = output<void>();
  readonly moveRequested = output<void>();
  readonly relationsRequested = output<void>();
  readonly archiveRequested = output<void>();
  readonly clearRequested = output<void>();
  readonly bulkModeToggled = output<boolean>();
  readonly bulkMoveRequested = output<void>();
  readonly bulkRelationsRequested = output<void>();
  readonly bulkArchiveRequested = output<void>();
  readonly bulkClearRequested = output<void>();

  protected get activeDoorHeading(): string { return ABWAB_LABELS.activeDoorHeading; }
  protected get noSelectionHint(): string { return ABWAB_LABELS.noSelectionHint; }
  protected get clearLabel(): string { return ABWAB_LABELS.clearSelection; }
  protected get operationsHeading(): string { return ABWAB_LABELS.operationsHeading; }
  protected get bulkToggleLabel(): string { return ABWAB_LABELS.bulkToggleLabel; }
  protected get addChildLabel(): string { return ABWAB_LABELS.addChildOp; }
  protected get editLabel(): string { return ABWAB_LABELS.editOp; }
  protected get moveLabel(): string { return ABWAB_LABELS.moveOp; }
  protected get relationsLabel(): string { return ABWAB_LABELS.relationsOp; }
  protected get archiveLabel(): string { return ABWAB_LABELS.archiveOp; }
  protected readonly bulkCountText = computed(() => ABWAB_LABELS.bulkSelectedCount(this.bulkCount()));
  protected get bulkMoveAllLabel(): string { return ABWAB_LABELS.bulkMoveAll; }
  protected get bulkRelationsLabel(): string { return ABWAB_LABELS.relationsBulkAddOp; }
  protected get bulkArchiveAllLabel(): string { return ABWAB_LABELS.bulkArchiveAll; }
  protected get bulkClearLabel(): string { return ABWAB_LABELS.bulkClear; }

  protected toggleBulkMode(): void {
    if (!this.canUseBulkMode()) {
      return;
    }
    this.bulkModeToggled.emit(!this.bulkMode());
  }
}
