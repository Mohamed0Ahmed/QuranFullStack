import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabNode, AbwabTreeSnapshotVm } from '../models/abwab.models';

function byOrderThenId(a: AbwabTreeDoorDto, b: AbwabTreeDoorDto): number {
  return a.orderValue - b.orderValue || a.id - b.id;
}

/** Live roots only (§5's invariant) — the superset's own order, independent of `orderValue`. */
function byGlobalOrderThenId(a: AbwabTreeDoorDto, b: AbwabTreeDoorDto): number {
  return (a.globalOrderValue ?? 0) - (b.globalOrderValue ?? 0) || a.id - b.id;
}

function byNodeOrderThenId(a: AbwabNode, b: AbwabNode): number {
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
    // Both derivations are memoized onto the node here rather than computed in the tree
    // component: they are pure functions of the built subtree, and the children are already
    // built, so each is one level of arithmetic over values the recursion just produced.
    const liveChildren = children.filter((child) => !child.isArchived);
    const node: AbwabNode = {
      id: doorDto.id,
      name: doorDto.name,
      description: doorDto.description,
      representativeAyahText: doorDto.representativeAyahText,
      aliases: doorDto.aliases,
      sectionId: doorDto.sectionId,
      sectionRetired: doorDto.sectionRetired,
      parentId: doorDto.parentId,
      orderValue: doorDto.orderValue,
      globalOrderValue: doorDto.globalOrderValue,
      version: doorDto.version,
      isArchived: doorDto.isArchived,
      depth,
      liveChildCount: liveChildren.length,
      liveDescendantCount: liveChildren.reduce(
        (sum, child) => sum + 1 + child.liveDescendantCount,
        0,
      ),
      maxRelativeDepth: liveChildren.reduce(
        (deepest, child) => Math.max(deepest, 1 + child.maxRelativeDepth),
        0,
      ),
      relationCount: doorDto.relationCount,
      children,
    };
    byId.set(node.id, node);
    return node;
  }

  // The superset's own order (T402) — independent of any section's orderValue.
  const liveRoots = dto.doors
    .filter((d) => !d.isArchived && d.parentId == null)
    .sort(byGlobalOrderThenId)
    .map((d) => build(d, 0, false));

  const archivedRoots = dto.doors
    .filter((d) => d.isArchived && (d.parentId == null || !doorById.get(d.parentId)?.isArchived))
    .sort(byOrderThenId)
    .map((d) => build(d, 0, true));

  // Item 19: root doors per section, built alongside liveRoots in the same pass. Every root belongs
  // to a section now, so Σ over this map does equal liveRoots.length — «كل الأبواب» still reads the
  // latter, because one number the backend already computed beats a sum assembled here.
  const rootCountBySectionId = new Map<number, number>();
  for (const root of liveRoots) {
    rootCountBySectionId.set(root.sectionId, (rootCountBySectionId.get(root.sectionId) ?? 0) + 1);
  }

  return {
    sections: dto.sections,
    liveRoots,
    archivedRoots,
    rootCountBySectionId,
    byId,
    version: dto.version,
  };
}

/** «كل الأبواب» is every root, already in `liveRoots`' own global order (T402) — left as-is. The
 * `null` here is the ACTIVE TAB meaning "no section selected", not a door's section: a door always
 * has one. A specific section re-sorts by its own `orderValue` (§5's other order space):
 * `liveRoots` is globally ordered now, and that order has nothing to do with any one section's
 * `1..N`, so keeping it would show a section's roots out of their own sequence. A nested door's
 * section always matches its parent's (plan.md §13.5), so filtering at the root is enough. `.filter()` on a `readonly AbwabNode[]` returns a fresh mutable array — sorting
 * that copy, not the shared snapshot array, is what makes `.sort()` legal without widening the
 * return type (which would drop the guard against an in-place sort of `liveRoots` itself). */
export function filterAbwabRootsBySection(
  roots: readonly AbwabNode[],
  sectionId: number | null,
): readonly AbwabNode[] {
  if (sectionId === null) {
    return roots;
  }
  return roots.filter((root) => root.sectionId === sectionId).sort(byNodeOrderThenId);
}

export interface AbwabSearchResult {
  readonly isFiltering: boolean;
  readonly matchedIds: ReadonlySet<number>;
  readonly visibleIds: ReadonlySet<number>;
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

/**
 * Searches names **and** aliases (plan-slice-b.md §6.2); ancestors of a match stay visible and
 * are marked to auto-expand, matching the mock's "open the path to a match" behavior.
 *
 * Two consumers now read different halves of one walk (ux-slice-l): the tree highlights
 * `matchedIds` and seeds `autoExpandedIds` open while keeping every row on screen, and the
 * cards/archive views still prune to `visibleIds`. One walk, one query, two presentations.
 *
 * The recursion carries ONE live ancestor stack — pushed before the children loop, popped after —
 * rather than allocating `[...ancestors, node]` per edge. Output is byte-identical; the M4/prune
 * cases pin exact set contents and pass unmodified, which is what makes that claim checkable.
 */
export function searchAbwabNodes(roots: readonly AbwabNode[], query: string): AbwabSearchResult {
  const trimmed = query.trim();
  if (trimmed === '') {
    return EMPTY_SEARCH_RESULT;
  }

  const matchedIds = new Set<number>();
  const visibleIds = new Set<number>();
  const autoExpandedIds = new Set<number>();
  const ancestors: AbwabNode[] = [];

  function walk(node: AbwabNode): boolean {
    const isMatch = nodeMatchesQuery(node, trimmed);
    let anyDescendantMatch = false;
    ancestors.push(node);
    for (const child of node.children) {
      anyDescendantMatch = walk(child) || anyDescendantMatch;
    }
    ancestors.pop();

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
    walk(root);
  }

  return { isFiltering: true, matchedIds, visibleIds, autoExpandedIds };
}

/**
 * Total live (non-archived) doors — item 17's «كل الأبواب» stat. **Live-only, deliberately**:
 * matches every other count in this feature. It is still NOT Σ `AbwabTreeSectionDto.doorsInScopeCount`
 * over `sections`, though the reason changed: every door belongs to a section now, so the two DO
 * reconcile — summing sections would simply recompute client-side what the backend already answered,
 * and fork from its definition of "in scope at any depth" the moment the two drift. Do not assert the
 * two sum: redundant, not impossible (feature README, stats-bar).
 */
export function countLiveAbwabDoors(byId: ReadonlyMap<number, AbwabNode>): number {
  let count = 0;
  for (const node of byId.values()) {
    if (!node.isArchived) {
      count++;
    }
  }
  return count;
}

/**
 * Doors in the currently open toolbar tab, item 17's second stat. «كل الأبواب»
 * (`activeSectionId === null`) has no per-section count on the wire, so it falls back to the
 * same live-only total as the first stat — the two numbers legitimately agree there, because
 * "everything" and "the open scope" are the same set on that tab. A specific section reads the
 * backend-computed `doorsInScopeCount` (every live door with that section at any depth, already
 * on the wire) rather than recomputing it — recomputing it here would silently diverge from the
 * backend's own definition the moment the two drift.
 */
export function countAbwabDoorsInOpenScope(
  sections: readonly AbwabTreeSectionDto[],
  activeSectionId: number | null,
  totalLiveDoors: number,
): number {
  if (activeSectionId === null) {
    return totalLiveDoors;
  }
  return sections.find((section) => section.id === activeSectionId)?.doorsInScopeCount ?? 0;
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
