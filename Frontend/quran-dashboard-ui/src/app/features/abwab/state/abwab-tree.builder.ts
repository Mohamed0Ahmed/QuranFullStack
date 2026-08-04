import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabNode, AbwabTreeSnapshotVm } from '../models/abwab.models';

function byOrderThenId(a: AbwabTreeDoorDto, b: AbwabTreeDoorDto): number {
  return a.orderValue - b.orderValue || a.id - b.id;
}

function byGlobalOrderThenId(a: AbwabTreeDoorDto, b: AbwabTreeDoorDto): number {
  return (a.globalOrderValue ?? 0) - (b.globalOrderValue ?? 0) || a.id - b.id;
}

function byNodeOrderThenId(a: AbwabNode, b: AbwabNode): number {
  return a.orderValue - b.orderValue || a.id - b.id;
}

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

  function build(doorDto: AbwabTreeDoorDto, depth: number, includeArchivedChildren: boolean): AbwabNode {
    const childDtos = (childrenByParentId.get(doorDto.id) ?? []).filter(
      (child) => includeArchivedChildren || !child.isArchived,
    );
    const children = childDtos.map((child) => build(child, depth + 1, includeArchivedChildren));
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

  const liveRoots = dto.doors
    .filter((d) => !d.isArchived && d.parentId == null)
    .sort(byGlobalOrderThenId)
    .map((d) => build(d, 0, false));

  const archivedRoots = dto.doors
    .filter((d) => d.isArchived && (d.parentId == null || !doorById.get(d.parentId)?.isArchived))
    .sort(byOrderThenId)
    .map((d) => build(d, 0, true));

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

export function countLiveAbwabDoors(byId: ReadonlyMap<number, AbwabNode>): number {
  let count = 0;
  for (const node of byId.values()) {
    if (!node.isArchived) {
      count++;
    }
  }
  return count;
}

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
