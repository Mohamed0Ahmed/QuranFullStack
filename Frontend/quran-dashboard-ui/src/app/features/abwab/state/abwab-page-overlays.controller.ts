import { Injectable, computed, inject, signal } from '@angular/core';

import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabWriteController } from './abwab-write.controller';
import { AbwabSectionsController } from './abwab-sections.controller';
import { AbwabNode } from '../models/abwab.models';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabMoveDestination } from '../components/abwab-move-picker/abwab-move-picker.component';

/**
 * Owns every overlay's open/closed state and orchestration for `AbwabPageComponent`
 * (door modal, single/bulk archive confirm, move picker, sections modal, row context
 * menu) — split out once the page shell's own file (URL ⇄ state wiring + composition)
 * approached the component-TS soft threshold (`FRONTEND_STRUCTURE.md`'s Large Page
 * Split guidance). This is state/orchestration only, not a template; the page still
 * renders every dialog and reads/calls into this controller. URL-side effects (writing
 * `door=null` back after an archive) stay the page's job via the optional callbacks
 * below — this controller has no `Router`/`ActivatedRoute` dependency.
 */
@Injectable({ providedIn: 'root' })
export class AbwabPageOverlaysController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly selection = inject(AbwabSelectionStore);
  private readonly writeController = inject(AbwabWriteController);
  private readonly sectionsController = inject(AbwabSectionsController);

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
      version: node.version,
    };
  });

  // Door modal (add/edit)
  readonly modalOpen = signal(false);
  readonly modalDoor = signal<AbwabDoorDto | null>(null);
  readonly modalParentId = signal<number | null>(null);
  readonly modalParentName = signal<string | null>(null);

  openCreateRoot(): void {
    this.modalDoor.set(null);
    this.modalParentId.set(null);
    this.modalParentName.set(null);
    this.modalOpen.set(true);
  }

  openCreateChild(): void {
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

  // Single-door archive confirm
  readonly archiveConfirming = signal(false);

  requestArchive(): void {
    if (this.selectedDoor()) {
      this.archiveConfirming.set(true);
    }
  }

  archiveConfirmMessage(): string {
    const door = this.selectedDoor();
    return door ? this.writeController.archiveConfirmMessageFor(door.id) : '';
  }

  confirmArchive(onSuccess: () => void): void {
    const door = this.selectedDoor();
    this.archiveConfirming.set(false);
    if (!door) {
      return;
    }
    this.writeController.archiveDoor(door.id, door.version).subscribe((outcome) => {
      if (outcome.kind === 'success') {
        this.selection.clearSelection();
        onSuccess();
      }
    });
  }

  cancelArchiveConfirm(): void {
    this.archiveConfirming.set(false);
  }

  // Bulk archive confirm
  readonly bulkArchiveConfirming = signal(false);

  requestBulkArchive(): void {
    if (this.selection.bulkCount() > 0) {
      this.bulkArchiveConfirming.set(true);
    }
  }

  bulkArchiveConfirmMessage(): string {
    return this.writeController.bulkArchiveConfirmMessage(Array.from(this.selection.bulkSet().keys()));
  }

  confirmBulkArchive(): void {
    this.bulkArchiveConfirming.set(false);
    this.writeController.bulkArchiveDoors().subscribe();
  }

  cancelBulkArchiveConfirm(): void {
    this.bulkArchiveConfirming.set(false);
  }

  // Move picker (single and bulk share it)
  readonly movePickerOpen = signal(false);
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

  readonly moveTitleText = computed(() => {
    const ids = this.moveDoorIds();
    if (ids.length === 1) {
      return ABWAB_LABELS.movePickerTitleSingle(this.byId().get(ids[0])?.name ?? '');
    }
    return ABWAB_LABELS.movePickerTitleBulk(ids.length);
  });

  openMovePicker(): void {
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.moveDoorIds.set([door.id]);
    this.movePickerOpen.set(true);
  }

  openBulkMovePicker(): void {
    const ids = Array.from(this.selection.bulkSet().keys());
    if (ids.length === 0) {
      return;
    }
    this.moveDoorIds.set(ids);
    this.movePickerOpen.set(true);
  }

  closeMovePicker(): void {
    this.movePickerOpen.set(false);
  }

  confirmMove(destination: AbwabMoveDestination): void {
    this.movePickerOpen.set(false);
    const ids = this.moveDoorIds();
    if (ids.length === 1) {
      const node = this.byId().get(ids[0]);
      if (!node) {
        return;
      }
      this.writeController
        .moveDoor(ids[0], {
          targetParentId: destination.targetParentId,
          targetSectionId: destination.targetSectionId,
          version: node.version,
        })
        .subscribe();
      return;
    }
    this.writeController.bulkMoveDoors(destination.targetParentId, destination.targetSectionId).subscribe();
  }

  // Sections modal
  readonly sectionsModalOpen = signal(false);

  openSectionsModal(): void {
    this.sectionsModalOpen.set(true);
  }

  closeSectionsModal(): void {
    this.sectionsModalOpen.set(false);
  }

  readonly createSection = (name: string) => this.sectionsController.createSection(name);
  readonly renameSection = (id: number, name: string, version: number) =>
    this.sectionsController.renameSection(id, name, version);
  readonly deleteSection = (id: number) => this.sectionsController.deleteSection(id);

  // Row context menu (T511) — right-click/keyboard both funnel through `menuRequested`.
  readonly contextMenuDoorId = signal<number | null>(null);
  readonly contextMenuPosition = signal<{ x: number; y: number }>({ x: 0, y: 0 });

  requestContextMenu(id: number): void {
    this.contextMenuDoorId.set(id);
  }

  setContextMenuPosition(x: number, y: number): void {
    this.contextMenuPosition.set({ x, y });
  }

  closeContextMenu(): void {
    this.contextMenuDoorId.set(null);
  }

  ctxEdit(): void {
    this.runContextAction(() => this.openEdit());
  }

  ctxAddChild(): void {
    this.runContextAction(() => this.openCreateChild());
  }

  ctxMove(): void {
    this.runContextAction(() => this.openMovePicker());
  }

  ctxArchive(): void {
    this.runContextAction(() => this.requestArchive());
  }

  private runContextAction(action: () => void): void {
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
  }
}
