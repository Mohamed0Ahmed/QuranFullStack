import { Injectable, computed, inject, signal } from '@angular/core';

import {
  AddDoorTemplateRequest,
  AddTemplateNodeAliasRequest,
  AddTemplateNodeRequest,
  ApplyDoorTemplateRequest,
  CategorySnapshotDto,
  DeleteDoorTemplateRequest,
  DoorTemplateDetailDto,
  DoorTemplateListDto,
  EditDoorTemplateRequest,
  EditTemplateNodeAliasRequest,
  EditTemplateNodeRequest,
  RemoveTemplateNodeAliasRequest,
  RemoveTemplateNodeRequest,
  ReorderTemplateNodesRequest,
  ReparentTemplateNodeRequest,
  RestoreDoorTemplateRequest,
  RestoreTemplateNodeAliasRequest,
} from '../../../core/api/generated/models';
import { AbwabTreeCache } from '../data-access/abwab-cache';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabConflictError } from '../data-access/abwab-conflict';
import { AbwabTemplatesCache } from '../data-access/abwab-templates-cache';
import { ABWAB_TEMPLATES_PORT } from '../data-access/abwab-templates.port';

export type AbwabTemplatesStatus = 'idle' | 'loading' | 'ready' | 'empty' | 'error';
export type AbwabTemplatesMutationStatus = 'idle' | 'pending' | 'success' | 'error' | 'conflict';

const LOAD_ERROR = 'تعذّر تحميل قوالب الأبواب. حاول مرة أخرى.';
const DETAIL_ERROR = 'تعذّر تحميل تفاصيل القالب. حاول مرة أخرى.';

// Every mutation runs through runMutation(), the SINGLE place the cache rule lives: invalidate, then
// reload from the server — on success AND on conflict. Nothing is applied to the rendered projection
// ahead of server confirmation, so a conflict re-syncs to server truth instead of undoing locally.
@Injectable()
export class AbwabTemplatesFacade {
  private readonly port = inject(ABWAB_TEMPLATES_PORT);
  private readonly corePort = inject(ABWAB_CORE_PORT);
  private readonly cache = inject(AbwabTemplatesCache);

  // The 029 tree snapshot cache, reached the same way the 029 facade reaches it (a direct instance
  // over the shared IndexedDB database, not DI): a successful application creates real categories,
  // so leaving that snapshot cached would show the operator a tree without the new doors.
  private readonly treeCache = new AbwabTreeCache();

  private readonly listSignal = signal<DoorTemplateListDto | null>(null);
  private readonly detailSignal = signal<DoorTemplateDetailDto | null>(null);
  private readonly statusSignal = signal<AbwabTemplatesStatus>('idle');
  private readonly errorMessageSignal = signal<string | null>(null);
  private readonly detailErrorMessageSignal = signal<string | null>(null);
  private readonly includeDeletedSignal = signal(false);
  private readonly selectedTemplateIdSignal = signal<string | null>(null);
  private readonly mutationStatusSignal = signal<AbwabTemplatesMutationStatus>('idle');
  private readonly mutationErrorSignal = signal<Error | null>(null);
  private readonly targetCandidatesSignal = signal<readonly CategorySnapshotDto[]>([]);

  readonly list = this.listSignal.asReadonly();
  readonly detail = this.detailSignal.asReadonly();
  readonly status = this.statusSignal.asReadonly();
  readonly errorMessage = this.errorMessageSignal.asReadonly();
  readonly detailErrorMessage = this.detailErrorMessageSignal.asReadonly();
  readonly includeDeleted = this.includeDeletedSignal.asReadonly();
  readonly selectedTemplateId = this.selectedTemplateIdSignal.asReadonly();
  readonly mutationStatus = this.mutationStatusSignal.asReadonly();
  readonly mutationError = this.mutationErrorSignal.asReadonly();
  readonly targetCandidates = this.targetCandidatesSignal.asReadonly();

  readonly templates = computed(() => this.listSignal()?.templates ?? []);
  readonly nodes = computed(() => this.detailSignal()?.nodes ?? []);
  readonly expectedTimelineGeneration = computed(() => this.listSignal()?.expectedTimelineGeneration.generation ?? null);
  readonly templateRevision = computed(() => this.detailSignal()?.template.templateRevision ?? null);
  readonly treeRevision = computed(() => this.detailSignal()?.treeRevision ?? null);

  async load(includeDeleted = false): Promise<void> {
    this.includeDeletedSignal.set(includeDeleted);

    const cached = await this.cache.getCachedList(includeDeleted);
    if (cached) {
      this.listSignal.set(cached);
      this.statusSignal.set(cached.templates.length === 0 ? 'empty' : 'ready');
    } else {
      this.statusSignal.set('loading');
    }

    await Promise.all([this.reload(), this.loadTargetCandidates()]);
  }

  async selectTemplate(doorTemplateId: string | null): Promise<void> {
    this.selectedTemplateIdSignal.set(doorTemplateId);
    await this.reloadDetail();
  }

  retryDetail(): Promise<void> {
    return this.reloadDetail();
  }

  async reload(): Promise<void> {
    const includeDeleted = this.includeDeletedSignal();
    try {
      const list = await this.port.getTemplates(includeDeleted);
      await this.cache.storeList(includeDeleted, list);
      this.listSignal.set(list);
      this.statusSignal.set(list.templates.length === 0 ? 'empty' : 'ready');
      this.errorMessageSignal.set(null);
      this.pruneContextAfterReload(list);
    } catch {
      this.statusSignal.set('error');
      this.errorMessageSignal.set(LOAD_ERROR);
      return;
    }

    await this.reloadDetail();
  }

  addTemplate(request: AddDoorTemplateRequest) {
    return this.runMutation(null, () => this.port.addTemplate(request));
  }

  editTemplate(doorTemplateId: string, request: EditDoorTemplateRequest) {
    return this.runMutation(doorTemplateId, () => this.port.editTemplate(doorTemplateId, request));
  }

  deleteTemplate(doorTemplateId: string, request: DeleteDoorTemplateRequest) {
    return this.runMutation(doorTemplateId, () => this.port.deleteTemplate(doorTemplateId, request));
  }

  restoreTemplate(doorTemplateId: string, request: RestoreDoorTemplateRequest) {
    return this.runMutation(doorTemplateId, () => this.port.restoreTemplate(doorTemplateId, request));
  }

  addNode(doorTemplateId: string, request: AddTemplateNodeRequest) {
    return this.runMutation(doorTemplateId, () => this.port.addNode(doorTemplateId, request));
  }

  editNode(doorTemplateId: string, templateNodeId: string, request: EditTemplateNodeRequest) {
    return this.runMutation(doorTemplateId, () => this.port.editNode(templateNodeId, request));
  }

  reparentNode(doorTemplateId: string, templateNodeId: string, request: ReparentTemplateNodeRequest) {
    return this.runMutation(doorTemplateId, () => this.port.reparentNode(templateNodeId, request));
  }

  reorderNodes(doorTemplateId: string, request: ReorderTemplateNodesRequest) {
    return this.runMutation(doorTemplateId, () => this.port.reorderNodes(doorTemplateId, request));
  }

  removeNode(doorTemplateId: string, templateNodeId: string, request: RemoveTemplateNodeRequest) {
    return this.runMutation(doorTemplateId, () => this.port.removeNode(templateNodeId, request));
  }

  addNodeAlias(doorTemplateId: string, templateNodeId: string, request: AddTemplateNodeAliasRequest) {
    return this.runMutation(doorTemplateId, () => this.port.addNodeAlias(templateNodeId, request));
  }

  editNodeAlias(doorTemplateId: string, aliasId: string, request: EditTemplateNodeAliasRequest) {
    return this.runMutation(doorTemplateId, () => this.port.editNodeAlias(aliasId, request));
  }

  removeNodeAlias(doorTemplateId: string, aliasId: string, request: RemoveTemplateNodeAliasRequest) {
    return this.runMutation(doorTemplateId, () => this.port.removeNodeAlias(aliasId, request));
  }

  restoreNodeAlias(doorTemplateId: string, aliasId: string, request: RestoreTemplateNodeAliasRequest) {
    return this.runMutation(doorTemplateId, () => this.port.restoreNodeAlias(aliasId, request));
  }

  async applyTemplate(doorTemplateId: string, request: ApplyDoorTemplateRequest): Promise<void> {
    await this.runMutation(doorTemplateId, async () => {
      await this.port.applyTemplate(doorTemplateId, request);
      await this.treeCache.invalidate();
    });
  }

  // The target picker is a convenience over the 029 tree projection; failing to populate it must
  // never take the template list itself out of its ready/empty state.
  private async loadTargetCandidates(): Promise<void> {
    try {
      this.targetCandidatesSignal.set((await this.corePort.getTreeSnapshot()).allCategoriesProjection);
    } catch {
      this.targetCandidatesSignal.set([]);
    }
  }

  // A detail read that fails keeps the selection and offers a retry rather than silently emptying the
  // panel. A template that genuinely disappeared is NOT handled here: reads never return a conflict,
  // so the only honest signal is its absence from the fresh list projection, which
  // pruneContextAfterReload acts on before this method runs.
  private async reloadDetail(): Promise<void> {
    const doorTemplateId = this.selectedTemplateIdSignal();
    if (doorTemplateId === null) {
      this.detailSignal.set(null);
      this.detailErrorMessageSignal.set(null);
      return;
    }

    const includeDeleted = this.includeDeletedSignal();
    const cached = await this.cache.getCachedDetail(doorTemplateId, includeDeleted);
    if (cached) {
      this.detailSignal.set(cached);
    }

    try {
      const detail = await this.port.getTemplate(doorTemplateId, includeDeleted);
      await this.cache.storeDetail(doorTemplateId, includeDeleted, detail);
      this.detailSignal.set(detail);
      this.detailErrorMessageSignal.set(null);
    } catch {
      this.detailErrorMessageSignal.set(DETAIL_ERROR);
    }
  }

  private async runMutation<T>(doorTemplateId: string | null, operation: () => Promise<T>): Promise<T> {
    this.mutationStatusSignal.set('pending');
    this.mutationErrorSignal.set(null);
    try {
      const result = await operation();
      await this.invalidateAndReload(doorTemplateId);
      this.mutationStatusSignal.set('success');
      return result;
    } catch (error) {
      await this.invalidateAndReload(doorTemplateId);
      this.mutationStatusSignal.set(error instanceof AbwabConflictError ? 'conflict' : 'error');
      this.mutationErrorSignal.set(error instanceof Error ? error : new Error(String(error)));
      throw error;
    }
  }

  private async invalidateAndReload(doorTemplateId: string | null): Promise<void> {
    await this.cache.invalidate(doorTemplateId ?? this.selectedTemplateIdSignal());
    await this.reload();
  }

  // Selection survives a reload as long as the template is still in the fresh projection; only an id
  // that genuinely disappeared is dropped.
  private pruneContextAfterReload(list: DoorTemplateListDto): void {
    const selected = this.selectedTemplateIdSignal();
    if (selected !== null && !list.templates.some((template) => template.doorTemplateId === selected)) {
      this.selectedTemplateIdSignal.set(null);
      this.detailErrorMessageSignal.set(null);
    }
  }
}
