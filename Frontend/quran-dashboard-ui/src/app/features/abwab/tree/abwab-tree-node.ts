import { CategorySnapshotDto } from '../../../core/api/generated/models';

export interface AbwabVisibleTreeNode {
  readonly category: CategorySnapshotDto;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

function orderKey(category: CategorySnapshotDto): number {
  return category.parentCategoryId === null ? category.sectionOrder ?? 0 : category.siblingOrder ?? 0;
}

// Flattens the category snapshot into the currently-VISIBLE rows only (a node's descendants are
// included iff every ancestor up to the root is expanded). This is what makes the tree cheap to
// virtualize (abwab-tree-view.component.ts) regardless of total tree size.
export function buildVisibleTreeNodes(
  categories: readonly CategorySnapshotDto[],
  expandedCategoryIds: ReadonlySet<string>,
): AbwabVisibleTreeNode[] {
  const childrenByParent = new Map<string | null, CategorySnapshotDto[]>();
  for (const category of categories) {
    const key = category.parentCategoryId;
    const siblings = childrenByParent.get(key) ?? [];
    siblings.push(category);
    childrenByParent.set(key, siblings);
  }
  for (const siblings of childrenByParent.values()) {
    siblings.sort((a, b) => orderKey(a) - orderKey(b));
  }

  const nodes: AbwabVisibleTreeNode[] = [];
  const visit = (parentId: string | null, depth: number): void => {
    for (const category of childrenByParent.get(parentId) ?? []) {
      const hasChildren = (childrenByParent.get(category.categoryId) ?? []).length > 0;
      const isExpanded = expandedCategoryIds.has(category.categoryId);
      nodes.push({ category, depth, hasChildren, isExpanded });
      if (hasChildren && isExpanded) {
        visit(category.categoryId, depth + 1);
      }
    }
  };
  visit(null, 0);
  return nodes;
}
