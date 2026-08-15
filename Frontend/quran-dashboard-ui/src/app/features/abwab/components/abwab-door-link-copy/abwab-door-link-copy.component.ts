import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinkCopyController } from '../../state/abwab-door-link-copy.controller';
import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabDoorPickerComponent } from '../abwab-door-picker/abwab-door-picker.component';

@Component({
  selector: 'qd-abwab-door-link-copy',
  standalone: true,
  imports: [
    AbwabDoorPickerComponent,
    QdActionDirective,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './abwab-door-link-copy.component.html',
  styleUrl: './abwab-door-link-copy.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinkCopyComponent {
  protected readonly controller = inject(AbwabDoorLinkCopyController);
  protected readonly doors = inject(AbwabSnapshotFacade);

  protected readonly labels = ABWAB_LABELS;
  protected readonly state = this.controller.state;
  protected readonly snapshot = this.doors.snapshot;
  protected readonly pickedIds = computed(() => {
    const targetDoorId = this.state().copy.targetDoorId;
    return targetDoorId === null ? [] : [targetDoorId];
  });
  protected readonly disabledIds = computed(() => {
    const snapshot = this.snapshot();
    return snapshot === null
      ? []
      : [...snapshot.byId.values()]
          .filter((door) => door.isArchived || door.sectionRetired)
          .map((door) => door.id);
  });
  protected readonly excludedIds = computed(() => {
    const sourceDoorId = this.state().openDoorId;
    return sourceDoorId === null ? [] : [sourceDoorId];
  });
  protected readonly pickerStatus = computed(() => {
    if (this.doors.isLoading()) {
      return 'loading' as const;
    }
    if (this.doors.errorMessage() !== null) {
      return 'error' as const;
    }
    return this.doors.isEmpty() ? 'empty' as const : 'ready' as const;
  });
  protected readonly canStart = computed(() => {
    const copy = this.state().copy;
    const selection = copy.sourceSelection;
    const selectedCount = selection === null
      ? 0
      : selection.mode === 'only'
        ? selection.unitIds.length
        : Math.max(this.state().records.totalCount - selection.unitIds.length, 0);
    return copy.status === 'choosing'
      && copy.targetDoorId !== null
      && selectedCount > 0;
  });
  protected readonly batchLabel = computed(() => {
    const copy = this.state().copy;
    return copy.currentBatchNumber > 0
      ? this.labels.doorLinksCopyBatch(copy.currentBatchNumber, copy.batches.length)
      : null;
  });

  protected retryDoors(): void {
    this.doors.load();
  }
}
