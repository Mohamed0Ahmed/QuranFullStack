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
} from '@angular/core';

import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { ABWAB_ORDER_SCOPE_TO_WIRE, AbwabMoveDestination, AbwabNode, AbwabOrderScope } from '../../models/abwab.models';
import { AbwabPageOverlaysController } from '../../state/abwab-page-overlays.controller';
import { AbwabManagementPickerSessionStore } from '../../state/abwab-management-picker-session.store';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabSelectionStore } from '../../state/abwab-selection.store';
import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { searchAbwabNodes } from '../../state/abwab-tree.builder';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { AbwabAnnouncerComponent } from '../abwab-announcer/abwab-announcer.component';
import { AbwabDoorModalComponent } from '../abwab-door-modal/abwab-door-modal.component';
import { AbwabMovePickerComponent } from '../abwab-move-picker/abwab-move-picker.component';
import { AbwabRelationsModalComponent } from '../abwab-relations-modal/abwab-relations-modal.component';
import { AbwabToolbarComponent } from '../abwab-toolbar/abwab-toolbar.component';
import { AbwabTreeComponent, AbwabTreeMenuRequest } from '../abwab-tree/abwab-tree.component';

const NO_IDS: ReadonlySet<number> = new Set<number>();
const NO_ROOTS: readonly AbwabNode[] = [];

@Component({
  selector: 'qd-abwab-management-picker',
  standalone: true,
  imports: [
    AbwabToolbarComponent,
    AbwabTreeComponent,
    AbwabAnnouncerComponent,
    AbwabDoorModalComponent,
    AbwabMovePickerComponent,
    AbwabRelationsModalComponent,
    QdContextMenuComponent,
    QdSkeletonRowsComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './abwab-management-picker.component.html',
  styleUrl: './abwab-management-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    AbwabSelectionStore,
    AbwabWriteController,
    AbwabPermissionsController,
    AbwabPageOverlaysController,
  ],
})
export class AbwabManagementPickerComponent {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly session = inject(AbwabManagementPickerSessionStore);

  readonly selectedDoorId = input<number | null>(null);
  readonly excludedDoorIds = input<readonly number[]>([]);
  readonly selectionMode = input(false);
  readonly selectionChanged = output<number | null>();

  protected readonly facade = inject(AbwabSnapshotFacade);
  protected readonly selection = inject(AbwabSelectionStore);
  protected readonly writeController = inject(AbwabWriteController);
  protected readonly overlays = inject(AbwabPageOverlaysController);
  protected readonly permissions = inject(AbwabPermissionsController);
  protected readonly searchQuery = signal('');
  protected readonly revealedId = signal<number | null>(null);

  private readonly pendingCreatedDoorId = signal<number | null>(null);
  private readonly revealSeedId = signal<number | null>(null);
  private sessionContextRestored = false;

  protected readonly sections = computed(() => this.facade.snapshot()?.sections ?? []);
  protected readonly byId = computed(() => this.facade.snapshot()?.byId ?? new Map<number, AbwabNode>());
  protected readonly contextMenuNode = computed(() => {
    const id = this.overlays.contextMenuDoorId();
    return id === null ? null : (this.byId().get(id) ?? null);
  });
  protected readonly excludedIds = computed(() => new Set(this.excludedDoorIds()));
  protected readonly liveRoots = computed<readonly AbwabNode[]>(
    () => this.facade.snapshot()?.liveRoots ?? NO_ROOTS,
  );
  private readonly searchResult = computed(() => searchAbwabNodes(this.liveRoots(), this.searchQuery()));
  protected readonly treeMatchedIds = computed(() => this.searchResult().matchedIds);
  protected readonly searchMatches = computed(() => Array.from(this.searchResult().matchedIds).flatMap((id) => {
    const node = this.byId().get(id);
    return node ? [node] : [];
  }));
  protected readonly searchExpandedIds = computed<ReadonlySet<number>>(() => {
    const ids = this.searchResult().autoExpandedIds;
    return ids.size === 0 ? NO_IDS : ids;
  });
  protected readonly expandSeedIds = computed<ReadonlySet<number>>(() => {
    const result = new Set(this.session.expandedDoorIds());
    const id = this.revealSeedId();
    if (id === null) {
      return result.size === 0 ? NO_IDS : result;
    }
    let parentId = this.byId().get(id)?.parentId ?? null;
    while (parentId !== null && !result.has(parentId)) {
      result.add(parentId);
      parentId = this.byId().get(parentId)?.parentId ?? null;
    }
    return result;
  });
  protected readonly selectedDoor = this.overlays.selectedDoor;
  protected readonly canDoorModalSave = computed(() =>
    this.overlays.modalDoor() === null ? this.permissions.canCreateDoor() : this.permissions.canEditDoor(),
  );
  protected readonly canArchiveSelected = computed(() => {
    const id = this.selectedDoor()?.id;
    return id !== undefined && !this.excludedIds().has(id);
  });
  protected readonly labels = ABWAB_LABELS;

  constructor() {
    this.selection.setArchiveViewActive(false);
    this.selection.setSectionScope(null);
    this.selection.setBulkMode(false);
    this.facade.load();

    effect(() => {
      const snapshot = this.facade.snapshot();
      const selectedDoorId = this.selectedDoorId();
      if (!snapshot) {
        return;
      }
      untracked(() => {
        if (selectedDoorId === null) {
          this.selection.clearSelection();
          return;
        }
        const node = snapshot.byId.get(selectedDoorId);
        if (this.isSelectable(node)) {
          this.selection.select(node.id, node.version);
          return;
        }
        this.selection.clearSelection();
        this.selectionChanged.emit(null);
      });
    });

    effect(() => {
      const snapshot = this.facade.snapshot();
      const anchorDoorId = this.selectedDoorId() ?? this.session.anchorDoorId();
      if (!snapshot || this.sessionContextRestored) {
        return;
      }
      const node = anchorDoorId === null ? undefined : snapshot.byId.get(anchorDoorId);
      const excluded = anchorDoorId !== null && this.excludedIds().has(anchorDoorId);
      untracked(() => {
        this.sessionContextRestored = true;
        if (anchorDoorId === null || excluded) {
          return;
        }
        if (!node || node.isArchived || node.sectionRetired) {
          this.session.forgetAnchorDoor(anchorDoorId);
          return;
        }
        this.revealContext(node.id);
      });
    });

    effect(() => {
      const pendingId = this.pendingCreatedDoorId();
      const node = pendingId === null ? undefined : this.byId().get(pendingId);
      if (!this.isSelectable(node)) {
        return;
      }
      untracked(() => {
        this.pendingCreatedDoorId.set(null);
        this.chooseDoor(node.id);
      });
    });

    effect(() => {
      const resolved = this.permissions.isResolved();
      this.permissions.canCreateDoor();
      this.permissions.canEditDoor();
      this.permissions.canMoveDoor();
      this.permissions.canArchiveDoor();
      if (resolved) {
        untracked(() => this.overlays.closeUnavailableWriteState());
      }
    });
  }

  protected chooseDoor(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!this.isSelectable(node)) {
      return;
    }
    this.selection.select(node.id, node.version);
    this.session.rememberAnchorDoor(node.id);
    this.selectionChanged.emit(node.id);
  }

  protected rememberExpandedDoorIds(ids: ReadonlySet<number>): void {
    this.session.rememberExpandedDoorIds(ids);
  }

  protected openMenu(request: AbwabTreeMenuRequest): void {
    if (this.excludedIds().has(request.id)) {
      return;
    }
    this.overlays.setContextMenuPosition(request.x, request.y);
    this.overlays.requestContextMenu(request.id, 'operations');
  }

  protected addChild(doorId: number): void {
    if (this.selection.selectedDoorId() !== doorId) {
      this.chooseDoor(doorId);
    }
    if (this.selection.selectedDoorId() === doorId) {
      this.overlays.openCreateChild();
    }
  }

  protected openRelations(doorId: number): void {
    if (this.selection.selectedDoorId() !== doorId) {
      this.chooseDoor(doorId);
    }
    if (this.selection.selectedDoorId() === doorId) {
      this.overlays.openRelations();
    }
  }

  protected commitOrder(event: { id: number; position: number; scope: AbwabOrderScope }): void {
    const node = this.byId().get(event.id);
    if (!node || this.excludedIds().has(event.id) || !this.permissions.canReorderDoor()) {
      return;
    }
    this.writeController.reorderDoor(event.id, {
      position: event.position,
      scope: ABWAB_ORDER_SCOPE_TO_WIRE[event.scope],
      version: node.version,
    }).subscribe();
  }

  protected revealDoor(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!this.isSelectable(node)) {
      return;
    }
    this.chooseDoor(node.id);
    this.revealContext(node.id);
  }

  protected onDoorSaved(door: AbwabDoorDto | null): void {
    if (this.overlays.modalDoor() === null && door !== null) {
      this.pendingCreatedDoorId.set(door.id);
    }
  }

  protected onMoveConfirmed(destination: AbwabMoveDestination): void {
    this.overlays.confirmMove(destination, () => this.overlays.closeMovePicker());
  }

  protected confirmArchive(): void {
    if (!this.canArchiveSelected()) {
      return;
    }
    const archivedDoorId = this.selectedDoor()?.id ?? null;
    this.overlays.confirmArchive(() => {
      if (archivedDoorId !== null) {
        this.session.forgetAnchorDoor(archivedDoorId);
      }
      this.selectionChanged.emit(null);
      this.focusTree();
    });
  }

  private isSelectable(node: AbwabNode | undefined): node is AbwabNode {
    return !!node && !node.isArchived && !node.sectionRetired && !this.excludedIds().has(node.id);
  }

  private focusTree(): void {
    setTimeout(() => {
      this.elementRef.nativeElement
        .querySelector<HTMLElement>('[data-testid="abwab-tree"] [tabindex="0"]')
        ?.focus();
    });
  }

  private revealContext(doorId: number): void {
    this.revealSeedId.set(doorId);
    this.revealedId.set(doorId);
    setTimeout(() => {
      this.elementRef.nativeElement
        .querySelector<HTMLElement>(`[data-testid="abwab-tree-row-${doorId}"]`)
        ?.scrollIntoView({ block: 'nearest' });
    });
    setTimeout(() => this.clearReveal(), 3000);
  }

  private clearReveal(): void {
    this.revealedId.set(null);
    this.revealSeedId.set(null);
  }
}
