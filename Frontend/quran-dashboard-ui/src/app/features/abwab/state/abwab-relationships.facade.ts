import { Injectable, computed, inject, signal } from '@angular/core';

import {
  AddRelationshipRequest,
  CategorySnapshotDto,
  CategoryRelationshipListDto,
  DeleteRelationshipRequest,
  EditRelationshipRequest,
  RestoreRelationshipRequest,
} from '../../../core/api/generated/models';
import { ABWAB_CORE_PORT } from '../data-access/abwab-core.port';
import { AbwabRelationshipsCache } from '../data-access/abwab-relationships-cache';
import { AbwabConflictError } from '../data-access/abwab-conflict';
import { ABWAB_RELATIONSHIPS_PORT } from '../data-access/abwab-relationships.port';

export type AbwabRelationshipsStatus = 'idle' | 'loading' | 'ready' | 'empty' | 'error';
export type AbwabRelationshipsMutationStatus = 'idle' | 'pending' | 'success' | 'error' | 'conflict';

const LOAD_ERROR = 'تعذّر تحميل علاقات الباب. حاول مرة أخرى.';

// Every mutation runs through runMutation(), the SINGLE place the cache rule lives: invalidate, then
// reload from the server — on success AND on conflict. Nothing is applied to the rendered list ahead
// of server confirmation, so a conflict re-syncs to server truth instead of undoing a local change.
@Injectable()
export class AbwabRelationshipsFacade {
  private readonly port = inject(ABWAB_RELATIONSHIPS_PORT);
  private readonly corePort = inject(ABWAB_CORE_PORT);
  private readonly cache = inject(AbwabRelationshipsCache);
  private readonly allCategoriesSignal = signal<readonly CategorySnapshotDto[]>([]);

  private readonly listSignal = signal<CategoryRelationshipListDto | null>(null);
  private readonly statusSignal = signal<AbwabRelationshipsStatus>('idle');
  private readonly errorMessageSignal = signal<string | null>(null);
  private readonly categoryIdSignal = signal<string | null>(null);
  private readonly includeDeletedSignal = signal(false);
  private readonly selectedRelationshipIdSignal = signal<string | null>(null);
  private readonly mutationStatusSignal = signal<AbwabRelationshipsMutationStatus>('idle');
  private readonly mutationErrorSignal = signal<Error | null>(null);

  readonly list = this.listSignal.asReadonly();
  readonly status = this.statusSignal.asReadonly();
  readonly errorMessage = this.errorMessageSignal.asReadonly();
  readonly categoryId = this.categoryIdSignal.asReadonly();
  readonly includeDeleted = this.includeDeletedSignal.asReadonly();
  readonly selectedRelationshipId = this.selectedRelationshipIdSignal.asReadonly();
  readonly mutationStatus = this.mutationStatusSignal.asReadonly();
  readonly mutationError = this.mutationErrorSignal.asReadonly();

  readonly relationships = computed(() => this.listSignal()?.relationships ?? []);
  readonly endpointCandidates = computed(() =>
    this.allCategoriesSignal().filter((category) => category.categoryId !== this.categoryIdSignal()),
  );
  readonly routedCategoryName = computed(
    () => this.allCategoriesSignal().find((category) => category.categoryId === this.categoryIdSignal())?.name ?? null,
  );
  readonly expectedTimelineGeneration = computed(() => this.listSignal()?.expectedTimelineGeneration.generation ?? null);
  readonly selectedRelationship = computed(
    () => this.relationships().find((relationship) => relationship.categoryRelationshipId === this.selectedRelationshipIdSignal()) ?? null,
  );

  async load(categoryId: string, includeDeleted = false): Promise<void> {
    this.categoryIdSignal.set(categoryId);
    this.includeDeletedSignal.set(includeDeleted);

    const cached = await this.cache.getCached(categoryId, includeDeleted);
    if (cached) {
      this.listSignal.set(cached);
      this.statusSignal.set(cached.relationships.length === 0 ? 'empty' : 'ready');
    } else {
      this.statusSignal.set('loading');
    }
    await Promise.all([this.reload(), this.loadEndpointCandidates()]);
  }

  // The endpoint picker is a convenience over the 029 tree projection; failing to populate it must
  // never take the relationship list itself out of its ready/empty state.
  private async loadEndpointCandidates(): Promise<void> {
    try {
      this.allCategoriesSignal.set((await this.corePort.getTreeSnapshot()).allCategoriesProjection);
    } catch {
      this.allCategoriesSignal.set([]);
    }
  }

  async reload(): Promise<void> {
    const categoryId = this.categoryIdSignal();
    if (categoryId === null) {
      return;
    }

    const includeDeleted = this.includeDeletedSignal();
    try {
      const list = await this.port.getRelationships(categoryId, includeDeleted);
      await this.cache.store(categoryId, includeDeleted, list);
      this.listSignal.set(list);
      this.statusSignal.set(list.relationships.length === 0 ? 'empty' : 'ready');
      this.errorMessageSignal.set(null);
      this.pruneContextAfterReload(list);
    } catch {
      this.statusSignal.set('error');
      this.errorMessageSignal.set(LOAD_ERROR);
    }
  }

  selectRelationship(relationshipId: string | null): void {
    this.selectedRelationshipIdSignal.set(relationshipId);
  }

  addRelationship(request: AddRelationshipRequest) {
    return this.runMutation(() => this.port.addRelationship(request));
  }

  editRelationship(relationshipId: string, request: EditRelationshipRequest) {
    return this.runMutation(() => this.port.editRelationship(relationshipId, request));
  }

  deleteRelationship(relationshipId: string, request: DeleteRelationshipRequest) {
    return this.runMutation(() => this.port.deleteRelationship(relationshipId, request));
  }

  restoreRelationship(relationshipId: string, request: RestoreRelationshipRequest) {
    return this.runMutation(() => this.port.restoreRelationship(relationshipId, request));
  }

  private async runMutation<T>(operation: () => Promise<T>): Promise<T> {
    this.mutationStatusSignal.set('pending');
    this.mutationErrorSignal.set(null);
    try {
      const result = await operation();
      await this.invalidateAndReload();
      this.mutationStatusSignal.set('success');
      return result;
    } catch (error) {
      await this.invalidateAndReload();
      this.mutationStatusSignal.set(error instanceof AbwabConflictError ? 'conflict' : 'error');
      this.mutationErrorSignal.set(error instanceof Error ? error : new Error(String(error)));
      throw error;
    }
  }

  private async invalidateAndReload(): Promise<void> {
    const categoryId = this.categoryIdSignal();
    if (categoryId !== null) {
      await this.cache.invalidate(categoryId);
    }
    await this.reload();
  }

  // Selection survives a reload as long as the relationship is still in the fresh projection; only
  // an id that genuinely disappeared (deleted, or gone dormant) is dropped.
  private pruneContextAfterReload(list: CategoryRelationshipListDto): void {
    const selected = this.selectedRelationshipIdSignal();
    if (selected === null) {
      return;
    }

    const stillPresent = list.relationships.some((relationship) => relationship.categoryRelationshipId === selected);
    if (!stillPresent) {
      this.selectedRelationshipIdSignal.set(null);
    }
  }
}
