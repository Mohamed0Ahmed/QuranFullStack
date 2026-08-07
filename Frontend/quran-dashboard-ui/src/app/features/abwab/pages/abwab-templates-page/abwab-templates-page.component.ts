import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { ABWAB_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { AbwabTemplatesPageDeleteController } from './abwab-templates-page-delete.controller';
import { AbwabTemplatesFacade } from '../../state/abwab-templates.facade';
import { AbwabTemplatesController } from '../../state/abwab-templates.controller';
import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabTemplatesOverlaysController } from '../../state/abwab-templates-overlays.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabAnnouncerComponent } from '../../components/abwab-announcer/abwab-announcer.component';
import {
  AbwabTemplateNodeMenuRequest,
  AbwabTemplateTreeComponent,
} from '../../components/abwab-template-tree/abwab-template-tree.component';
import { AbwabTemplateNodeModalComponent } from '../../components/abwab-template-node-modal/abwab-template-node-modal.component';
import { AbwabTemplateCopyModalComponent } from '../../components/abwab-template-copy-modal/abwab-template-copy-modal.component';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';

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
    QdStateComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './abwab-templates-page.component.html',
  styleUrl: './abwab-templates-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AbwabTemplatesPageDeleteController, AbwabPermissionsController, AbwabTemplatesOverlaysController],
})
export class AbwabTemplatesPageComponent implements OnInit {
  protected readonly facade = inject(AbwabTemplatesFacade);
  protected readonly controller = inject(AbwabTemplatesController);
  protected readonly permissions = inject(AbwabPermissionsController);
  protected readonly overlays = inject(AbwabTemplatesOverlaysController);
  private readonly doorsFacade = inject(AbwabSnapshotFacade);

  protected readonly doorsRoutePath = `/${ABWAB_ROUTE_PATH}`;

  protected readonly namingTemplate = signal(false);
  protected readonly newTemplateName = signal('');
  private readonly templateDelete = inject(AbwabTemplatesPageDeleteController);
  protected readonly confirmingTemplateDelete = this.templateDelete.confirming;
  protected readonly templateDeleteBusy = this.templateDelete.busy;
  protected readonly templateDeleteError = this.templateDelete.error;
  protected readonly nodeModalOpen = this.overlays.nodeModalOpen;
  protected readonly nodeModalFields = this.overlays.nodeModalFields;
  protected readonly nodeModalIsEdit = this.overlays.nodeModalIsEdit;
  protected readonly nodeModalIsRoot = this.overlays.nodeModalIsRoot;
  protected readonly nodeModalContextName = this.overlays.nodeModalContextName;
  protected readonly canNodeModalSubmit = this.overlays.canNodeModalSubmit;
  protected readonly canRootTemplateTreeContextMenu = this.overlays.canRootTemplateTreeContextMenu;
  protected readonly canNodeTemplateTreeContextMenu = this.overlays.canNodeTemplateTreeContextMenu;
  protected readonly deletingNodeName = this.overlays.deletingNodeName;
  protected readonly contextMenuIsRoot = this.overlays.contextMenuIsRoot;
  protected readonly contextMenuNodeId = this.overlays.contextMenuNodeId;
  protected readonly contextMenuPosition = this.overlays.contextMenuPosition;
  protected readonly nodeDeleteBusy = this.overlays.nodeDeleteBusy;
  protected readonly nodeDeleteError = this.overlays.nodeDeleteError;
  protected readonly copyModalOpen = this.overlays.copyModalOpen;
  protected readonly submitNode = this.overlays.submitNode;
  protected readonly applyTemplate = this.overlays.applyTemplate;

  private readonly headerFallbackFocus = viewChild<ElementRef<HTMLAnchorElement>>('headerFallbackFocus');

  protected readonly liveRoots = computed(() => this.doorsFacade.snapshot()?.liveRoots ?? []);
  protected readonly doorsLoading = computed(() => this.doorsFacade.isLoading() && !this.doorsFacade.snapshot());
  protected readonly doorsError = computed(() =>
    this.doorsFacade.snapshot() ? null : this.doorsFacade.errorMessage(),
  );

  protected get pageTitle(): string { return ABWAB_LABELS.templatesPageTitle; }
  protected get pageSubtitle(): string { return ABWAB_LABELS.templatesPageSubtitle; }
  protected get backToDoorsLabel(): string { return ABWAB_LABELS.backToDoorsButton; }
  protected get newTemplateLabel(): string { return ABWAB_LABELS.newTemplateButton; }
  protected get newTemplateNameLabel(): string { return ABWAB_LABELS.newTemplateNameLabel; }
  protected get newTemplateNamePlaceholder(): string { return ABWAB_LABELS.newTemplateNamePlaceholder; }
  protected get editTemplateLabel(): string { return ABWAB_LABELS.editTemplateButton; }
  protected get deleteTemplateLabel(): string { return ABWAB_LABELS.deleteTemplateButton; }
  protected get templateDeleteConfirmTitle(): string { return ABWAB_LABELS.templateDeleteConfirmTitle; }
  protected get templateNodeDeleteConfirmTitle(): string { return ABWAB_LABELS.templateNodeDeleteConfirmTitle; }
  protected get copyToDoorsLabel(): string { return ABWAB_LABELS.copyToDoorsButton; }
  protected get templatesListAriaLabel(): string { return ABWAB_LABELS.templatesListAriaLabel; }
  protected get templateTreeAriaLabel(): string { return ABWAB_LABELS.templateTreeAriaLabel; }
  protected get templatesEmptyMessage(): string { return ABWAB_LABELS.templatesEmptyMessage; }
  protected get templateNoneSelectedMessage(): string { return ABWAB_LABELS.templateNoneSelectedMessage; }
  protected get templatesLoadingMessage(): string { return ABWAB_LABELS.templatesLoadingMessage; }
  protected get templateLoadingMessage(): string { return ABWAB_LABELS.templateLoadingMessage; }
  protected get nodeEditOpLabel(): string { return ABWAB_LABELS.templateNodeEditOp; }
  protected get nodeAddChildOpLabel(): string { return ABWAB_LABELS.templateNodeAddChildOp; }
  protected get nodeDeleteOpLabel(): string { return ABWAB_LABELS.templateNodeDeleteOp; }
  protected get templateDeleteConfirmMessage(): string { return ABWAB_LABELS.templateDeleteConfirm; }
  protected get deleteConfirmLabel(): string { return ABWAB_LABELS.deleteConfirmButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }

  protected elementCountLabel(count: number): string {
    return ABWAB_LABELS.templateElementCount(count);
  }

  protected nodeDeleteConfirmMessage(nodeName: string): string {
    return ABWAB_LABELS.templateNodeDeleteConfirm(nodeName);
  }

  constructor() {
    effect(() => {
      if (!this.permissions.canCreateTemplate()) {
        this.namingTemplate.set(false);
        this.newTemplateName.set('');
      }
      if (!this.permissions.canDeleteTemplate()) {
        this.confirmingTemplateDelete.set(false);
      }
    });
  }

  ngOnInit(): void {
    this.facade.loadList();
  }

  protected selectTemplate(templateId: number): void {
    this.closeOverlays();
    this.facade.select(templateId);
  }

  protected startNamingTemplate(): void {
    if (!this.permissions.canCreateTemplate()) {
      return;
    }
    this.namingTemplate.set(true);
    this.newTemplateName.set('');
  }

  protected onNewTemplateNameInput(event: Event): void {
    this.newTemplateName.set((event.target as HTMLInputElement).value);
  }

  protected createTemplate(event: Event): void {
    event.preventDefault();
    const name = this.newTemplateName().trim();
    if (!this.permissions.canCreateTemplate() || !name) {
      return;
    }
    this.controller.createTemplate(name).subscribe((outcome) => {
      if (outcome.kind !== 'success') {
        return;
      }
      this.namingTemplate.set(false);
      this.newTemplateName.set('');
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
    this.overlays.editRoot();
  }

  protected onAddChildRequested(parentNodeId: number): void {
    this.overlays.onAddChildRequested(parentNodeId);
  }

  protected onEditRequested(nodeId: number): void {
    this.overlays.onEditRequested(nodeId);
  }

  protected closeNodeModal(): void {
    this.overlays.closeNodeModal(() => this.focusHeaderFallback());
  }

  protected onQuickAddRequested(name: string): void {
    this.overlays.onQuickAddRequested(name);
  }

  protected onOrderCommitted(event: { nodeId: number; position: number }): void {
    this.overlays.onOrderCommitted(event);
  }

  protected onMenuRequested(request: AbwabTemplateNodeMenuRequest): void {
    this.overlays.onMenuRequested(request);
  }

  protected closeContextMenu(): void {
    this.overlays.closeContextMenu();
  }

  protected requestNodeDelete(): void {
    this.overlays.requestNodeDelete();
  }

  protected confirmNodeDelete(): void {
    this.overlays.confirmNodeDelete(() => this.focusHeaderFallback());
  }

  protected cancelNodeDelete(): void {
    this.overlays.cancelNodeDelete(() => this.focusHeaderFallback());
  }

  protected requestTemplateDelete(): void {
    if (!this.permissions.canDeleteTemplate()) {
      return;
    }
    this.overlays.closeContextMenu();
    this.templateDelete.request();
  }

  protected ctxDeleteTemplate(): void {
    if (!this.permissions.canDeleteTemplate() || !this.contextMenuIsRoot()) {
      return;
    }

    this.requestTemplateDelete();
    this.overlays.markContextMenuOrigin();
  }

  protected confirmTemplateDelete(): void {
    if (!this.permissions.canDeleteTemplate()) {
      return;
    }
    this.templateDelete.confirm(() => {
      this.overlays.consumeContextMenuOrigin();
      this.focusHeaderFallback();
    });
  }

  protected cancelTemplateDelete(): void {
    this.templateDelete.cancel(() => {
      if (this.overlays.consumeContextMenuOrigin()) {
        this.focusHeaderFallback();
      }
    });
  }

  protected openCopyModal(): void {
    this.overlays.openCopyModal(() => this.doorsFacade.load());
  }

  protected closeCopyModal(): void {
    this.overlays.closeCopyModal();
  }

  protected retryDoorsLoad(): void {
    this.doorsFacade.load();
  }

  protected ctxEdit(): void {
    this.overlays.ctxEdit();
  }

  protected ctxAddChild(): void {
    this.overlays.ctxAddChild();
  }

  private focusHeaderFallback(): void {
    setTimeout(() => this.headerFallbackFocus()?.nativeElement.focus(), 0);
  }

  private closeOverlays(): void {
    this.overlays.closeOverlays();
    this.confirmingTemplateDelete.set(false);
  }
}
