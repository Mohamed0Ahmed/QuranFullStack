import { Injectable, computed, inject, signal } from '@angular/core';

import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabPageOverlaysController } from './abwab-page-overlays.controller';
import { AbwabModalKind, AbwabModalState, isDoorDependentAbwabModalKind } from '../models/abwab.models';
import { AbwabPermissionsController } from './abwab-permissions.controller';

export const DOOR_MODAL_KINDS: readonly AbwabModalKind[] = ['create', 'child', 'edit'];

interface OpenedModal {
  readonly kind: AbwabModalKind;
  readonly doorId: number | null;
}

@Injectable()
export class AbwabModalUrlController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly overlays = inject(AbwabPageOverlaysController);
  private readonly permissions = inject(AbwabPermissionsController);

  private readonly modalSignal = signal<AbwabModalState | null>(null);
  private readonly doorSignal = signal<number | null>(null);

  readonly modal = this.modalSignal.asReadonly();

  readonly unauthorizedWriteModal = computed<AbwabModalState | null>(() => {
    const modal = this.modalSignal();
    return modal !== null && !this.permissions.canOpenModal(modal.kind) ? modal : null;
  });

  private opened: OpenedModal | null = null;

  readonly restorableModal = computed<AbwabModalState | null>(() => {
    const modal = this.modalSignal();
    if (modal === null || !modal.closed) {
      return null;
    }
    if (modal.subjectDoorId !== null) {
      const node = this.facade.snapshot()?.byId.get(modal.subjectDoorId);
      return !!node && !node.isArchived ? modal : null;
    }
    return this.canOpen(modal.kind) ? modal : null;
  });

  syncFromUrl(modal: AbwabModalState | null, door: number | null): void {
    this.modalSignal.set(modal);
    this.doorSignal.set(door);

    const opened = this.opened;
    if (opened === null) {
      return;
    }
    const stillOpen = modal !== null && !modal.closed && modal.kind === opened.kind;
    if (stillOpen && this.sameSubject(opened, door)) {
      return;
    }
    this.closeOverlayFor(opened.kind);
    this.opened = null;
  }

  reconcileOpen(): void {
    const modal = this.modalSignal();
    if (modal === null || modal.closed) {
      return;
    }
    if (this.opened !== null || this.isOverlayOpen(modal.kind) || !this.canOpen(modal.kind)) {
      return;
    }
    this.open(modal.kind);
    this.trackOpen(modal.kind, this.doorSignal());
  }

  open(kind: AbwabModalKind): void {
    switch (kind) {
      case 'create':
        this.overlays.openCreateRoot();
        return;
      case 'child':
        this.overlays.openCreateChild();
        return;
      case 'edit':
        this.overlays.openEdit();
        return;
      case 'move':
        this.overlays.openMovePicker();
        return;
      case 'sections':
        this.overlays.openSectionsModal();
        return;
      case 'relations':
        this.overlays.openRelations();
        return;
    }
  }

  trackOpen(kind: AbwabModalKind, doorId: number | null): void {
    this.opened = { kind, doorId: isDoorDependentAbwabModalKind(kind) ? doorId : null };
  }

  urlBackedKind(kinds: readonly AbwabModalKind[]): AbwabModalKind | null {
    const opened = this.opened;
    return opened !== null && kinds.includes(opened.kind) ? opened.kind : null;
  }

  releaseTracking(): void {
    this.opened = null;
  }

  clearUnauthorizedWriteModal(): void {
    const modal = this.unauthorizedWriteModal();
    if (modal === null) {
      return;
    }
    this.closeOverlayFor(modal.kind);
    this.opened = null;
    this.modalSignal.set(null);
  }

  private canOpen(kind: AbwabModalKind): boolean {
    if (!this.permissions.canOpenModal(kind)) {
      return false;
    }
    if (!isDoorDependentAbwabModalKind(kind)) {
      return this.facade.snapshot() !== null;
    }
    const doorId = this.doorSignal();
    if (doorId === null) {
      return false;
    }
    const node = this.facade.snapshot()?.byId.get(doorId);
    return !!node && !node.isArchived && this.overlays.selectedDoor()?.id === doorId;
  }

  private sameSubject(opened: OpenedModal, door: number | null): boolean {
    return !isDoorDependentAbwabModalKind(opened.kind) || opened.doorId === door;
  }

  private isOverlayOpen(kind: AbwabModalKind): boolean {
    switch (kind) {
      case 'create':
      case 'child':
      case 'edit':
        return this.overlays.modalOpen();
      case 'move':
        return this.overlays.movePickerOpen();
      case 'sections':
        return this.overlays.sectionsModalOpen();
      case 'relations':
        return this.overlays.relationsModalOpen();
    }
  }

  private closeOverlayFor(kind: AbwabModalKind): void {
    switch (kind) {
      case 'create':
      case 'child':
      case 'edit':
        this.overlays.closeModal();
        return;
      case 'move':
        this.overlays.closeMovePicker();
        return;
      case 'sections':
        this.overlays.closeSectionsModal();
        return;
      case 'relations':
        this.overlays.closeRelationsModal();
        return;
    }
  }
}
