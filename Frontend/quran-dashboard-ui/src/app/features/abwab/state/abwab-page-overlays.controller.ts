import { Injectable, computed, inject, signal } from '@angular/core';
import { of } from 'rxjs';

import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabWriteController, AbwabWriteOutcome, abwabPermissionDenied } from './abwab-write.controller';
import { AbwabSectionsController } from './abwab-sections.controller';
import { AbwabRelationsController } from './abwab-relations.controller';
import { AbwabNode, AbwabRelationDirectionKind, AbwabRelationKind } from '../models/abwab.models';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabMoveDestination } from '../models/abwab.models';
import { AbwabPermissionsController } from './abwab-permissions.controller';

type ContextActionCallback = (doorId: number) => void;

const NO_ANCESTORS: readonly AbwabNode[] = [];

@Injectable()
export class AbwabPageOverlaysController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly selection = inject(AbwabSelectionStore);
  private readonly writeController = inject(AbwabWriteController);
  private readonly sectionsController = inject(AbwabSectionsController);
  private readonly relationsController = inject(AbwabRelationsController);
  private readonly permissions = inject(AbwabPermissionsController);

  private readonly byId = computed(() => this.facade.snapshot()?.byId ?? new Map<number, AbwabNode>());

  readonly selectedDoor = computed<AbwabDoorDto | null>(() => {
    const id = this.selection.selectedDoorId();
    const node = id !== null ? this.byId().get(id) : undefined;
    if (!node) {
      return null;
    }
    return {
      id: node.id,
      name: node.name,
      description: node.description,
      representativeAyahText: node.representativeAyahText,
      aliases: [...node.aliases],
      parentId: node.parentId,
      sectionId: node.sectionId,
      orderValue: node.orderValue,
      globalOrderValue: node.globalOrderValue,
      version: node.version,
    };
  });

  readonly modalOpen = signal(false);
  readonly modalDoor = signal<AbwabDoorDto | null>(null);
  readonly modalParentId = signal<number | null>(null);
  readonly modalParentName = signal<string | null>(null);

  openCreateRoot(): void {
    if (!this.permissions.canCreateDoor()) {
      return;
    }
    this.modalDoor.set(null);
    this.modalParentId.set(null);
    this.modalParentName.set(null);
    this.modalOpen.set(true);
  }

  openCreateChild(): void {
    if (!this.permissions.canCreateDoor()) {
      return;
    }
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.modalDoor.set(null);
    this.modalParentId.set(door.id);
    this.modalParentName.set(door.name);
    this.modalOpen.set(true);
  }

  openEdit(): void {
    if (!this.permissions.canEditDoor()) {
      return;
    }
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.modalDoor.set(door);
    this.modalParentId.set(null);
    this.modalParentName.set(null);
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
  }

  readonly archiveConfirm = signal<'single' | 'bulk' | null>(null);
  readonly archiveBusy = signal(false);
  readonly archiveError = signal<string | null>(null);

  readonly archiveConfirming = computed(() => this.archiveConfirm() === 'single');
  readonly bulkArchiveConfirming = computed(() => this.archiveConfirm() === 'bulk');

  private readonly archiveFromContextMenu = signal(false);
  readonly archiveCameFromContextMenu = this.archiveFromContextMenu.asReadonly();

  requestArchive(): void {
    if (this.permissions.canArchiveDoor() && this.selectedDoor()) {
      this.openArchiveConfirm('single');
    }
  }

  requestBulkArchive(): void {
    if (this.permissions.canArchiveDoor() && this.selection.bulkCount() > 0) {
      this.openArchiveConfirm('bulk');
    }
  }

  archiveConfirmMessage(): string {
    const door = this.selectedDoor();
    return door ? this.writeController.archiveConfirmMessageFor(door.id) : '';
  }

  bulkArchiveConfirmMessage(): string {
    return this.writeController.bulkArchiveConfirmMessage(Array.from(this.selection.bulkSet().keys()));
  }

  confirmArchive(onSuccess: () => void): void {
    const door = this.selectedDoor();
    if (!this.permissions.canArchiveDoor() || !door || this.archiveBusy()) {
      if (!this.permissions.canArchiveDoor()) {
        this.closeArchiveConfirm();
      }
      return;
    }
    this.beginArchiveWrite();
    this.writeController.archiveDoor(door.id, door.version).subscribe((outcome) => {
      this.archiveBusy.set(false);
      if (outcome.kind !== 'success') {
        this.archiveError.set(outcome.message);
        return;
      }
      this.archiveConfirm.set(null);
      this.selection.clearSelection();
      onSuccess();
    });
  }

  confirmBulkArchive(onSuccess: () => void): void {
    if (!this.permissions.canArchiveDoor() || this.archiveBusy()) {
      if (!this.permissions.canArchiveDoor()) {
        this.closeArchiveConfirm();
      }
      return;
    }
    this.beginArchiveWrite();
    this.writeController.bulkArchiveDoors().subscribe((outcome) => {
      this.archiveBusy.set(false);
      if (outcome.kind !== 'success') {
        this.archiveError.set(outcome.message);
        return;
      }
      this.archiveConfirm.set(null);
      onSuccess();
    });
  }

  cancelArchiveConfirm(): void {
    this.closeArchiveConfirm();
  }

  cancelBulkArchiveConfirm(): void {
    this.closeArchiveConfirm();
  }

  private openArchiveConfirm(kind: 'single' | 'bulk'): void {
    this.archiveError.set(null);
    this.archiveBusy.set(false);
    this.archiveFromContextMenu.set(false);
    this.archiveConfirm.set(kind);
  }

  private beginArchiveWrite(): void {
    this.archiveError.set(null);
    this.archiveBusy.set(true);
  }

  private closeArchiveConfirm(): void {
    if (this.archiveBusy()) {
      return;
    }
    this.archiveConfirm.set(null);
    this.archiveError.set(null);
  }

  readonly movePickerOpen = signal(false);
  readonly moveBusy = signal(false);
  readonly moveError = signal<string | null>(null);
  private readonly moveDoorIds = signal<readonly number[]>([]);

  readonly moveExcludedIds = computed(() => {
    const byId = this.byId();
    const result = new Set<number>();
    const walk = (node: AbwabNode): void => {
      if (result.has(node.id)) return;
      result.add(node.id);
      node.children.forEach(walk);
    };
    for (const id of this.moveDoorIds()) {
      const node = byId.get(id);
      if (node) walk(node);
    }
    return result;
  });

  readonly moveSectionIds = computed<readonly number[]>(() => {
    const byId = this.byId();
    return this.moveDoorIds()
      .map((id) => byId.get(id)?.sectionId)
      .filter((sectionId): sectionId is number => sectionId !== undefined);
  });

  readonly moveTitleText = computed(() => {
    const ids = this.moveDoorIds();
    if (ids.length === 1) {
      return ABWAB_LABELS.movePickerTitleSingle(this.byId().get(ids[0])?.name ?? '');
    }
    return ABWAB_LABELS.movePickerTitleBulk(ids.length);
  });

  openMovePicker(): void {
    if (!this.permissions.canMoveDoor()) {
      return;
    }
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.moveDoorIds.set([door.id]);
    this.moveBusy.set(false);
    this.moveError.set(null);
    this.movePickerOpen.set(true);
  }

  openBulkMovePicker(): void {
    if (!this.permissions.canMoveDoor()) {
      return;
    }
    const ids = Array.from(this.selection.bulkSet().keys());
    if (ids.length === 0) {
      return;
    }
    this.moveDoorIds.set(ids);
    this.moveBusy.set(false);
    this.moveError.set(null);
    this.movePickerOpen.set(true);
  }

  closeMovePicker(): void {
    this.movePickerOpen.set(false);
    this.moveBusy.set(false);
    this.moveError.set(null);
  }

  confirmMove(destination: AbwabMoveDestination, onSuccess: () => void): void {
    if (!this.permissions.canMoveDoor() || this.moveBusy()) {
      return;
    }
    const ids = this.moveDoorIds();
    this.moveBusy.set(true);
    this.moveError.set(null);
    if (ids.length === 1) {
      const node = this.byId().get(ids[0]);
      if (!node) {
        this.moveBusy.set(false);
        return;
      }
      this.writeController
        .moveDoor(ids[0], {
          targetParentId: destination.targetParentId,
          targetSectionId: destination.targetSectionId,
          version: node.version,
        })
        .subscribe((outcome) => this.handleMoveOutcome(outcome, onSuccess));
      return;
    }
    this.writeController
      .bulkMoveDoors(destination.targetParentId, destination.targetSectionId)
      .subscribe((outcome) => this.handleMoveOutcome(outcome, onSuccess));
  }

  private handleMoveOutcome(outcome: AbwabWriteOutcome<unknown>, onSuccess: () => void): void {
    this.moveBusy.set(false);
    if (outcome.kind === 'success') {
      onSuccess();
      return;
    }
    this.moveError.set(outcome.message);
  }

  private readonly restoreDoorId = signal<number | null>(null);

  readonly restoreTarget = computed(() => {
    const id = this.restoreDoorId();
    return id === null ? null : (this.byId().get(id) ?? null);
  });

  readonly restoreAncestors = computed<readonly AbwabNode[]>(() => {
    const target = this.restoreTarget();
    if (!target) {
      return NO_ANCESTORS;
    }
    const byId = this.byId();
    const chain: AbwabNode[] = [];
    let current = target.parentId;
    while (current !== null) {
      const node = byId.get(current);
      if (!node) {
        break;
      }
      chain.unshift(node);
      current = node.parentId;
    }
    return chain;
  });

  openRestoreModal(id: number): void {
    if (!this.permissions.canRestoreDoor() || !this.byId().has(id)) {
      return;
    }
    this.restoreDoorId.set(id);
  }

  closeRestoreModal(): void {
    this.restoreDoorId.set(null);
  }

  readonly sectionsModalOpen = signal(false);

  openSectionsModal(): void {
    if (!this.permissions.canManageSections()) {
      return;
    }
    this.sectionsModalOpen.set(true);
  }

  closeSectionsModal(): void {
    this.sectionsModalOpen.set(false);
  }

  readonly createSection = (name: string) =>
    this.permissions.canCreateSection()
      ? this.sectionsController.createSection(name)
      : of(abwabPermissionDenied());
  readonly renameSection = (id: number, name: string, version: number) =>
    this.permissions.canEditSection()
      ? this.sectionsController.renameSection(id, name, version)
      : of(abwabPermissionDenied());
  readonly deleteSection = (id: number) =>
    this.permissions.canDeleteSection() ? this.sectionsController.deleteSection(id) : of(abwabPermissionDenied());
  readonly reorderSection = (id: number, position: number, version: number) =>
    this.permissions.canReorderSection()
      ? this.sectionsController.reorderSection(id, position, version)
      : of(abwabPermissionDenied());

  readonly relationsModalOpen = signal(false);
  readonly relationsAnchorPickMode = signal(false);
  private readonly relationsAnchorId = signal<number | null>(null);

  readonly relationsAnchorDoorId = this.relationsAnchorId.asReadonly();

  readonly relationsAnchorName = computed(() => {
    const id = this.relationsAnchorId();
    return id === null ? '' : (this.byId().get(id)?.name ?? '');
  });

  readonly relationsAnchorCount = computed(() => {
    const id = this.relationsAnchorId();
    return id === null ? 0 : (this.byId().get(id)?.relationCount ?? 0);
  });

  readonly relationsBulkTargets = computed(() => {
    const byId = this.byId();
    return Array.from(this.selection.bulkSet().keys(), (id) => ({
      id,
      name: byId.get(id)?.name ?? String(id),
    }));
  });

  openRelations(): void {
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.relationsAnchorPickMode.set(false);
    this.relationsAnchorId.set(door.id);
    this.relationsModalOpen.set(true);
  }

  openBulkRelations(): void {
    if (!this.permissions.canCreateRelation() || this.selection.bulkCount() === 0) {
      return;
    }
    this.relationsAnchorPickMode.set(true);
    this.relationsAnchorId.set(null);
    this.relationsModalOpen.set(true);
  }

  closeRelationsModal(): void {
    this.relationsModalOpen.set(false);
  }

  readonly loadRelations = (doorId: number) => this.relationsController.loadFor(doorId);
  readonly refetchRelations = (doorId: number) => this.relationsController.refetchFor(doorId);
  readonly addRelations = (
    anchorDoorId: number,
    kind: AbwabRelationKind,
    direction: AbwabRelationDirectionKind | null,
    targetDoorIds: readonly number[],
  ) =>
    this.permissions.canCreateRelation()
      ? this.relationsController.addRelations(anchorDoorId, kind, direction, targetDoorIds)
      : of(abwabPermissionDenied());
  readonly deleteRelation = (relationId: number) =>
    this.permissions.canDeleteRelation() ? this.relationsController.deleteRelation(relationId) : of(abwabPermissionDenied());

  readonly contextMenuDoorId = signal<number | null>(null);
  readonly contextMenuPosition = signal<{ x: number; y: number }>({ x: 0, y: 0 });
  readonly contextMenuKind = signal<'details' | 'operations'>('operations');

  requestContextMenu(id: number, kind: 'details' | 'operations'): void {
    this.contextMenuKind.set(kind);
    this.contextMenuDoorId.set(id);
  }

  setContextMenuPosition(x: number, y: number): void {
    this.contextMenuPosition.set({ x, y });
  }

  closeContextMenu(): void {
    this.contextMenuDoorId.set(null);
  }

  closeUnavailableWriteState(): void {
    if (this.modalOpen() && (this.modalDoor() === null ? !this.permissions.canCreateDoor() : !this.permissions.canEditDoor())) {
      this.closeModal();
    }
    if (!this.permissions.canMoveDoor()) {
      this.closeMovePicker();
    }
    if (!this.permissions.canRestoreDoor()) {
      this.closeRestoreModal();
    }
    if (!this.permissions.canManageSections()) {
      this.closeSectionsModal();
    }
    if (!this.permissions.canArchiveDoor()) {
      this.closeArchiveConfirm();
    }
    if (!this.permissions.canUseBulkMode()) {
      this.selection.setBulkMode(false);
    }
  }

  ctxEdit(onActed?: ContextActionCallback): void {
    this.runContextAction(() => this.openEdit(), onActed);
  }

  ctxAddChild(onActed?: ContextActionCallback): void {
    this.runContextAction(() => this.openCreateChild(), onActed);
  }

  ctxMove(onActed?: ContextActionCallback): void {
    this.runContextAction(() => this.openMovePicker(), onActed);
  }

  ctxArchive(): void {
    this.runContextAction(() => {
      this.requestArchive();
      this.archiveFromContextMenu.set(this.archiveConfirm() === 'single');
    });
  }

  ctxRelations(onActed?: ContextActionCallback): void {
    this.runContextAction(() => this.openRelations(), onActed);
  }

  private runContextAction(action: () => void, onActed?: ContextActionCallback): void {
    const id = this.contextMenuDoorId();
    this.closeContextMenu();
    if (id === null) {
      return;
    }
    const node = this.byId().get(id);
    if (!node) {
      return;
    }
    this.selection.select(id, node.version);
    action();
    onActed?.(id);
  }
}
