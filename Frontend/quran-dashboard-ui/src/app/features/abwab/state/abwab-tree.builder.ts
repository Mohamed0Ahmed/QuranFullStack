import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabNode, AbwabTreeSnapshotVm } from '../models/abwab.models';

function byOrderThenId(a: AbwabTreeDoorDto, b: AbwabTreeDoorDto): number {
  return a.orderValue - b.orderValue || a.id - b.id;
}

/**
 * Snapshot DTO → view-model tree. Pure (plan-slice-b.md §7 T406): builds parent/child
 * links, sorts siblings gap-tolerantly, and partitions archived doors into their own
 * tree. An archive "root" is an archived door whose parent is live or absent — its own
 * parent may sit inside the live tree, since archiving a subtree never touches ancestors
 * (plan.md §4). Every descendant of an archived door is archived too (the archive-subtree
 * invariant), so recursing without re-checking `isArchived` past the root is safe.
 */
export function buildAbwabTreeSnapshot(dto: AbwabTreeDto): AbwabTreeSnapshotVm {
  const doorById = new Map<number, AbwabTreeDoorDto>(dto.doors.map((d) => [d.id, d]));
  const childrenByParentId = new Map<number, AbwabTreeDoorDto[]>();
  for (const doorDto of dto.doors) {
    if (doorDto.parentId == null) {
      continue;
    }
    const siblings = childrenByParentId.get(doorDto.parentId) ?? [];
    siblings.push(doorDto);
    childrenByParentId.set(doorDto.parentId, siblings);
  }
  for (const siblings of childrenByParentId.values()) {
    siblings.sort(byOrderThenId);
  }

  const byId = new Map<number, AbwabNode>();

  // The live tree recurses only into live children (an archived child starts its own
  // archive-tree root instead, M2); the archive tree recurses into every child, since
  // everything under an archived door is archived too.
  function build(doorDto: AbwabTreeDoorDto, depth: number, includeArchivedChildren: boolean): AbwabNode {
    const childDtos = (childrenByParentId.get(doorDto.id) ?? []).filter(
      (child) => includeArchivedChildren || !child.isArchived,
    );
    const children = childDtos.map((child) => build(child, depth + 1, includeArchivedChildren));
    const node: AbwabNode = {
      id: doorDto.id,
      name: doorDto.name,
      description: doorDto.description,
      representativeAyahText: doorDto.representativeAyahText,
      aliases: doorDto.aliases,
      sectionId: doorDto.sectionId,
      parentId: doorDto.parentId,
      orderValue: doorDto.orderValue,
      version: doorDto.version,
      isArchived: doorDto.isArchived,
      depth,
      liveChildCount: children.filter((child) => !child.isArchived).length,
      children,
    };
    byId.set(node.id, node);
    return node;
  }

  const liveRoots = dto.doors
    .filter((d) => !d.isArchived && d.parentId == null)
    .sort(byOrderThenId)
    .map((d) => build(d, 0, false));

  const archivedRoots = dto.doors
    .filter((d) => d.isArchived && (d.parentId == null || !doorById.get(d.parentId)?.isArchived))
    .sort(byOrderThenId)
    .map((d) => build(d, 0, true));

  return {
    sections: dto.sections,
    liveRoots,
    archivedRoots,
    byId,
    version: dto.version,
  };
}

/** «كل الأبواب» (`sectionId === null`) is every root; a specific section keeps only its own roots — a
 * nested door's section always matches its parent's (plan.md §13.5), so filtering at the root is enough. */
export function filterAbwabRootsBySection(
  roots: readonly AbwabNode[],
  sectionId: number | null,
): readonly AbwabNode[] {
  if (sectionId === null) {
    return roots;
  }
  return roots.filter((root) => root.sectionId === sectionId);
}

export interface AbwabSearchResult {
  readonly isFiltering: boolean;
  readonly matchedIds: ReadonlySet<number>;
  /** Matched nodes plus every strict ancestor of a match — what stays on screen. */
  readonly visibleIds: ReadonlySet<number>;
  /** Strict ancestors of a match only — these must open even if the user collapsed them. */
  readonly autoExpandedIds: ReadonlySet<number>;
}

const EMPTY_SEARCH_RESULT: AbwabSearchResult = {
  isFiltering: false,
  matchedIds: new Set(),
  visibleIds: new Set(),
  autoExpandedIds: new Set(),
};

function nodeMatchesQuery(node: AbwabNode, query: string): boolean {
  if (node.name.includes(query)) {
    return true;
  }
  return node.aliases.some((alias) => alias.includes(query));
}

/** Searches names **and** aliases (plan-slice-b.md §6.2); ancestors of a match stay visible
 * and are marked to auto-expand, matching the mock's "open the path to a match" behavior. */
export function searchAbwabNodes(roots: readonly AbwabNode[], query: string): AbwabSearchResult {
  const trimmed = query.trim();
  if (trimmed === '') {
    return EMPTY_SEARCH_RESULT;
  }

  const matchedIds = new Set<number>();
  const visibleIds = new Set<number>();
  const autoExpandedIds = new Set<number>();

  function walk(node: AbwabNode, ancestors: readonly AbwabNode[]): boolean {
    const isMatch = nodeMatchesQuery(node, trimmed);
    let anyDescendantMatch = false;
    for (const child of node.children) {
      anyDescendantMatch = walk(child, [...ancestors, node]) || anyDescendantMatch;
    }

    if (isMatch) {
      matchedIds.add(node.id);
    }
    if (isMatch || anyDescendantMatch) {
      visibleIds.add(node.id);
      for (const ancestor of ancestors) {
        visibleIds.add(ancestor.id);
        autoExpandedIds.add(ancestor.id);
      }
    }
    return isMatch || anyDescendantMatch;
  }

  for (const root of roots) {
    walk(root, []);
  }

  return { isFiltering: true, matchedIds, visibleIds, autoExpandedIds };
}

/** Rebuilds a node list keeping only ids in `visibleIds`, recursing into children
 * (T507 — search filtering for the tree/cards/archive views). `visibleIds` already
 * contains every match plus its strict ancestors (searchAbwabNodes' output), so a
 * kept parent's non-matching, non-ancestor children are the only thing this drops. */
export function pruneAbwabNodesToVisible(
  nodes: readonly AbwabNode[],
  visibleIds: ReadonlySet<number>,
): readonly AbwabNode[] {
  const result: AbwabNode[] = [];
  for (const node of nodes) {
    if (!visibleIds.has(node.id)) {
      continue;
    }
    const children = pruneAbwabNodesToVisible(node.children, visibleIds);
    result.push(children === node.children ? node : { ...node, children });
  }
  return result;
}
