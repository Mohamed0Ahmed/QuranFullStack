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
  /** `null` for the door-independent kinds, whose subject is not `door=` at all. */
  readonly doorId: number | null;
}

/**
 * Audit item 11's page-side machinery for the seventh URL key: which overlay the URL currently
 * owns, whether a parsed key may be acted on, and the two halves of reconciliation. Split out
 * of `AbwabPageComponent` on the `abwab-page-overlays.controller.ts` precedent, and with the
 * same boundary — **no `Router`/`ActivatedRoute` dependency**. Every navigation this state
 * implies stays the page's job: the page feeds URL values in through `syncFromUrl`, asks this
 * controller what it may do, and writes the key itself.
 *
 * **Deliberately not `providedIn: 'root'`** — `AbwabPageComponent` provides it, so its lifetime
 * is the page's, exactly like the overlay state it tracks.
 */
@Injectable()
export class AbwabModalUrlController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly overlays = inject(AbwabPageOverlaysController);

  private readonly modalSignal = signal<AbwabModalState | null>(null);
  private readonly doorSignal = signal<number | null>(null);

  /** The parsed key, for the page's settle-gated open effect to depend on. */
  readonly modal = this.modalSignal.asReadonly();

  private opened: OpenedModal | null = null;

  /** The retained state the restore control renders from — null while the key is absent, open,
   * or pointing at a subject that cannot be restored (the same live guard `reconcileOpen`
   * applies), so an inert key shows no affordance at all. */
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

  /**
   * The close half of reconciliation, driven by the param emission and **not** by an effect:
   * between a gesture's navigation and the emission that lands it the URL still says nothing,
   * and an effect that read that as "close everything" would shut the modal the click just
   * opened. The emission fires exactly when the URL genuinely changed, which is the only
   * moment a close can be inferred from it.
   *
   * A URL-driven close goes through the overlay's own `close` setter and therefore **bypasses
   * the door and sections modals' unsaved-changes confirm** — deliberate: the URL is the single
   * source of truth, so a Back that says the overlay is closed closes it, and restoring returns
   * a pristine overlay rather than the draft (README, «Restoring reopens the overlay, not a
   * draft»).
   */
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

  /**
   * The open half, and only that. Acts on the delta between what the page holds open and what
   * the URL asks for, which is what makes the echo of a gesture's own patch a no-op —
   * re-opening an already-open door modal would reset the form under the user.
   */
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

  /** Opens an overlay in response to a user gesture. The page writes the key in the same patch
   * as any selection the gesture changes; this only puts the overlay on screen. */
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

  /** Records an overlay as URL-backed. Called before the page navigates, so the emission that
   * lands the gesture's own patch is recognised as an echo rather than as a change. */
  trackOpen(kind: AbwabModalKind, doorId: number | null): void {
    this.opened = { kind, doorId: isDoorDependentAbwabModalKind(kind) ? doorId : null };
  }

  /** `kinds` is what the closing overlay can be holding on the URL's behalf. A mismatch means a
   * bulk gesture opened it — the key was never written, so closing must not write one either. */
  urlBackedKind(kinds: readonly AbwabModalKind[]): AbwabModalKind | null {
    const opened = this.opened;
    return opened !== null && kinds.includes(opened.kind) ? opened.kind : null;
  }

  /** Drops the tracking without touching an overlay — for the paths that close it themselves
   * (every `(closed)` handler) and for the reveal, whose single patch discards the key. */
  releaseTracking(): void {
    this.opened = null;
  }

  /** Door-dependent kinds need a subject that is bound **and live**. `byId` holds archived nodes
   * and the `door=` effect checks presence only, so a crafted `modal=edit` on an archived door
   * would otherwise open an editor over something the tree does not show. The fail-closed
   * outcome is the one a dead `door=` already produces: nothing happens and the key sits
   * inert. */
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
