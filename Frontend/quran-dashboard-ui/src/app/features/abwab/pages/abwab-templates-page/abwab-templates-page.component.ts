import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, of } from 'rxjs';

import { ABWAB_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { AbwabTemplatesFacade } from '../../state/abwab-templates.facade';
import { AbwabTemplatesController } from '../../state/abwab-templates.controller';
import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import {
  AbwabAuthoringFields,
  collectAbwabTemplateNodes,
  toAuthoringFields,
} from '../../models/abwab-templates.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabAnnouncerComponent } from '../../components/abwab-announcer/abwab-announcer.component';
import {
  AbwabTemplateNodeMenuRequest,
  AbwabTemplateTreeComponent,
} from '../../components/abwab-template-tree/abwab-template-tree.component';
import { AbwabTemplateNodeModalComponent } from '../../components/abwab-template-node-modal/abwab-template-node-modal.component';
import { AbwabTemplateCopyModalComponent } from '../../components/abwab-template-copy-modal/abwab-template-copy-modal.component';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';

/** What the node modal is currently authoring. `parentNodeId` is the new node's parent when
 * adding; `nodeId` is the edited node when editing. */
type AbwabNodeModalState =
  | { readonly mode: 'add'; readonly parentNodeId: number }
  | { readonly mode: 'edit'; readonly nodeId: number };

/**
 * Route shell for `/abwab/templates` — the templates workshop: the template list, the tree
 * editor, and the node authoring modal. It composes the same `.qd-page`/`.qd-container` shell as
 * the doors page, so the flat parchment+green surface rules apply without being restated here.
 *
 * State lives in the root-scoped `AbwabTemplatesFacade` (a cache) while the overlays are owned by
 * this component (page-scoped) — the split `features/abwab/README.md` already records.
 */
@Component({
  selector: 'qd-abwab-templates-page',
  standalone: true,
  imports: [
    RouterLink,
    AbwabAnnouncerComponent,
    AbwabTemplateTreeComponent,
    AbwabTemplateNodeModalComponent,
    AbwabTemplateCopyModalComponent,
    QdContextMenuComponent,
    QdSkeletonRowsComponent,
    ExplorerPanelSkeletonComponent,
  ],
  templateUrl: './abwab-templates-page.component.html',
  styleUrl: './abwab-templates-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTemplatesPageComponent implements OnInit {
  protected readonly facade = inject(AbwabTemplatesFacade);
  protected readonly controller = inject(AbwabTemplatesController);
  private readonly doorsFacade = inject(AbwabSnapshotFacade);

  protected readonly doorsRoutePath = `/${ABWAB_ROUTE_PATH}`;

  protected readonly namingTemplate = signal(false);
  protected readonly newTemplateName = signal('');
  protected readonly nodeModal = signal<AbwabNodeModalState | null>(null);
  protected readonly contextMenuNodeId = signal<number | null>(null);
  protected readonly contextMenuPosition = signal<{ x: number; y: number }>({ x: 0, y: 0 });
  protected readonly deletingNodeId = signal<number | null>(null);
  protected readonly confirmingTemplateDelete = signal(false);
  protected readonly copyModalOpen = signal(false);

  /** The picker's only source. It is fetched when the modal opens rather than on page entry:
   * the workshop is reachable directly by URL, so the doors snapshot may never have been
   * loaded — and loading it on every entry would buy a request the picker often never uses. */
  protected readonly liveRoots = computed(() => this.doorsFacade.snapshot()?.liveRoots ?? []);
  /** Only while there is nothing to show — once a snapshot exists the facade leaves it in place
   * across a refresh, so the picker keeps rendering doors rather than blinking to a status line. */
  protected readonly doorsLoading = computed(() => this.doorsFacade.isLoading() && !this.doorsFacade.snapshot());
  protected readonly doorsError = computed(() =>
    this.doorsFacade.snapshot() ? null : this.doorsFacade.errorMessage(),
  );

  private readonly nodesById = computed(() => collectAbwabTemplateNodes(this.facade.selectedTemplate()?.root ?? null));

  protected readonly nodeModalOpen = computed(() => this.nodeModal() !== null);
  protected readonly nodeModalIsEdit = computed(() => this.nodeModal()?.mode === 'edit');

  protected readonly nodeModalFields = computed<AbwabAuthoringFields | null>(() => {
    const state = this.nodeModal();
    if (state?.mode !== 'edit') {
      return null;
    }
    const node = this.nodesById().get(state.nodeId);
    return node ? toAuthoringFields(node) : null;
  });

  protected readonly nodeModalIsRoot = computed(() => {
    const state = this.nodeModal();
    return state?.mode === 'edit' && this.nodesById().get(state.nodeId)?.parentNodeId === null;
  });

  protected readonly nodeModalContextName = computed(() => {
    const state = this.nodeModal();
    if (!state) {
      return null;
    }
    const nodeId = state.mode === 'edit' ? state.nodeId : state.parentNodeId;
    return this.nodesById().get(nodeId)?.name ?? null;
  });

  protected readonly deletingNodeName = computed(() => {
    const nodeId = this.deletingNodeId();
    return nodeId === null ? null : (this.nodesById().get(nodeId)?.name ?? null);
  });

  /** The root is the template itself: deleting it is refused by the API, so the row menu offers
   * «حذف القالب» rather than a node delete the user would only see fail. */
  protected readonly contextMenuIsRoot = computed(() => {
    const nodeId = this.contextMenuNodeId();
    return nodeId !== null && this.nodesById().get(nodeId)?.parentNodeId === null;
  });

  protected get pageTitle(): string { return ABWAB_LABELS.templatesPageTitle; }
  protected get pageSubtitle(): string { return ABWAB_LABELS.templatesPageSubtitle; }
  protected get backToDoorsLabel(): string { return ABWAB_LABELS.backToDoorsButton; }
  protected get newTemplateLabel(): string { return ABWAB_LABELS.newTemplateButton; }
  protected get newTemplateNameLabel(): string { return ABWAB_LABELS.newTemplateNameLabel; }
  protected get newTemplateNamePlaceholder(): string { return ABWAB_LABELS.newTemplateNamePlaceholder; }
  protected get editTemplateLabel(): string { return ABWAB_LABELS.editTemplateButton; }
  protected get deleteTemplateLabel(): string { return ABWAB_LABELS.deleteTemplateButton; }
  protected get copyToDoorsLabel(): string { return ABWAB_LABELS.copyToDoorsButton; }
  protected get templatesListAriaLabel(): string { return ABWAB_LABELS.templatesListAriaLabel; }
  protected get templateTreeAriaLabel(): string { return ABWAB_LABELS.templateTreeAriaLabel; }
  protected get templatesEmptyMessage(): string { return ABWAB_LABELS.templatesEmptyMessage; }
  protected get templateNoneSelectedMessage(): string { return ABWAB_LABELS.templateNoneSelectedMessage; }
  protected get templatesLoadingMessage(): string { return ABWAB_LABELS.templatesLoadingMessage; }
  protected get nodeEditOpLabel(): string { return ABWAB_LABELS.templateNodeEditOp; }
  protected get nodeAddChildOpLabel(): string { return ABWAB_LABELS.templateNodeAddChildOp; }
  protected get nodeDeleteOpLabel(): string { return ABWAB_LABELS.templateNodeDeleteOp; }
  protected get templateDeleteConfirmMessage(): string { return ABWAB_LABELS.templateDeleteConfirm; }
  protected get deleteConfirmLabel(): string { return ABWAB_LABELS.deleteConfirmButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

  protected elementCountLabel(count: number): string {
    return ABWAB_LABELS.templateElementCount(count);
  }

  protected nodeDeleteConfirmMessage(nodeName: string): string {
    return ABWAB_LABELS.templateNodeDeleteConfirm(nodeName);
  }

  ngOnInit(): void {
    this.facade.loadList();
  }

  protected selectTemplate(templateId: number): void {
    this.closeOverlays();
    this.facade.select(templateId);
  }

  protected startNamingTemplate(): void {
    this.namingTemplate.set(true);
    this.newTemplateName.set('');
  }

  protected onNewTemplateNameInput(event: Event): void {
    this.newTemplateName.set((event.target as HTMLInputElement).value);
  }

  protected createTemplate(event: Event): void {
    event.preventDefault();
    const name = this.newTemplateName().trim();
    if (!name) {
      return;
    }
    this.controller.createTemplate(name).subscribe((outcome) => {
      if (outcome.kind !== 'success') {
        return;
      }
      this.namingTemplate.set(false);
      this.newTemplateName.set('');
      // Selecting the new template is what puts its root — the only node it has — on screen so
      // it can be authored through the full modal. Through `selectTemplate`, not the facade
      // directly: every selection change must also close the overlays, or a node id from the
      // previous template survives the switch in a still-open modal or confirm.
      if (outcome.data) {
        this.selectTemplate(outcome.data.id);
      }
    });
  }

  protected cancelNamingTemplate(): void {
    this.namingTemplate.set(false);
    this.newTemplateName.set('');
  }

  protected editRoot(): void {
    const rootId = this.facade.selectedTemplate()?.root?.id;
    if (rootId !== undefined) {
      this.nodeModal.set({ mode: 'edit', nodeId: rootId });
    }
  }

  protected onAddChildRequested(parentNodeId: number): void {
    this.closeContextMenu();
    this.nodeModal.set({ mode: 'add', parentNodeId });
  }

  protected onEditRequested(nodeId: number): void {
    this.closeContextMenu();
    this.nodeModal.set({ mode: 'edit', nodeId });
  }

  protected closeNodeModal(): void {
    this.nodeModal.set(null);
  }

  /** Bound into the node modal as a function input, the `abwab-sections-modal` precedent, so the
   * modal never reaches for a controller of its own. */
  protected readonly submitNode = (fields: AbwabAuthoringFields): Observable<AbwabWriteOutcome<unknown>> => {
    const state = this.nodeModal();
    if (state?.mode === 'edit') {
      return this.controller.editNode(state.nodeId, fields);
    }
    const template = this.facade.selectedTemplate();
    if (state === null || template === null) {
      // Unreachable: the modal only opens over a selected template. No request is invented for a
      // state that would have to fabricate an id to send.
      return of<AbwabWriteOutcome<unknown>>({ kind: 'invalid', message: ABWAB_LABELS.writeInvalidFallback });
    }
    return this.controller.addNode(template.id, state.parentNodeId, fields);
  };

  protected onQuickAddRequested(name: string): void {
    const template = this.facade.selectedTemplate();
    const rootId = template?.root?.id;
    if (!template || rootId === undefined) {
      return;
    }
    this.controller
      .addNode(template.id, rootId, { name, description: '', representativeAyahText: '', aliases: [] })
      .subscribe();
  }

  protected onOrderCommitted(event: { nodeId: number; position: number }): void {
    this.controller.reorderNode(event.nodeId, event.position).subscribe();
  }

  protected onMenuRequested(request: AbwabTemplateNodeMenuRequest): void {
    this.contextMenuPosition.set({ x: request.x, y: request.y });
    this.contextMenuNodeId.set(request.nodeId);
  }

  protected closeContextMenu(): void {
    this.contextMenuNodeId.set(null);
  }

  protected requestNodeDelete(): void {
    const nodeId = this.contextMenuNodeId();
    this.closeContextMenu();
    if (nodeId !== null) {
      this.deletingNodeId.set(nodeId);
    }
  }

  protected confirmNodeDelete(): void {
    const nodeId = this.deletingNodeId();
    this.deletingNodeId.set(null);
    if (nodeId !== null) {
      this.controller.deleteNode(nodeId).subscribe();
    }
  }

  protected cancelNodeDelete(): void {
    this.deletingNodeId.set(null);
  }

  protected requestTemplateDelete(): void {
    this.closeContextMenu();
    this.confirmingTemplateDelete.set(true);
  }

  protected confirmTemplateDelete(): void {
    const template = this.facade.selectedTemplate();
    this.confirmingTemplateDelete.set(false);
    if (template === null) {
      return;
    }
    this.controller.deleteTemplate(template.id).subscribe((outcome) => {
      if (outcome.kind === 'success') {
        this.facade.clearSelection();
      }
    });
  }

  protected cancelTemplateDelete(): void {
    this.confirmingTemplateDelete.set(false);
  }

  protected openCopyModal(): void {
    this.doorsFacade.load();
    this.copyModalOpen.set(true);
  }

  protected closeCopyModal(): void {
    this.copyModalOpen.set(false);
  }

  /** Bound into the copy modal as a function input. The apply refreshes nothing here: it writes
   * doors, and `AbwabPageComponent.ngOnInit` calls `facade.load()` on every entry, so returning
   * to `/abwab` is what makes the copies visible.
   *
   * The id comes off the same object the modal's preview renders from, never from
   * `selectedTemplateId()`: the two can only ever name the same template that way, so a copy
   * cannot land under a template the user was not shown. */
  protected readonly applyTemplate = (
    targetDoorIds: readonly number[],
  ): Observable<AbwabWriteOutcome<AbwabDoorDto[] | null>> => {
    const template = this.facade.selectedTemplate();
    if (template === null) {
      return of<AbwabWriteOutcome<AbwabDoorDto[] | null>>({
        kind: 'invalid',
        message: ABWAB_LABELS.writeInvalidFallback,
      });
    }
    return this.controller.applyTemplate(template.id, targetDoorIds);
  };

  protected ctxEdit(): void {
    const nodeId = this.contextMenuNodeId();
    if (nodeId !== null) {
      this.onEditRequested(nodeId);
    }
  }

  protected ctxAddChild(): void {
    const nodeId = this.contextMenuNodeId();
    if (nodeId !== null) {
      this.onAddChildRequested(nodeId);
    }
  }

  private closeOverlays(): void {
    this.nodeModal.set(null);
    this.contextMenuNodeId.set(null);
    this.deletingNodeId.set(null);
    this.confirmingTemplateDelete.set(false);
    this.copyModalOpen.set(false);
  }
}
