import { AddCategoryAliasRequest, EditCategoryAliasRequest, RemoveCategoryAliasRequest } from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { normalizeArabicNameForUi } from './abwab-mock-normalize';
import { AbwabMockState } from './abwab-mock-state';
import { assertGeneration } from './abwab-mock-shared';

export function addCategoryAlias(
  state: AbwabMockState,
  idFactory: () => string,
  categoryId: string,
  request: AddCategoryAliasRequest,
): string {
  assertGeneration(state, request.expectedTimelineGeneration);
  const category = state.categories.get(categoryId);
  if (!category || category.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  const normalizedValue = normalizeArabicNameForUi(request.value);
  const conflict = [...state.aliases.values()].some(
    (alias) => !alias.deleted && alias.categoryId === categoryId && alias.normalizedValue === normalizedValue,
  );
  if (conflict) {
    throw new AbwabConflictError('abwab.category_alias_conflict');
  }
  const aliasId = idFactory();
  state.aliases.set(aliasId, { aliasId, categoryId, value: request.value, normalizedValue, version: 1, deleted: false });
  category.categoryContentRevision += 1;
  return aliasId;
}

export function editCategoryAlias(state: AbwabMockState, aliasId: string, request: EditCategoryAliasRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const alias = state.aliases.get(aliasId);
  if (!alias || alias.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (alias.version !== request.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  const normalizedValue = normalizeArabicNameForUi(request.value);
  const conflict = [...state.aliases.values()].some(
    (other) => other.aliasId !== aliasId && !other.deleted && other.categoryId === alias.categoryId && other.normalizedValue === normalizedValue,
  );
  if (conflict) {
    throw new AbwabConflictError('abwab.category_alias_conflict');
  }
  alias.value = request.value;
  alias.normalizedValue = normalizedValue;
  alias.version += 1;
  bumpOwningCategoryContentRevision(state, alias.categoryId);
}

export function removeCategoryAlias(state: AbwabMockState, aliasId: string, request: RemoveCategoryAliasRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const alias = state.aliases.get(aliasId);
  if (!alias || alias.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (alias.version !== request.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  // Removal is a tracked soft delete; the row is never physically deleted (categories-api.md §7.1).
  alias.deleted = true;
  alias.version += 1;
  bumpOwningCategoryContentRevision(state, alias.categoryId);
}

function bumpOwningCategoryContentRevision(state: AbwabMockState, categoryId: string): void {
  const category = state.categories.get(categoryId);
  if (category) {
    category.categoryContentRevision += 1;
  }
}
