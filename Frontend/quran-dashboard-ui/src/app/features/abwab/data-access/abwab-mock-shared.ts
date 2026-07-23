import { AbwabConflictError } from './abwab-conflict';
import { AbwabMockState, MockCategoryRow } from './abwab-mock-state';

export interface MockClock {
  nowUtc(): string;
}

export const SYSTEM_MOCK_CLOCK: MockClock = { nowUtc: () => new Date().toISOString() };

export function assertGeneration(state: AbwabMockState, expected: number): void {
  if (expected !== state.timelineGeneration) {
    throw new AbwabConflictError('abwab.timeline_generation_stale');
  }
}

export function assertTreeRevision(state: AbwabMockState, expected: number): void {
  if (expected !== state.treeRevision) {
    throw new AbwabConflictError('abwab.tree_revision_stale');
  }
}

export function bumpTreeRevision(state: AbwabMockState): void {
  state.treeRevision += 1;
}

export function activeCategories(state: AbwabMockState): MockCategoryRow[] {
  return [...state.categories.values()].filter((category) => !category.deleted);
}

export function isCategoryActiveRootOfSection(state: AbwabMockState, sectionId: string): boolean {
  return activeCategories(state).some((category) => category.sectionId === sectionId && category.parentCategoryId === null);
}

export function nextSiblingOrder(state: AbwabMockState, parentCategoryId: string): number {
  const siblingOrders = activeCategories(state)
    .filter((category) => category.parentCategoryId === parentCategoryId)
    .map((category) => category.siblingOrder ?? -1);
  return Math.max(-1, ...siblingOrders) + 1;
}

export function isDescendantOf(state: AbwabMockState, candidateId: string, ancestorId: string): boolean {
  const candidate = state.categories.get(candidateId);
  return !!candidate && candidate.ancestorIds.includes(ancestorId);
}
