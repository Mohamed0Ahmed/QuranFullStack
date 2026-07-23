import { OperationRestoreRequest, SubtreeDeleteRequest } from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { AbwabMockState } from './abwab-mock-state';
import { activeCategories, assertGeneration, assertTreeRevision, bumpTreeRevision } from './abwab-mock-shared';

export function subtreeDeleteCategory(
  state: AbwabMockState,
  idFactory: () => string,
  categoryId: string,
  request: SubtreeDeleteRequest,
): string {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);
  const root = state.categories.get(categoryId);
  if (!root || root.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (root.version !== request.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }

  const affected = activeCategories(state).filter(
    (category) => category.categoryId === categoryId || category.ancestorIds.includes(categoryId),
  );
  const deletionOperationId = idFactory();
  // Deterministic ID-order locking mirrors the backend's affected-row lock order (categories-api.md §7.1).
  for (const category of affected.sort((a, b) => a.categoryId.localeCompare(b.categoryId))) {
    category.deleted = true;
    category.deletionOperationId = deletionOperationId;
    category.version += 1;
  }
  bumpTreeRevision(state);
  return deletionOperationId;
}

export function operationRestoreCategory(
  state: AbwabMockState,
  deletionOperationId: string,
  request: OperationRestoreRequest,
): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);

  const affected = [...state.categories.values()].filter((category) => category.deletionOperationId === deletionOperationId);
  if (affected.length === 0) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }

  const parentFirst = affected.sort((a, b) => a.depth - b.depth);
  for (const category of parentFirst) {
    const siblingConflict = activeCategories(state).some(
      (other) =>
        other.categoryId !== category.categoryId &&
        other.parentCategoryId === category.parentCategoryId &&
        other.normalizedName === category.normalizedName,
    );
    if (siblingConflict) {
      throw new AbwabConflictError('abwab.category_name_conflict');
    }
  }
  for (const category of parentFirst) {
    category.deleted = false;
    category.deletionOperationId = null;
    category.version += 1;
  }
  bumpTreeRevision(state);
}
