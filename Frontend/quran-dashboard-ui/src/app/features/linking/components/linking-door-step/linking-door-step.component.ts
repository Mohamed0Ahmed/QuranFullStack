import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AbwabManagementPickerComponent } from '../../../abwab/components/abwab-management-picker/abwab-management-picker.component';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';

@Component({
  selector: 'qd-linking-door-step',
  standalone: true,
  imports: [AbwabManagementPickerComponent],
  templateUrl: './linking-door-step.component.html',
  styleUrl: './linking-door-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingDoorStepComponent {
  protected readonly workflow = inject(LinkingWorkflowFacade);

  protected changeDoor(doorId: number | null): void {
    if (doorId === null) {
      this.workflow.clearDoor();
      return;
    }
    this.workflow.selectDoor(doorId);
  }
}
