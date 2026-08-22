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
      linkCount: doorDto.linkCount,
      selectedWordCount: doorDto.selectedWordCount,
      inclusionSourceCount: doorDto.inclusionSourceCount,
      inclusionConsumerCount: doorDto.inclusionConsumerCount,
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
