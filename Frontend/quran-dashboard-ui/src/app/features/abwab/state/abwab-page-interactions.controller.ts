import { Injectable, computed, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabWriteController } from './abwab-write.controller';
import { AbwabPageOverlaysController } from './abwab-page-overlays.controller';
import { AbwabModalUrlController, DOOR_MODAL_KINDS } from './abwab-modal-url.controller';
import { AbwabPermissionsController } from './abwab-permissions.controller';
import { AbwabRevealController } from './abwab-reveal.controller';
import { buildAbwabQueryParams } from './abwab-url-sync';
import {
  ABWAB_ORDER_SCOPE_TO_WIRE,
  ABWAB_QUERY_KEYS,
  AbwabModalKind,
  AbwabMoveDestination,
  AbwabNode,
  AbwabOrderScope,
  AbwabView,
} from '../models/abwab.models';

type FocusCallback = () => void;

interface AbwabTreeMenuRequest {
  readonly id: number;
  readonly x: number;
  readonly y: number;
}

@Injectable()
export class AbwabPageInteractionsController {
  private readonly facade = inject(AbwabSnapshotFacade);
  private readonly selection = inject(AbwabSelectionStore);
  private readonly writeController = inject(AbwabWriteController);
  private readonly overlays = inject(AbwabPageOverlaysController);
  private readonly modalUrl = inject(AbwabModalUrlController);
  private readonly permissions = inject(AbwabPermissionsController);
  private readonly reveal = inject(AbwabRevealController);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly byId = computed(() => this.facade.snapshot()?.byId ?? new Map<number, AbwabNode>());
  private doorModalCommitted = false;

  onSectionChanged(sectionId: number | null): void {
    this.updateQueryParams(buildAbwabQueryParams({ section: sectionId }));
  }

  onViewChanged(view: AbwabView): void {
    this.updateQueryParams(buildAbwabQueryParams({ view }));
  }

  onSearchQueryChanged(q: string): void {
    this.updateQueryParams(buildAbwabQueryParams({ q }));
  }

  onCardDrilled(id: number): void {
    this.updateQueryParams(buildAbwabQueryParams({ card: id }));
  }

  onCardCrumbSelected(id: number | null): void {
    this.updateQueryParams(buildAbwabQueryParams({ card: id }));
  }

  onArchiveToggle(archiveActive: boolean): void {
    this.updateQueryParams(buildAbwabQueryParams({ archive: !archiveActive }));
  }

  onTreeSelected(doorId: number): void {
    const node = this.facade.snapshot()?.byId.get(doorId);
    if (!node) {
      return;
    }
    this.selection.select(doorId, node.version);
    this.updateQueryParams(buildAbwabQueryParams({ door: doorId }));
  }

  onClearSelection(): void {
    this.selection.clearSelection();
    this.updateQueryParams(buildAbwabQueryParams({ door: null }));
  }

  onBulkModeToggled(on: boolean): void {
    if (!this.permissions.canUseBulkMode()) {
      this.selection.setBulkMode(false);
      return;
    }
    this.selection.setBulkMode(on);
  }

  onBulkToggled(id: number): void {
    if (!this.permissions.canUseBulkMode()) {
      return;
    }
    const node = this.byId().get(id);
    if (!node) {
      return;
    }
    this.selection.toggleBulk(id, node.version);
  }

  onBulkClearRequested(): void {
    this.selection.clearBulk();
  }

  confirmArchiveAndClearUrl(onSuccess: FocusCallback): void {
    if (!this.permissions.canArchiveDoor()) {
      return;
    }
    this.overlays.confirmArchive(() => {
      this.updateQueryParams(buildAbwabQueryParams({ door: null }));
      onSuccess();
    });
  }

  onBulkArchiveConfirmed(onSuccess: FocusCallback): void {
    if (!this.permissions.canArchiveDoor()) {
      return;
    }
    this.overlays.confirmBulkArchive(onSuccess);
  }

  onArchiveConfirmCancelled(onFocus: FocusCallback): void {
    const cameFromContextMenu = this.overlays.archiveCameFromContextMenu();
    this.overlays.cancelArchiveConfirm();
    if (cameFromContextMenu) {
      onFocus();
    }
  }

  onBulkArchiveConfirmCancelled(): void {
    this.overlays.cancelBulkArchiveConfirm();
  }

  onOrderCommitted(event: { id: number; position: number; scope: AbwabOrderScope }): void {
    if (!this.permissions.canReorderDoor()) {
      return;
    }
    const node = this.byId().get(event.id);
    if (!node) {
      return;
    }
    this.writeController
      .reorderDoor(event.id, {
        position: event.position,
        scope: ABWAB_ORDER_SCOPE_TO_WIRE[event.scope],
        version: node.version,
      })
      .subscribe();
  }

  onRestoreRequested(id: number): void {
    if (!this.permissions.canRestoreDoor()) {
      return;
    }
    this.overlays.openRestoreModal(id);
  }

  onDoorRestored(onFocus: FocusCallback): void {
    this.overlays.closeRestoreModal();
    onFocus();
  }

  onRelationsRequested(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!node) {
      return;
    }
    this.selection.select(doorId, node.version);
    this.overlays.openRelations();
    this.commitModalOpen('relations', doorId);
  }

  onRevealRequested(doorId: number, activeSectionId: number | null, view: AbwabView): void {
    this.reveal.onRevealRequested(doorId, activeSectionId, view);
  }

  onMenuRequested(request: AbwabTreeMenuRequest): void {
    this.overlays.setContextMenuPosition(request.x, request.y);
    this.overlays.requestContextMenu(request.id);
  }

  onCreateRootRequested(): void {
    if (!this.permissions.canCreateDoor()) {
      return;
    }
    this.modalUrl.open('create');
    this.commitModalOpen('create');
  }

  onSectionsRequested(): void {
    if (!this.permissions.canManageSections()) {
      return;
    }
    this.modalUrl.open('sections');
    this.commitModalOpen('sections');
  }

  onAddChildRequested(): void {
    if (!this.permissions.canCreateDoor()) {
      return;
    }
    this.openOnSelectedDoor('child');
  }

  onEditRequested(): void {
    if (!this.permissions.canEditDoor()) {
      return;
    }
    this.openOnSelectedDoor('edit');
  }

  onMoveRequested(): void {
    if (!this.permissions.canMoveDoor()) {
      return;
    }
    this.openOnSelectedDoor('move');
  }

  onRelationsOpenRequested(): void {
    this.openOnSelectedDoor('relations');
  }

  onTreeAddChildRequested(doorId: number): void {
    if (!this.permissions.canCreateDoor()) {
      return;
    }
    const node = this.byId().get(doorId);
    if (!node) {
      return;
    }
    this.modalUrl.open('child');
    this.commitModalOpen('child', doorId);
  }

  onCtxEdit(): void {
    if (!this.permissions.canEditDoor()) {
      return;
    }
    this.overlays.ctxEdit((id) => this.commitModalOpen('edit', id));
  }

  onCtxAddChild(): void {
    if (!this.permissions.canCreateDoor()) {
      return;
    }
    this.overlays.ctxAddChild((id) => this.commitModalOpen('child', id));
  }

  onCtxMove(): void {
    if (!this.permissions.canMoveDoor()) {
      return;
    }
    this.overlays.ctxMove((id) => this.commitModalOpen('move', id));
  }

  onCtxRelations(): void {
    this.overlays.ctxRelations((id) => this.commitModalOpen('relations', id));
  }

  onDoorModalSaved(): void {
    this.doorModalCommitted = true;
  }

  onDoorModalClosed(onRestoreFocus: FocusCallback): void {
    const committed = this.doorModalCommitted;
    this.doorModalCommitted = false;
    this.closeUrlBackedModal(DOOR_MODAL_KINDS, () => this.overlays.closeModal(), onRestoreFocus, committed);
  }

  onMovePickerClosed(onRestoreFocus: FocusCallback): void {
    this.closeUrlBackedModal(['move'], () => this.overlays.closeMovePicker(), onRestoreFocus);
  }

  onMoveConfirmed(destination: AbwabMoveDestination): void {
    if (!this.permissions.canMoveDoor()) {
      this.closeUrlBackedModal(['move'], () => this.overlays.closeMovePicker(), () => undefined, true);
      return;
    }
    this.closeUrlBackedModal(['move'], () => this.overlays.confirmMove(destination), () => undefined, true);
  }

  onSectionsModalClosed(onRestoreFocus: FocusCallback): void {
    this.closeUrlBackedModal(['sections'], () => this.overlays.closeSectionsModal(), onRestoreFocus);
  }

  onRelationsModalClosed(onRestoreFocus: FocusCallback): void {
    this.closeUrlBackedModal(['relations'], () => this.overlays.closeRelationsModal(), onRestoreFocus);
  }

  onModalRestoreRequested(): void {
    const retained = this.modalUrl.restorableModal();
    if (retained === null || !this.permissions.canOpenModal(retained.kind)) {
      return;
    }
    this.updateQueryParams(
      buildAbwabQueryParams({
        ...(retained.subjectDoorId === null ? {} : { door: retained.subjectDoorId }),
        modal: { kind: retained.kind, closed: false, subjectDoorId: null },
      }),
    );
  }

  onModalDiscardRequested(onFocus: FocusCallback): void {
    this.updateQueryParams(buildAbwabQueryParams({ modal: null }), true);
    this.focusQueued(onFocus);
  }

  clearUnauthorizedWriteModal(): void {
    if (this.modalUrl.unauthorizedWriteModal() === null) {
      return;
    }
    this.modalUrl.clearUnauthorizedWriteModal();
    this.updateQueryParams(buildAbwabQueryParams({ modal: null }), true);
  }

  clearSectionQueryParam(): void {
    this.updateQueryParams({ [ABWAB_QUERY_KEYS.section]: null }, true);
  }

  private closeUrlBackedModal(
    kinds: readonly AbwabModalKind[],
    close: () => void,
    onRestoreFocus: FocusCallback,
    discard = false,
  ): void {
    const kind = this.modalUrl.urlBackedKind(kinds);
    close();
    if (kind === null) {
      return;
    }
    this.modalUrl.releaseTracking();
    this.updateQueryParams(buildAbwabQueryParams({ modal: discard ? null : { kind, closed: true, subjectDoorId: null } }), true);
    if (!discard) {
      this.focusQueued(onRestoreFocus);
    }
  }

  private focusQueued(focus: FocusCallback): void {
    setTimeout(focus, 0);
  }

  private openOnSelectedDoor(kind: AbwabModalKind): void {
    if (!this.permissions.canOpenModal(kind)) {
      return;
    }
    const door = this.overlays.selectedDoor();
    if (!door) {
      return;
    }
    this.modalUrl.open(kind);
    this.commitModalOpen(kind, door.id);
  }

  private commitModalOpen(kind: AbwabModalKind, doorId?: number): void {
    this.modalUrl.trackOpen(kind, doorId ?? null);
    this.updateQueryParams(
      buildAbwabQueryParams({
        ...(doorId === undefined ? {} : { door: doorId }),
        modal: { kind, closed: false, subjectDoorId: null },
      }),
    );
  }

  private updateQueryParams(changes: Record<string, string | null>, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }
}
