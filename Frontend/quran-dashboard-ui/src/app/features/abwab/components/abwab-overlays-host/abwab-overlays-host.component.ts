import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import { AbwabPageOverlaysController } from '../../state/abwab-page-overlays.controller';
import { AbwabPageInteractionsController } from '../../state/abwab-page-interactions.controller';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabDoorModalComponent } from '../abwab-door-modal/abwab-door-modal.component';
import { AbwabMovePickerComponent } from '../abwab-move-picker/abwab-move-picker.component';
import { AbwabDoorRestoreModalComponent } from '../abwab-door-restore-modal/abwab-door-restore-modal.component';
import { AbwabRelationsModalComponent } from '../abwab-relations-modal/abwab-relations-modal.component';
import { AbwabSectionsModalComponent } from '../abwab-sections-modal/abwab-sections-modal.component';
import { AbwabInclusionsModalComponent } from '../abwab-inclusions-modal/abwab-inclusions-modal.component';
import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabModalUrlController } from '../../state/abwab-modal-url.controller';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import type { AbwabTreeComponent } from '../abwab-tree/abwab-tree.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';

@Component({
  selector: 'qd-abwab-overlays-host',
  standalone: true,
  imports: [
    AbwabDoorModalComponent,
    AbwabMovePickerComponent,
    AbwabDoorRestoreModalComponent,
    AbwabRelationsModalComponent,
    AbwabSectionsModalComponent,
    AbwabInclusionsModalComponent,
    QdContextMenuComponent,
    ConfirmDialogComponent,
    QdErrorStateComponent,
  ],
  templateUrl: './abwab-overlays-host.component.html',
  styleUrl: './abwab-overlays-host.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabOverlaysHostComponent {
  readonly activeSectionId = input.required<number | null>();
  readonly sections = input.required<readonly AbwabTreeSectionDto[]>();
  readonly liveRoots = input.required<readonly AbwabNode[]>();
  readonly tree = input<AbwabTreeComponent>();

  readonly doorModalClosed = output<void>();
  readonly movePickerClosed = output<void>();
  readonly doorRestored = output<void>();
  readonly relationsModalClosed = output<void>();
  readonly sectionsModalClosed = output<void>();
  readonly revealRequested = output<number>();
  readonly archiveConfirmed = output<void>();
  readonly bulkArchiveConfirmed = output<void>();
  readonly archiveConfirmCancelled = output<void>();
  readonly inclusionsModalClosed = output<void>();

  protected readonly overlays = inject(AbwabPageOverlaysController);
  protected readonly interactions = inject(AbwabPageInteractionsController);
  protected readonly permissions = inject(AbwabPermissionsController);
  protected readonly snapshot = inject(AbwabSnapshotFacade);
  protected readonly modalUrl = inject(AbwabModalUrlController);
  protected readonly labels = ABWAB_LABELS;

  protected readonly byId = computed(() =>
    this.snapshot.snapshot()?.byId ?? new Map<number, AbwabNode>(),
  );

  protected inclusionsContextMenuLabel(node: AbwabNode): string {
    return ABWAB_LABELS.inclusionsContextMenuLabel(
      node.inclusionSourceCount,
      node.inclusionConsumerCount,
    );
  }
}
