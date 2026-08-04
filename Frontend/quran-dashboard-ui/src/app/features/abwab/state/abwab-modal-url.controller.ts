import { Injectable, computed, inject, signal } from '@angular/core';

import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabPageOverlaysController } from './abwab-page-overlays.controller';
import { AbwabModalKind, AbwabModalState, isDoorDependentAbwabModalKind } from '../models/abwab.models';

/** The three modes the one door modal serves — which of them it is showing is the private
 * signal set the opener wrote, so the URL kind is the page's own record of it. */
export const DOOR_MODAL_KINDS: readonly AbwabModalKind[] = ['create', 'child', 'edit'];

/** What the page holds open **on the URL's behalf**. The subject is tracked beside the kind so
 * an emission that keeps the kind but moves `door=` still reads as a different overlay rather
 * than as the same one — without it the state machine would rely on the modal backdrop being
 * the only thing that can write `door=`, which is true today and is not a contract. */
interface OpenedModal {
  readonly kind: AbwabModalKind;
  readonly doorId: number | null;
}

@Injectable()
export class AbwabModalUrlController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly overlays = inject(AbwabPageOverlaysController);

  private readonly modalSignal = signal<AbwabModalState | null>(null);
  private readonly doorSignal = signal<number | null>(null);

  readonly modal = this.modalSignal.asReadonly();

  private opened: OpenedModal | null = null;

  readonly restorableModal = computed<AbwabModalState | null>(() => {
    const modal = this.modalSignal();
    if (modal === null || !modal.closed) {
      return null;
    }
    // A carried subject is checked against itself, not against `door=`: the whole point of the
    // id is that `door=` has moved on (a reveal put the target there). The liveness rule is the
    // same one `canOpen` applies, and an id naming a dead or archived door leaves the key inert
    // — no control, no rewrite — exactly as a dead `door=` already does. Unlike the plain
    // `-closed` forms, this subject is **pinned**: selecting another door does not move it.
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
    // `isOverlayOpen` is the guard for the overlays a bulk gesture shares (the move picker and
    // the relations modal): those are opened without ever writing the key, so `opened` is null
    // for them, and restoring a retained key while one is on screen would convert it to
    // single-subject mode under the user.
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

  private canOpen(kind: AbwabModalKind): boolean {
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
