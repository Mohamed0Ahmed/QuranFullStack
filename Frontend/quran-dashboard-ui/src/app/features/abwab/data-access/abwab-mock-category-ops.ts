import { AddCategoryRequest, EditCategoryRequest } from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { normalizeArabicNameForUi } from './abwab-mock-normalize';
import { AbwabMockState, MockCategoryRow, permanentDefaultSectionId } from './abwab-mock-state';
import { activeCategories, assertGeneration, assertTreeRevision, bumpTreeRevision, nextSiblingOrder, MockClock } from './abwab-mock-shared';

export function addCategory(state: AbwabMockState, idFactory: () => string, request: AddCategoryRequest): string {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);

  const normalizedName = normalizeArabicNameForUi(request.name);
  const isRoot = request.parentCategoryId === null || request.parentCategoryId === undefined;

  if (isRoot) {
    const rootConflict = activeCategories(state).some(
      (category) => category.parentCategoryId === null && category.normalizedName === normalizedName,
    );
    if (rootConflict) {
      throw new AbwabConflictError('abwab.category_name_conflict');
    }
  } else {
    const parent = state.categories.get(request.parentCategoryId!);
    if (!parent || parent.deleted) {
      throw new AbwabConflictError('abwab.category_unavailable');
    }
    const siblingConflict = activeCategories(state).some(
      (category) => category.parentCategoryId === request.parentCategoryId && category.normalizedName === normalizedName,
    );
    if (siblingConflict) {
      throw new AbwabConflictError('abwab.category_name_conflict');
    }
  }

  const categoryId = idFactory();
  const parent = isRoot ? null : state.categories.get(request.parentCategoryId!)!;
  const sectionId = isRoot ? request.sectionId ?? permanentDefaultSectionId(state) : parent!.sectionId;

  const row: MockCategoryRow = {
    categoryId,
    name: request.name,
    normalizedName,
    description: request.description,
    representativeQuranExcerpt: request.representativeQuranExcerpt,
    parentCategoryId: isRoot ? null : request.parentCategoryId!,
    sectionId,
    siblingOrder: isRoot ? null : nextSiblingOrder(state, request.parentCategoryId!),
    sectionOrder: isRoot ? state.nextRootSectionOrder++ : null,
    globalOrder: isRoot ? state.nextRootGlobalOrder++ : null,
    ancestorIds: isRoot ? [] : [...parent!.ancestorIds, parent!.categoryId],
    depth: isRoot ? 0 : parent!.depth + 1,
    categoryContentRevision: 1,
    version: 1,
    deleted: false,
    deletionOperationId: null,
    lastEditedAtUtc: null,
    lastEditorSubject: null,
  };
  state.categories.set(categoryId, row);
  bumpTreeRevision(state);
  return categoryId;
}

export function editCategory(
  state: AbwabMockState,
  categoryId: string,
  request: EditCategoryRequest,
  actorSubject: string,
  clock: MockClock,
): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  const category = state.categories.get(categoryId);
  if (!category || category.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (category.version !== request.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }

  const normalizedName = normalizeArabicNameForUi(request.name);
  const siblingScope = category.parentCategoryId;
  const conflict = activeCategories(state).some(
    (other) =>
      other.categoryId !== categoryId &&
      other.normalizedName === normalizedName &&
      (siblingScope === null ? other.parentCategoryId === null : other.parentCategoryId === siblingScope),
  );
  if (conflict) {
    throw new AbwabConflictError('abwab.category_name_conflict');
  }

  category.name = request.name;
  category.normalizedName = normalizedName;
  category.description = request.description;
  category.representativeQuranExcerpt = request.representativeQuranExcerpt;
  category.version += 1;
  category.categoryContentRevision += 1;
  category.lastEditedAtUtc = clock.nowUtc();
  category.lastEditorSubject = actorSubject;
}
