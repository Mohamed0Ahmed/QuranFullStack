import { CategoryMoveEntryRequest, CategoryOrderScope, MoveCategoriesRequest, ReorderCategoriesRequest } from '../../../core/api/generated/models';
import { AbwabConflictError } from './abwab-conflict';
import { AbwabMockState, MockCategoryRow, permanentDefaultSectionId } from './abwab-mock-state';
import { activeCategories, assertGeneration, assertTreeRevision, bumpTreeRevision, isDescendantOf, nextSiblingOrder } from './abwab-mock-shared';

export function moveCategories(state: AbwabMockState, request: MoveCategoriesRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);

  const movedIds = new Set(request.moves.map((move) => move.categoryId));
  for (const move of request.moves) {
    for (const otherId of movedIds) {
      if (otherId !== move.categoryId && isDescendantOf(state, otherId, move.categoryId)) {
        throw new AbwabConflictError('abwab.category_overlapping_move');
      }
    }
  }

  for (const move of request.moves) {
    validateSingleMove(state, move);
  }
  for (const move of request.moves) {
    applySingleMove(state, move);
  }
  bumpTreeRevision(state);
}

function validateSingleMove(state: AbwabMockState, move: CategoryMoveEntryRequest): void {
  const category = state.categories.get(move.categoryId);
  if (!category || category.deleted) {
    throw new AbwabConflictError('abwab.category_unavailable');
  }
  if (category.version !== move.expectedVersion) {
    throw new AbwabConflictError('abwab.row_stale');
  }
  if (move.newParentCategoryId === move.categoryId) {
    throw new AbwabConflictError('abwab.category_cycle');
  }
  if (move.newParentCategoryId && isDescendantOf(state, move.newParentCategoryId, move.categoryId)) {
    throw new AbwabConflictError('abwab.category_cycle');
  }
  if (move.newParentCategoryId) {
    const destination = state.categories.get(move.newParentCategoryId);
    if (!destination || destination.deleted) {
      throw new AbwabConflictError('abwab.category_unavailable');
    }
  }
  if (move.newSectionId) {
    const destinationSection = state.sections.get(move.newSectionId);
    if (!destinationSection || destinationSection.deleted) {
      throw new AbwabConflictError('abwab.category_unavailable');
    }
  }
}

function applySingleMove(state: AbwabMockState, move: CategoryMoveEntryRequest): void {
  const category = state.categories.get(move.categoryId)!;
  const becomingRoot = move.newParentCategoryId === null || move.newParentCategoryId === undefined;
  const previousGlobalOrder = category.globalOrder;

  if (becomingRoot) {
    category.parentCategoryId = null;
    category.sectionId = move.newSectionId ?? category.sectionId ?? permanentDefaultSectionId(state);
    category.ancestorIds = [];
    category.depth = 0;
    category.siblingOrder = null;
    category.sectionOrder = category.sectionOrder ?? state.nextRootSectionOrder++;
    // Global order is preserved across a section move unless a global reorder is issued
    // in the same audited operation (categories-api.md §7.1).
    category.globalOrder = previousGlobalOrder ?? state.nextRootGlobalOrder++;
  } else {
    const parent = state.categories.get(move.newParentCategoryId!)!;
    category.parentCategoryId = parent.categoryId;
    category.sectionId = parent.sectionId;
    category.ancestorIds = [...parent.ancestorIds, parent.categoryId];
    category.depth = parent.depth + 1;
    category.siblingOrder = nextSiblingOrder(state, parent.categoryId);
    category.sectionOrder = null;
    category.globalOrder = null;
  }
  category.version += 1;

  rewriteDescendantAncestry(state, category.categoryId);
}

function rewriteDescendantAncestry(state: AbwabMockState, rootId: string): void {
  const root = state.categories.get(rootId)!;
  const children = activeCategories(state).filter((category) => category.parentCategoryId === rootId);
  for (const child of children) {
    child.ancestorIds = [...root.ancestorIds, root.categoryId];
    child.depth = root.depth + 1;
    rewriteDescendantAncestry(state, child.categoryId);
  }
}

const SIBLINGS: CategoryOrderScope = 0;
const SECTION_ROOTS: CategoryOrderScope = 1;

export function reorderCategories(state: AbwabMockState, request: ReorderCategoriesRequest): void {
  assertGeneration(state, request.expectedTimelineGeneration);
  assertTreeRevision(state, request.expectedTreeRevision);

  for (const order of request.orders) {
    const category = state.categories.get(order.categoryId);
    if (!category || category.deleted) {
      throw new AbwabConflictError('abwab.category_unavailable');
    }
    if (category.version !== order.expectedVersion) {
      throw new AbwabConflictError('abwab.row_stale');
    }
  }

  for (const order of request.orders) {
    const category = state.categories.get(order.categoryId)!;
    applyOrderScope(category, request.scope, order.newOrder);
    category.version += 1;
  }
  bumpTreeRevision(state);
}

function applyOrderScope(category: MockCategoryRow, scope: CategoryOrderScope, newOrder: number): void {
  if (scope === SIBLINGS) {
    category.siblingOrder = newOrder;
  } else if (scope === SECTION_ROOTS) {
    category.sectionOrder = newOrder;
  } else {
    category.globalOrder = newOrder;
  }
}
