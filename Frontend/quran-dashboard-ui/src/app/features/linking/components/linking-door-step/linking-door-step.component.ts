import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AbwabDoorPickerComponent } from '../../../abwab/components/abwab-door-picker/abwab-door-picker.component';
import { AbwabSnapshotFacade } from '../../../abwab/state/abwab-snapshot.facade';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';

@Component({
  selector: 'qd-linking-door-step',
  standalone: true,
  imports: [AbwabDoorPickerComponent],
  templateUrl: './linking-door-step.component.html',
  styleUrl: './linking-door-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingDoorStepComponent {
  private readonly doors = inject(AbwabSnapshotFacade);
  private readonly workflow = inject(LinkingWorkflowFacade);

  protected readonly labels = LINKING_LABELS;
  protected readonly snapshot = this.doors.snapshot;
  protected readonly errorMessage = this.doors.errorMessage;
  protected readonly pickedIds = computed(() => {
    const id = this.workflow.selectedDoorId();
    return id === null ? [] : [id];
  });
  protected readonly status = computed(() => {
    if (this.doors.isLoading()) {
      return 'loading' as const;
    }
    if (this.doors.errorMessage() !== null) {
      return 'error' as const;
    }
    return this.doors.isEmpty() ? 'empty' as const : 'ready' as const;
  });

  protected selectDoor(doorId: number): void {
    this.workflow.selectDoor(doorId);
  }

  protected retry(): void {
    this.workflow.loadDoors();
  }
}
