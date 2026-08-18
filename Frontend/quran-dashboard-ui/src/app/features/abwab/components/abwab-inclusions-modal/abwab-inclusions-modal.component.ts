import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  untracked,
  viewChild,
} from '@angular/core';

import { AbwabInclusionsController } from '../../state/abwab-inclusions.controller';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorPickerComponent } from '../abwab-door-picker/abwab-door-picker.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import {
  QdRefreshingIndicatorComponent,
} from '../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'qd-abwab-inclusions-modal',
  standalone: true,
  imports: [
    AbwabDoorPickerComponent,
    QdModalShellComponent,
    QdActionDirective,
    QdSkeletonRowsComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdRefreshingIndicatorComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './abwab-inclusions-modal.component.html',
  styleUrl: './abwab-inclusions-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabInclusionsModalComponent {
  readonly liveRoots = input.required<readonly AbwabNode[]>();
  readonly closed = output<void>();

  protected readonly controller = inject(AbwabInclusionsController);
  protected readonly permissions = inject(AbwabPermissionsController);
  protected readonly labels = ABWAB_LABELS;

  private readonly picker = viewChild(AbwabDoorPickerComponent);
  private readonly nodesById = computed(() => {
    const nodes = new Map<number, AbwabNode>();
    const visit = (node: AbwabNode): void => {
      nodes.set(node.id, node);
      node.children.forEach(visit);
    };
    this.liveRoots().forEach(visit);
    return nodes;
  });

  protected readonly targetExcludedIds = computed(() => {
    const target = this.controller.target();
    return target === null ? [] : [target.id];
  });
  protected readonly directSourceIds = computed(() => [...this.controller.directSourceIds()]);
  protected readonly selectedSourceIds = computed(() => [...this.controller.selectedSourceIds()]);
  protected readonly selectedNames = computed(() => {
    const nodes = this.nodesById();
    return this.selectedSourceIds().map((id) => nodes.get(id)?.name ?? String(id));
  });
  protected readonly selectedSummary = computed(() =>
    this.controller.selectedSourceCount() === 0
      ? ABWAB_LABELS.inclusionsNoneSelected
      : ABWAB_LABELS.inclusionsSelectedSummary(this.controller.selectedSourceCount()),
  );
  protected readonly addButtonLabel = computed(() =>
    ABWAB_LABELS.inclusionsAddButton(this.controller.selectedSourceCount()),
  );
  protected readonly isLiveTarget = computed(() => this.controller.target()?.isArchived === false);
  protected readonly canCreateSources = computed(() =>
    this.isLiveTarget() && this.permissions.canCreateInclusion(),
  );
  protected readonly canDetachSources = computed(() =>
    this.isLiveTarget() && this.permissions.canDeleteInclusion(),
  );
  protected readonly hasArchivedParticipants = computed(() => {
    const topology = this.controller.topology();
    return topology !== null
      && (topology.sources.some((source) => source.isArchived)
        || topology.consumers.some((consumer) => consumer.isArchived));
  });

  constructor() {
    effect(() => {
      const open = this.controller.isOpen();
      const loaded = this.controller.topology() !== null;
      untracked(() => {
        if (open && loaded && this.canCreateSources()) {
          setTimeout(() => this.picker()?.focusSearch());
        }
      });
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  protected detachAriaLabel(doorName: string): string {
    return ABWAB_LABELS.inclusionsDetachAriaLabel(doorName);
  }

  protected detachConfirmBody(sourceName: string, targetName: string): string {
    return ABWAB_LABELS.inclusionsDetachConfirmBody(sourceName, targetName);
  }
}
