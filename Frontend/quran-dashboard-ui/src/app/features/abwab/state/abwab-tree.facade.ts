import { Injectable, computed, inject, signal } from '@angular/core';

import {
  AbwabTreeSnapshotDto,
  AddCategoryAliasRequest,
  AddCategoryRequest,
  AddSectionRequest,
  ApplyFullProtectionPresetRequest,
  ApplyManualProtectionRequest,
  CategoryProtectionProfileDto,
  CategorySearchResultDto,
  DeleteSectionRequest,
  EditCategoryAliasRequest,
  EditCategoryRequest,
  EditSectionRequest,
  LiftManualProtectionRequest,
  ManualProtectionType,
  MoveCategoriesRequest,
  OperationRestoreRequest,
  RemoveCategoryAliasRequest,
  ReorderCategoriesRequest,
  ReorderSectionsRequest,
  SubtreeDeleteRequest,
} from '../../../core/api/generated/models';
import { AbwabTreeCache } from '../data-access/abwab-cache';
import { AbwabConflictError } from '../data-access/abwab-conflict';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';

export type AbwabTreeStatus = 'idle' | 'loading' | 'ready' | 'error';
export type AbwabMutationStatus = 'idle' | 'pending' | 'success' | 'error' | 'conflict';

const LOAD_ERROR = 'تعذّر تحميل شجرة الأبواب. حاول مرة أخرى.';

// Every mutation runs through runMutation(), the SINGLE place the cache-invalidate rule lives
// (abwab-cache.ts): invalidate, then reload from the server. A conflict does the SAME invalidate +
// reload — that IS this facade's "rollback": nothing is ever applied to the rendered snapshot ahead
// of server confirmation, so a conflict simply re-syncs to server truth instead of undoing a change.
@Injectable()
export class AbwabTreeFacade {
  private readonly port = inject(ABWAB_CORE_PORT);
  private readonly cache = new AbwabTreeCache();

  private readonly snapshotSignal = signal<AbwabTreeSnapshotDto | null>(null);
  private readonly statusSignal = signal<AbwabTreeStatus>('idle');
  private readonly errorMessageSignal = signal<string | null>(null);
  private readonly selectedCategoryIdSignal = signal<string | null>(null);
  private readonly expandedCategoryIdsSignal = signal<ReadonlySet<string>>(new Set());
  private readonly searchQuerySignal = signal('');
  private readonly searchResultsSignal = signal<CategorySearchResultDto | null>(null);
  private readonly mutationStatusSignal = signal<AbwabMutationStatus>('idle');
  private readonly mutationErrorSignal = signal<Error | null>(null);

  readonly snapshot = this.snapshotSignal.asReadonly();
  readonly status = this.statusSignal.asReadonly();
  readonly errorMessage = this.errorMessageSignal.asReadonly();
  readonly selectedCategoryId = this.selectedCategoryIdSignal.asReadonly();
  readonly expandedCategoryIds = this.expandedCategoryIdsSignal.asReadonly();
  readonly searchQuery = this.searchQuerySignal.asReadonly();
  readonly searchResults = this.searchResultsSignal.asReadonly();
  readonly mutationStatus = this.mutationStatusSignal.asReadonly();
  readonly mutationError = this.mutationErrorSignal.asReadonly();

  readonly sections = computed(() => this.snapshotSignal()?.sections ?? []);
  readonly categories = computed(() => this.snapshotSignal()?.categories ?? []);
  readonly selectedCategory = computed(
    () => this.categories().find((category) => category.categoryId === this.selectedCategoryIdSignal()) ?? null,
  );

  async load(): Promise<void> {
    const cached = await this.cache.getCached();
    if (cached) {
      this.snapshotSignal.set(cached);
      this.statusSignal.set('ready');
    } else {
      this.statusSignal.set('loading');
    }
    await this.reload();
  }

  async reload(): Promise<void> {
    try {
      const snapshot = await this.port.getTreeSnapshot();
      await this.cache.store(snapshot);
      this.snapshotSignal.set(snapshot);
      this.statusSignal.set('ready');
      this.errorMessageSignal.set(null);
      this.pruneContextAfterReload(snapshot);
    } catch {
      this.statusSignal.set('error');
      this.errorMessageSignal.set(LOAD_ERROR);
    }
  }

  selectCategory(categoryId: string | null): void {
    this.selectedCategoryIdSignal.set(categoryId);
  }

  toggleExpanded(categoryId: string): void {
    const expanded = new Set(this.expandedCategoryIdsSignal());
    if (expanded.has(categoryId)) {
      expanded.delete(categoryId);
    } else {
      expanded.add(categoryId);
    }
    this.expandedCategoryIdsSignal.set(expanded);
  }

  async search(query: string): Promise<void> {
    this.searchQuerySignal.set(query);
    if (query.trim().length === 0) {
      this.searchResultsSignal.set(null);
      return;
    }
    this.searchResultsSignal.set(await this.port.search(query));
  }

  loadProtectionProfile(categoryId: string): Promise<CategoryProtectionProfileDto> {
    return this.port.getProtectionProfile(categoryId);
  }

  addSection(request: AddSectionRequest) {
    return this.runMutation(() => this.port.addSection(request));
  }

  editSection(sectionId: string, request: EditSectionRequest) {
    return this.runMutation(() => this.port.editSection(sectionId, request));
  }

  reorderSections(request: ReorderSectionsRequest) {
    return this.runMutation(() => this.port.reorderSections(request));
  }

  deleteSection(sectionId: string, request: DeleteSectionRequest) {
    return this.runMutation(() => this.port.deleteSection(sectionId, request));
  }

  addCategory(request: AddCategoryRequest) {
    return this.runMutation(() => this.port.addCategory(request));
  }

  editCategory(categoryId: string, request: EditCategoryRequest) {
    return this.runMutation(() => this.port.editCategory(categoryId, request));
  }

  moveCategories(request: MoveCategoriesRequest) {
    return this.runMutation(() => this.port.moveCategories(request));
  }

  reorderCategories(request: ReorderCategoriesRequest) {
    return this.runMutation(() => this.port.reorderCategories(request));
  }

  subtreeDeleteCategory(categoryId: string, request: SubtreeDeleteRequest) {
    return this.runMutation(() => this.port.subtreeDeleteCategory(categoryId, request));
  }

  operationRestoreCategory(deletionOperationId: string, request: OperationRestoreRequest) {
    return this.runMutation(() => this.port.operationRestoreCategory(deletionOperationId, request));
  }

  addCategoryAlias(categoryId: string, request: AddCategoryAliasRequest) {
    return this.runMutation(() => this.port.addCategoryAlias(categoryId, request));
  }

  editCategoryAlias(aliasId: string, request: EditCategoryAliasRequest) {
    return this.runMutation(() => this.port.editCategoryAlias(aliasId, request));
  }

  removeCategoryAlias(aliasId: string, request: RemoveCategoryAliasRequest) {
    return this.runMutation(() => this.port.removeCategoryAlias(aliasId, request));
  }

  applyManualProtection(categoryId: string, protectionType: ManualProtectionType, request: ApplyManualProtectionRequest) {
    return this.runMutation(() => this.port.applyManualProtection(categoryId, protectionType, request));
  }

  liftManualProtection(categoryId: string, protectionType: ManualProtectionType, request: LiftManualProtectionRequest) {
    return this.runMutation(() => this.port.liftManualProtection(categoryId, protectionType, request));
  }

  applyFullProtectionPreset(categoryId: string, request: ApplyFullProtectionPresetRequest) {
    return this.runMutation(() => this.port.applyFullProtectionPreset(categoryId, request));
  }

  private async runMutation<T>(operation: () => Promise<T>): Promise<T> {
    this.mutationStatusSignal.set('pending');
    this.mutationErrorSignal.set(null);
    try {
      const result = await operation();
      await this.cache.invalidate();
      await this.reload();
      this.mutationStatusSignal.set('success');
      return result;
    } catch (error) {
      await this.cache.invalidate();
      await this.reload();
      if (error instanceof AbwabConflictError) {
        this.mutationStatusSignal.set('conflict');
      } else {
        this.mutationStatusSignal.set('error');
      }
      this.mutationErrorSignal.set(error instanceof Error ? error : new Error(String(error)));
      throw error;
    }
  }

  // Selection/expansion survive a reload as long as the category still exists in the fresh
  // snapshot; only ids that truly disappeared (e.g. a subtree delete) are pruned. This is the
  // post-mutation context-preservation contract (SC-015): a reload never blindly resets the view.
  private pruneContextAfterReload(snapshot: AbwabTreeSnapshotDto): void {
    const liveIds = new Set(snapshot.categories.map((category) => category.categoryId));

    const selected = this.selectedCategoryIdSignal();
    if (selected && !liveIds.has(selected)) {
      this.selectedCategoryIdSignal.set(null);
    }

    const expanded = this.expandedCategoryIdsSignal();
    const prunedExpanded = new Set([...expanded].filter((id) => liveIds.has(id)));
    if (prunedExpanded.size !== expanded.size) {
      this.expandedCategoryIdsSignal.set(prunedExpanded);
    }
  }
}
