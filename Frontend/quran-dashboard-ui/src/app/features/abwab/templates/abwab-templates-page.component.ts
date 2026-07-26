import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { TemplateNodeAliasDto, TemplateNodeDto } from '../../../core/api/generated/models';
import { QdStateComponent } from '../../../shared/ui/state/state.component';
import { ABWAB_CONFLICT_MESSAGES, AbwabConflictError } from '../data-access/abwab-conflict';
import { ABWAB_TEMPLATES_CACHE_PROVIDER } from '../data-access/abwab-templates-cache';
import { AbwabPermissions } from '../state/abwab-permissions';
import { AbwabTemplatesFacade, AbwabTemplatesMutationStatus } from '../state/abwab-templates.facade';
import { TemplateApplicationPanelComponent, TemplateApplyRequest } from './template-application-panel.component';
import {
  TemplateNodeAddRequest,
  TemplateNodeAliasAddRequest,
  TemplateNodeAliasEditRequest,
  TemplateNodeEditRequest,
  TemplateNodeEditorComponent,
  TemplateNodeReorderRequest,
  TemplateNodeReparentRequest,
} from './template-node-editor.component';

const NOT_AUTHORIZED_MESSAGE = 'لا تملك صلاحية عرض قوالب الأبواب.';
const EMPTY_MESSAGE = 'لا توجد قوالب مسجّلة.';
const LOADING_MESSAGE = 'جارٍ تحميل القوالب…';
const RETRY_LABEL = 'إعادة المحاولة';
const INCLUDE_DELETED_PARAM = 'includeDeleted';

type TemplateForm = FormGroup<{
  name: FormControl<string>;
  description: FormControl<string>;
}>;

interface StructuralContext {
  readonly doorTemplateId: string;
  readonly templateRevision: number;
  readonly generation: number;
}

// The page shell: it owns the template list and aggregate CRUD, and turns the node editor's and the
// application panel's intent events into facade mutations by attaching the concurrency expectations.
// The frozen §5 UI label is «قوالب الأبواب». Saving is always an explicit submit — no autosave and no
// edit-session lock — and there is no drag surface anywhere in this feature.
@Component({
  selector: 'qd-abwab-templates-page',
  standalone: true,
  imports: [ReactiveFormsModule, QdStateComponent, TemplateNodeEditorComponent, TemplateApplicationPanelComponent],
  providers: [AbwabTemplatesFacade, ABWAB_TEMPLATES_CACHE_PROVIDER, AbwabPermissions],
  templateUrl: './abwab-templates-page.component.html',
  styleUrl: './abwab-templates-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTemplatesPageComponent implements OnInit {
  protected readonly facade = inject(AbwabTemplatesFacade);
  protected readonly permissions = inject(AbwabPermissions);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly pageTitle = 'قوالب الأبواب';
  protected readonly notAuthorizedMessage = NOT_AUTHORIZED_MESSAGE;
  protected readonly emptyMessage = EMPTY_MESSAGE;
  protected readonly loadingMessage = LOADING_MESSAGE;
  protected readonly retryLabel = RETRY_LABEL;

  protected readonly editingTemplateId = signal<string | null>(null);

  // Two SEPARATE scoped statuses, deliberately not the page-wide facade signals. Each sub-editor
  // discards the operator's input only when ITS OWN write was accepted: forwarding one shared signal
  // would let an unrelated success wipe a chosen apply target or close a node form mid-edit, and let
  // an unrelated conflict render as though that sub-editor had failed.
  private readonly applyStatusSignal = signal<AbwabTemplatesMutationStatus>('idle');
  private readonly applyErrorSignal = signal<Error | null>(null);
  private readonly editorStatusSignal = signal<AbwabTemplatesMutationStatus>('idle');

  protected readonly applyStatus = this.applyStatusSignal.asReadonly();
  protected readonly editorStatus = this.editorStatusSignal.asReadonly();

  protected readonly addTemplateForm: TemplateForm = this.buildTemplateForm();
  protected readonly editTemplateForm: TemplateForm = this.buildTemplateForm();

  // An apply failure is rendered by the apply panel alone: the facade holds the SAME Error instance,
  // so identity is what tells the page-level banner to stay silent instead of duplicating it.
  private readonly unhandledMutationError = computed(() => {
    const error = this.facade.mutationError();
    return error === null || error === this.applyErrorSignal() ? null : error;
  });

  protected readonly conflictMessage = computed(() => {
    const error = this.unhandledMutationError();
    return error instanceof AbwabConflictError ? ABWAB_CONFLICT_MESSAGES[error.code] : null;
  });

  protected readonly applyConflictMessage = computed(() => {
    const error = this.applyErrorSignal();
    return error instanceof AbwabConflictError ? ABWAB_CONFLICT_MESSAGES[error.code] : null;
  });

  protected readonly mutationErrorMessage = computed(() => {
    const error = this.unhandledMutationError();
    return error === null || error instanceof AbwabConflictError ? null : error.message;
  });

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => void this.facade.load(params.get(INCLUDE_DELETED_PARAM) === 'true'));
  }

  protected toggleIncludeDeleted(includeDeleted: boolean): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [INCLUDE_DELETED_PARAM]: includeDeleted ? 'true' : null },
      queryParamsHandling: 'merge',
    });
  }

  protected reload(): void {
    void this.facade.reload();
  }

  protected retryDetail(): void {
    void this.facade.retryDetail();
  }

  protected selectTemplate(doorTemplateId: string): void {
    this.editingTemplateId.set(null);
    this.applyStatusSignal.set('idle');
    this.applyErrorSignal.set(null);
    this.editorStatusSignal.set('idle');
    void this.facade.selectTemplate(doorTemplateId);
  }

  protected submitAddTemplate(): void {
    const generation = this.facade.expectedTimelineGeneration();
    if (generation === null || this.addTemplateForm.invalid) {
      return;
    }

    const { name, description } = this.addTemplateForm.getRawValue();
    void this.facade
      .addTemplate({ name, description: description || null, expectedTimelineGeneration: generation })
      // Unsaved input is preserved on conflict so the operator never retypes it (§14.3).
      .then(() => this.addTemplateForm.reset())
      .catch(() => undefined);
  }

  protected beginEditTemplate(): void {
    const template = this.facade.detail()?.template;
    if (!template) {
      return;
    }

    this.editingTemplateId.set(template.doorTemplateId);
    this.editTemplateForm.setValue({ name: template.name, description: template.description ?? '' });
  }

  protected cancelEditTemplate(): void {
    this.editingTemplateId.set(null);
  }

  protected submitEditTemplate(): void {
    const template = this.facade.detail()?.template;
    const generation = this.facade.expectedTimelineGeneration();
    if (!template || generation === null || this.editTemplateForm.invalid) {
      return;
    }

    const { name, description } = this.editTemplateForm.getRawValue();
    void this.facade
      .editTemplate(template.doorTemplateId, {
        name,
        description: description || null,
        expectedVersion: template.version,
        expectedTimelineGeneration: generation,
      })
      .then(() => this.editingTemplateId.set(null))
      .catch(() => undefined);
  }

  protected deleteTemplate(): void {
    const template = this.facade.detail()?.template;
    const generation = this.facade.expectedTimelineGeneration();
    if (!template || generation === null) {
      return;
    }

    void this.facade
      .deleteTemplate(template.doorTemplateId, { expectedVersion: template.version, expectedTimelineGeneration: generation })
      .catch(() => undefined);
  }

  protected restoreTemplate(doorTemplateId: string, version: number): void {
    const generation = this.facade.expectedTimelineGeneration();
    if (generation === null) {
      return;
    }

    void this.facade
      .restoreTemplate(doorTemplateId, { expectedVersion: version, expectedTimelineGeneration: generation })
      .catch(() => undefined);
  }

  protected onAddNode(request: TemplateNodeAddRequest): void {
    this.runEditorMutation((context) =>
      this.facade.addNode(context.doorTemplateId, {
        parentTemplateNodeId: request.parentTemplateNodeId,
        name: request.name,
        representativeQuranExcerpt: request.representativeQuranExcerpt,
        description: request.description,
        expectedTemplateRevision: context.templateRevision,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onEditNode(request: TemplateNodeEditRequest): void {
    this.runEditorMutation((context) =>
      this.facade.editNode(context.doorTemplateId, request.node.templateNodeId, {
        name: request.name,
        representativeQuranExcerpt: request.representativeQuranExcerpt,
        description: request.description,
        expectedVersion: request.node.version,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onReparentNode(request: TemplateNodeReparentRequest): void {
    this.runEditorMutation((context) =>
      this.facade.reparentNode(context.doorTemplateId, request.node.templateNodeId, {
        newParentTemplateNodeId: request.newParentTemplateNodeId,
        expectedVersion: request.node.version,
        expectedTemplateRevision: context.templateRevision,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onReorderNodes(request: TemplateNodeReorderRequest): void {
    this.runEditorMutation((context) =>
      this.facade.reorderNodes(context.doorTemplateId, {
        parentTemplateNodeId: request.parentTemplateNodeId,
        orderedTemplateNodeIds: [...request.orderedTemplateNodeIds],
        expectedTemplateRevision: context.templateRevision,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onRemoveNode(node: TemplateNodeDto): void {
    this.runEditorMutation((context) =>
      this.facade.removeNode(context.doorTemplateId, node.templateNodeId, {
        expectedVersion: node.version,
        expectedTemplateRevision: context.templateRevision,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onAddAlias(request: TemplateNodeAliasAddRequest): void {
    this.runEditorMutation((context) =>
      this.facade.addNodeAlias(context.doorTemplateId, request.node.templateNodeId, {
        value: request.value,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  // Every alias command carries the ALIAS row's own xmin: sending the node's would make an alias edit
  // succeed against a row another editor had already changed.
  protected onEditAlias(request: TemplateNodeAliasEditRequest): void {
    this.runEditorMutation((context) =>
      this.facade.editNodeAlias(context.doorTemplateId, request.alias.templateNodeSearchAliasId, {
        value: request.value,
        expectedVersion: request.alias.version,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onRemoveAlias(alias: TemplateNodeAliasDto): void {
    this.runEditorMutation((context) =>
      this.facade.removeNodeAlias(context.doorTemplateId, alias.templateNodeSearchAliasId, {
        expectedVersion: alias.version,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onRestoreAlias(alias: TemplateNodeAliasDto): void {
    this.runEditorMutation((context) =>
      this.facade.restoreNodeAlias(context.doorTemplateId, alias.templateNodeSearchAliasId, {
        expectedVersion: alias.version,
        expectedTimelineGeneration: context.generation,
      }),
    );
  }

  protected onApply(request: TemplateApplyRequest): void {
    const context = this.structuralContext();
    const treeRevision = this.facade.treeRevision();
    if (!context || treeRevision === null) {
      return;
    }

    this.applyStatusSignal.set('pending');
    this.applyErrorSignal.set(null);

    void this.facade
      .applyTemplate(context.doorTemplateId, {
        targetCategoryId: request.targetCategoryId,
        expectedTemplateRevision: context.templateRevision,
        expectedTreeRevision: treeRevision,
        expectedTargetVersion: request.expectedTargetVersion,
        expectedTimelineGeneration: context.generation,
      })
      .then(() => this.applyStatusSignal.set('success'))
      .catch((error: unknown) => {
        this.applyErrorSignal.set(error instanceof Error ? error : new Error(String(error)));
        this.applyStatusSignal.set(error instanceof AbwabConflictError ? 'conflict' : 'error');
      });
  }

  private runEditorMutation(mutation: (context: StructuralContext) => Promise<unknown>): void {
    const context = this.structuralContext();
    if (!context) {
      return;
    }

    this.editorStatusSignal.set('pending');
    void mutation(context)
      .then(() => this.editorStatusSignal.set('success'))
      .catch((error: unknown) => this.editorStatusSignal.set(error instanceof AbwabConflictError ? 'conflict' : 'error'));
  }

  private structuralContext(): StructuralContext | null {
    const detail = this.facade.detail();
    const generation = this.facade.expectedTimelineGeneration();
    return detail && generation !== null
      ? { doorTemplateId: detail.template.doorTemplateId, templateRevision: detail.template.templateRevision, generation }
      : null;
  }

  private buildTemplateForm(): TemplateForm {
    return this.formBuilder.group({
      name: this.formBuilder.control('', [Validators.required]),
      description: this.formBuilder.control(''),
    });
  }
}
