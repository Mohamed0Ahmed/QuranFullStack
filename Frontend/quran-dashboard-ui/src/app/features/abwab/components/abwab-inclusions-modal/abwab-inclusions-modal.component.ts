import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';

import { AbwabInclusionsController } from '../../state/abwab-inclusions.controller';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { buildAbwabNodePaths } from '../../state/abwab-tree-paths';
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

type AbwabInclusionsView = 'overview' | 'add';

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
  readonly revealRequested = output<number>();

  protected readonly controller = inject(AbwabInclusionsController);
  protected readonly permissions = inject(AbwabPermissionsController);
  protected readonly labels = ABWAB_LABELS;
  protected readonly view = signal<AbwabInclusionsView>('overview');

  private readonly picker = viewChild(AbwabDoorPickerComponent);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
  private activeTargetId: number | null = null;
  private readonly pathsById = computed(() => buildAbwabNodePaths(this.liveRoots()));
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
      const targetId = this.controller.target()?.id ?? null;
      const addCompletion = this.controller.addCompletion();
      untracked(() => {
        if (!open || targetId !== this.activeTargetId || addCompletion > 0) {
          this.view.set('overview');
        }
        this.activeTargetId = open ? targetId : null;
      });
    });

    effect(() => {
      const open = this.controller.isOpen();
      const loaded = this.controller.topology() !== null;
      const view = this.view();
      const canCreateSources = this.canCreateSources();
      untracked(() => {
        if (open && view === 'add' && !canCreateSources) {
          this.host.nativeElement
            .querySelector<HTMLElement>('[data-testid="abwab-inclusions-modal-close"]')
            ?.focus();
          this.controller.clearSourceDraft();
          this.view.set('overview');
        } else if (open && loaded && view === 'add') {
          setTimeout(() => this.picker()?.focusSearch());
        }
      });
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  protected openAddSources(): void {
    if (!this.canCreateSources()) {
      return;
    }
    this.controller.clearSourceDraft();
    this.controller.clearNotice();
    this.view.set('add');
  }

  protected backToOverview(): void {
    this.controller.clearSourceDraft();
    this.view.set('overview');
  }

  protected submit(): void {
    if (this.view() === 'add') {
      this.controller.submit();
    }
  }

  protected doorPath(doorId: number, doorName: string): string {
    return this.pathsById().get(doorId) ?? doorName;
  }

  protected revealAriaLabel(doorName: string): string {
    return ABWAB_LABELS.doorRevealAriaLabel(doorName);
  }

  protected requestReveal(doorId: number): void {
    this.revealRequested.emit(doorId);
  }

  protected detachAriaLabel(doorName: string): string {
    return ABWAB_LABELS.inclusionsDetachAriaLabel(doorName);
  }

  protected detachConfirmBody(sourceName: string, targetName: string): string {
    return ABWAB_LABELS.inclusionsDetachConfirmBody(sourceName, targetName);
  }
}
