import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinkCopyController } from '../../state/abwab-door-link-copy.controller';
import { AbwabManagementPickerComponent } from '../abwab-management-picker/abwab-management-picker.component';

@Component({
  selector: 'qd-abwab-door-link-copy',
  standalone: true,
  imports: [
    AbwabManagementPickerComponent,
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

  protected readonly labels = ABWAB_LABELS;
  protected readonly state = this.controller.state;
  protected readonly excludedIds = computed(() => {
    const sourceDoorId = this.state().openDoorId;
    return sourceDoorId === null ? [] : [sourceDoorId];
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

  protected startFromEnter(event: Event): void {
    if (!(event instanceof KeyboardEvent) || event.defaultPrevented || event.isComposing || event.target instanceof HTMLButtonElement || !this.canStart()) {
      return;
    }
    event.preventDefault();
    this.controller.start();
  }
}
