import { describe, expect, it } from 'vitest';

import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import {
  buildAbwabTreeSnapshot,
  filterAbwabRootsBySection,
  pruneAbwabNodesToVisible,
  searchAbwabNodes,
} from './abwab-tree.builder';

function door(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    aliases: [],
    description: null,
    directChildCount: 0,
    isArchived: false,
    orderValue: overrides.id,
    parentId: null,
    representativeAyahText: null,
    sectionId: null,
    version: 1,
    ...overrides,
  };
}

function tree(doors: AbwabTreeDoorDto[]): AbwabTreeDto {
  return { doors, sections: [], version: 'v1' };
}

describe('buildAbwabTreeSnapshot', () => {
  it('M1 — orders siblings by orderValue and tolerates gaps', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'A', orderValue: 10 }),
        door({ id: 2, name: 'B', orderValue: 1 }),
        door({ id: 3, name: 'C', orderValue: 5 }),
      ]),
    );

    expect(snapshot.liveRoots.map((n) => n.name)).toEqual(['B', 'C', 'A']);
  });

  it('M2 — partitions archived doors out of the live tree into the archive tree', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'root-live', orderValue: 1 }),
        door({ id: 2, name: 'child-archived', parentId: 1, isArchived: true, orderValue: 1 }),
        door({ id: 3, name: 'archived-root', isArchived: true, orderValue: 2 }),
        door({ id: 4, name: 'archived-grandchild', parentId: 3, isArchived: true, orderValue: 1 }),
      ]),
    );

    expect(snapshot.liveRoots).toHaveLength(1);
    expect(snapshot.liveRoots[0].name).toBe('root-live');
    expect(snapshot.liveRoots[0].children).toHaveLength(0);

    expect(snapshot.archivedRoots.map((n) => n.name)).toEqual(['child-archived', 'archived-root']);
    const archivedRootNode = snapshot.archivedRoots.find((n) => n.name === 'archived-root');
    expect(archivedRootNode?.children.map((c) => c.name)).toEqual(['archived-grandchild']);

    expect(snapshot.byId.size).toBe(4);
  });

  it('computes liveChildCount from live children only, ignoring an archived sibling', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'parent', orderValue: 1 }),
        door({ id: 2, name: 'live-child', parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'archived-child', parentId: 1, isArchived: true, orderValue: 2 }),
      ]),
    );

    expect(snapshot.liveRoots[0].liveChildCount).toBe(1);
  });

  it('assigns depth 0 to every root, live or archived, and increments per level', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'root', orderValue: 1 }),
        door({ id: 2, name: 'child', parentId: 1, orderValue: 1 }),
      ]),
    );

    expect(snapshot.liveRoots[0].depth).toBe(0);
    expect(snapshot.liveRoots[0].children[0].depth).toBe(1);
  });
});

describe('filterAbwabRootsBySection — M3', () => {
  it('keeps section-less doors in «كل الأبواب» (null) and excludes them from a specific section tab', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'sectionless', sectionId: null, orderValue: 1 }),
        door({ id: 2, name: 'sectioned', sectionId: 5, orderValue: 2 }),
      ]),
    );

    const allDoors = filterAbwabRootsBySection(snapshot.liveRoots, null);
    expect(allDoors.map((n) => n.name)).toEqual(['sectionless', 'sectioned']);

    const sectionFive = filterAbwabRootsBySection(snapshot.liveRoots, 5);
    expect(sectionFive.map((n) => n.name)).toEqual(['sectioned']);
  });
});

describe('searchAbwabNodes — M4', () => {
  it('matches over name and alias and marks strict ancestors for auto-expand, leaving unrelated branches out', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'العلم بالله', orderValue: 1 }),
        door({ id: 2, name: 'الألوهية', parentId: 1, aliases: ['التوحيد'], orderValue: 1 }),
        door({ id: 3, name: 'أسماء الله الحسنى', parentId: 2, orderValue: 1 }),
        door({ id: 4, name: 'الرسول', orderValue: 2 }),
      ]),
    );

    const byAlias = searchAbwabNodes(snapshot.liveRoots, 'التوحيد');
    expect(byAlias.matchedIds).toEqual(new Set([2]));
    expect(byAlias.visibleIds).toEqual(new Set([1, 2]));
    expect(byAlias.autoExpandedIds).toEqual(new Set([1]));

    const byDeepName = searchAbwabNodes(snapshot.liveRoots, 'حسنى');
    expect(byDeepName.matchedIds).toEqual(new Set([3]));
    expect(byDeepName.visibleIds).toEqual(new Set([1, 2, 3]));
    expect(byDeepName.autoExpandedIds).toEqual(new Set([1, 2]));
  });

  it('reports isFiltering=false and empty sets for a blank query', () => {
    const snapshot = buildAbwabTreeSnapshot(tree([door({ id: 1, name: 'أي شيء', orderValue: 1 })]));
    const result = searchAbwabNodes(snapshot.liveRoots, '   ');

    expect(result.isFiltering).toBe(false);
    expect(result.matchedIds.size).toBe(0);
  });
});

describe('pruneAbwabNodesToVisible — T507 (search-filtered rendering, M4/M31)', () => {
  it('drops a root and its whole subtree when absent from visibleIds, keeping a matching branch intact', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'مطابق', orderValue: 1 }),
        door({ id: 2, name: 'ابن المطابق', parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'غير ذي صلة', orderValue: 2 }),
      ]),
    );

    const pruned = pruneAbwabNodesToVisible(snapshot.liveRoots, new Set([1, 2]));

    expect(pruned.map((n) => n.name)).toEqual(['مطابق']);
    expect(pruned[0].children.map((n) => n.name)).toEqual(['ابن المطابق']);
  });

  it('prunes a non-matching sibling out of an otherwise-visible parent’s children', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'أب', orderValue: 1 }),
        door({ id: 2, name: 'ابن مطابق', parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'ابن غير مطابق', parentId: 1, orderValue: 2 }),
      ]),
    );

    const pruned = pruneAbwabNodesToVisible(snapshot.liveRoots, new Set([1, 2]));

    expect(pruned[0].children.map((n) => n.name)).toEqual(['ابن مطابق']);
  });
});
