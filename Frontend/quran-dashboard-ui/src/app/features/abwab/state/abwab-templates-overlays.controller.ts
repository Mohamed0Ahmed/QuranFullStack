import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, of } from 'rxjs';

import { ABWAB_LABELS } from '../models/abwab.labels';
import {
  AbwabAuthoringFields,
  collectAbwabTemplateNodes,
  toAuthoringFields,
} from '../models/abwab-templates.models';
import { AbwabTemplatesFacade } from './abwab-templates.facade';
import { AbwabTemplatesController } from './abwab-templates.controller';
import { AbwabPermissionsController } from './abwab-permissions.controller';
import { AbwabWriteOutcome } from './abwab-write.controller';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';

type AbwabNodeModalState =
  | { readonly mode: 'add'; readonly parentNodeId: number }
  | { readonly mode: 'edit'; readonly nodeId: number };

interface AbwabTemplateNodeMenuRequest {
  readonly nodeId: number;
  readonly x: number;
  readonly y: number;
}

@Injectable()
export class AbwabTemplatesOverlaysController {
  private readonly facade = inject(AbwabTemplatesFacade);
  private readonly templates = inject(AbwabTemplatesController);
  private readonly permissions = inject(AbwabPermissionsController);

  readonly nodeModal = signal<AbwabNodeModalState | null>(null);
  readonly contextMenuNodeId = signal<number | null>(null);
  readonly contextMenuPosition = signal<{ x: number; y: number }>({ x: 0, y: 0 });
  readonly deletingNodeId = signal<number | null>(null);
  readonly nodeDeleteBusy = signal(false);
  readonly nodeDeleteError = signal<string | null>(null);
  readonly copyModalOpen = signal(false);

  private readonly overlayFromContextMenu = signal(false);
  private readonly nodesById = computed(() => collectAbwabTemplateNodes(this.facade.selectedTemplate()?.root ?? null));

  readonly nodeModalOpen = computed(() => this.nodeModal() !== null);
  readonly nodeModalIsEdit = computed(() => this.nodeModal()?.mode === 'edit');
  readonly canNodeModalSubmit = computed(() => {
    const state = this.nodeModal();
    return state?.mode === 'edit' ? this.permissions.canEditTemplateNode() : this.permissions.canCreateTemplateNode();
  });
  readonly canRootTemplateTreeContextMenu = computed(
    () =>
      this.permissions.canEditTemplateNode() ||
      this.permissions.canCreateTemplateNode() ||
      this.permissions.canDeleteTemplate(),
  );
  readonly canNodeTemplateTreeContextMenu = computed(
    () =>
      this.permissions.canEditTemplateNode() ||
      this.permissions.canCreateTemplateNode() ||
      this.permissions.canDeleteTemplateNode(),
  );
  readonly nodeModalFields = computed<AbwabAuthoringFields | null>(() => {
    const state = this.nodeModal();
    if (state?.mode !== 'edit') {
      return null;
    }
    const node = this.nodesById().get(state.nodeId);
    return node ? toAuthoringFields(node) : null;
  });
  readonly nodeModalIsRoot = computed(() => {
    const state = this.nodeModal();
    return state?.mode === 'edit' && this.nodesById().get(state.nodeId)?.parentNodeId === null;
  });
  readonly nodeModalContextName = computed(() => {
    const state = this.nodeModal();
    if (!state) {
      return null;
    }
    const nodeId = state.mode === 'edit' ? state.nodeId : state.parentNodeId;
    return this.nodesById().get(nodeId)?.name ?? null;
  });
  readonly deletingNodeName = computed(() => {
    const nodeId = this.deletingNodeId();
    return nodeId === null ? null : (this.nodesById().get(nodeId)?.name ?? null);
  });
  readonly contextMenuIsRoot = computed(() => {
    const nodeId = this.contextMenuNodeId();
    return nodeId !== null && this.nodesById().get(nodeId)?.parentNodeId === null;
  });

  readonly submitNode = (fields: AbwabAuthoringFields): Observable<AbwabWriteOutcome<unknown>> => {
    const state = this.nodeModal();
    if (state?.mode === 'edit') {
      return this.templates.editNode(state.nodeId, fields);
    }
    const template = this.facade.selectedTemplate();
    if (state === null || template === null) {
      return of<AbwabWriteOutcome<unknown>>({ kind: 'invalid', message: ABWAB_LABELS.writeInvalidFallback });
    }
    return this.templates.addNode(template.id, state.parentNodeId, fields);
  };

  readonly applyTemplate = (
    targetDoorIds: readonly number[],
  ): Observable<AbwabWriteOutcome<AbwabDoorDto[] | null>> => {
    const template = this.facade.selectedTemplate();
    if (template === null) {
      return of<AbwabWriteOutcome<AbwabDoorDto[] | null>>({
        kind: 'invalid',
        message: ABWAB_LABELS.writeInvalidFallback,
      });
    }
    return this.templates.applyTemplate(template.id, targetDoorIds);
  };

  constructor() {
    effect(() => {
      if (this.nodeModalOpen() && !this.canNodeModalSubmit()) {
        this.nodeModal.set(null);
      }
      if (!this.permissions.canApplyTemplate()) {
        this.copyModalOpen.set(false);
      }
      if (!this.permissions.canDeleteTemplateNode()) {
        this.deletingNodeId.set(null);
        this.nodeDeleteError.set(null);
      }
      const nodeId = this.contextMenuNodeId();
      if (nodeId !== null && !this.permissions.canOpenTemplateContextMenu(this.contextMenuIsRoot())) {
        this.closeContextMenu();
      }
    });
  }

  editRoot(): void {
    if (!this.permissions.canEditTemplateNode()) {
      return;
    }
    const rootId = this.facade.selectedTemplate()?.root?.id;
    if (rootId !== undefined) {
      this.nodeModal.set({ mode: 'edit', nodeId: rootId });
    }
  }

  onAddChildRequested(parentNodeId: number): void {
    if (!this.permissions.canCreateTemplateNode()) {
      return;
    }
    this.closeContextMenu();
    this.nodeModal.set({ mode: 'add', parentNodeId });
  }

  onEditRequested(nodeId: number): void {
    if (!this.permissions.canEditTemplateNode()) {
      return;
    }
    this.closeContextMenu();
    this.nodeModal.set({ mode: 'edit', nodeId });
  }

  closeNodeModal(onClosedFromContextMenu: () => void): void {
    const fromContextMenu = this.consumeContextMenuOrigin();
    this.nodeModal.set(null);
    if (fromContextMenu) {
      onClosedFromContextMenu();
    }
  }

  onQuickAddRequested(name: string): void {
    if (!this.permissions.canCreateTemplateNode()) {
      return;
    }
    const template = this.facade.selectedTemplate();
    const rootId = template?.root?.id;
    if (!template || rootId === undefined) {
      return;
    }
    this.templates
      .addNode(template.id, rootId, { name, description: '', representativeAyahText: '', aliases: [] })
      .subscribe();
  }

  onOrderCommitted(event: { nodeId: number; position: number }): void {
    if (!this.permissions.canReorderTemplateNode()) {
      return;
    }
    const node = this.nodesById().get(event.nodeId);
    if (!node || node.parentNodeId === null) {
      return;
    }
    this.templates.reorderNode(event.nodeId, event.position).subscribe();
  }

  onMenuRequested(request: AbwabTemplateNodeMenuRequest): void {
    const node = this.nodesById().get(request.nodeId);
    if (!node || !this.permissions.canOpenTemplateContextMenu(node.parentNodeId === null)) {
      return;
    }
    this.contextMenuPosition.set({ x: request.x, y: request.y });
    this.contextMenuNodeId.set(request.nodeId);
  }

  closeContextMenu(): void {
    this.contextMenuNodeId.set(null);
  }

  requestNodeDelete(): void {
    if (!this.permissions.canDeleteTemplateNode()) {
      return;
    }
    const nodeId = this.contextMenuNodeId();
    if (nodeId === null) {
      return;
    }
    const node = this.nodesById().get(nodeId);
    if (!node || node.parentNodeId === null) {
      return;
    }
    this.closeContextMenu();
    this.nodeDeleteError.set(null);
    this.nodeDeleteBusy.set(false);
    this.deletingNodeId.set(nodeId);
    this.markContextMenuOrigin();
  }

  confirmNodeDelete(onDeleted: () => void): void {
    const nodeId = this.deletingNodeId();
    if (nodeId === null) {
      return;
    }
    const node = this.nodesById().get(nodeId);
    if (!this.permissions.canDeleteTemplateNode() || !node || node.parentNodeId === null || this.nodeDeleteBusy()) {
      return;
    }
    this.nodeDeleteBusy.set(true);
    this.nodeDeleteError.set(null);
    this.templates.deleteNode(nodeId).subscribe((outcome) => {
      this.nodeDeleteBusy.set(false);
      if (outcome.kind !== 'success') {
        this.nodeDeleteError.set(outcome.message);
        return;
      }
      this.consumeContextMenuOrigin();
      this.deletingNodeId.set(null);
      onDeleted();
    });
  }

  cancelNodeDelete(onCancelled: () => void): void {
    if (this.nodeDeleteBusy()) {
      return;
    }
    this.consumeContextMenuOrigin();
    this.deletingNodeId.set(null);
    this.nodeDeleteError.set(null);
    onCancelled();
  }

  openCopyModal(onOpened: () => void): void {
    if (!this.permissions.canApplyTemplate()) {
      return;
    }
    onOpened();
    this.copyModalOpen.set(true);
  }

  closeCopyModal(): void {
    this.copyModalOpen.set(false);
  }

  ctxEdit(): void {
    if (!this.permissions.canEditTemplateNode()) {
      return;
    }
    const nodeId = this.contextMenuNodeId();
    if (nodeId !== null) {
      this.onEditRequested(nodeId);
      this.markContextMenuOrigin();
    }
  }

  ctxAddChild(): void {
    if (!this.permissions.canCreateTemplateNode()) {
      return;
    }
    const nodeId = this.contextMenuNodeId();
    if (nodeId !== null) {
      this.onAddChildRequested(nodeId);
      this.markContextMenuOrigin();
    }
  }

  markContextMenuOrigin(): void {
    this.overlayFromContextMenu.set(true);
  }

  consumeContextMenuOrigin(): boolean {
    const fromContextMenu = this.overlayFromContextMenu();
    this.overlayFromContextMenu.set(false);
    return fromContextMenu;
  }

  closeOverlays(): void {
    this.overlayFromContextMenu.set(false);
    this.nodeModal.set(null);
    this.contextMenuNodeId.set(null);
    this.deletingNodeId.set(null);
    this.copyModalOpen.set(false);
  }
}
