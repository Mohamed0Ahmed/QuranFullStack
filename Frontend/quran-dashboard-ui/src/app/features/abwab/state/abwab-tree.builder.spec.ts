import { describe, expect, it } from 'vitest';

import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import {
  buildAbwabTreeSnapshot,
  countAbwabDoorsInOpenScope,
  countLiveAbwabDoors,
  filterAbwabRootsBySection,
  pruneAbwabNodesToVisible,
  searchAbwabNodes,
} from './abwab-tree.builder';

function door(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    aliases: [],
    description: null,
    directChildCount: 0,
    relationCount: 0,
    globalOrderValue: null,
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
  it('M1 — orders live roots by globalOrderValue, independent of orderValue, and tolerates gaps', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'A', orderValue: 1, globalOrderValue: 10 }),
        door({ id: 2, name: 'B', orderValue: 2, globalOrderValue: 1 }),
        door({ id: 3, name: 'C', orderValue: 3, globalOrderValue: 5 }),
      ]),
    );

    // orderValue alone would give [A, B, C]; globalOrderValue gives [B, C, A] — proves the
    // superset's own order drives root placement, not the per-scope order (plan.md §3).
    expect(snapshot.liveRoots.map((n) => n.name)).toEqual(['B', 'C', 'A']);
  });

  it('breaks a globalOrderValue tie by id (hardening — the column has no unique index, plan.md §6)', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 2, name: 'higher-id', orderValue: 1, globalOrderValue: 1 }),
        door({ id: 1, name: 'lower-id', orderValue: 2, globalOrderValue: 1 }),
      ]),
    );

    expect(snapshot.liveRoots.map((n) => n.name)).toEqual(['lower-id', 'higher-id']);
  });

  it('nested children still order by orderValue — globalOrderValue is NULL past the root and never consulted', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'root', orderValue: 1, globalOrderValue: 1 }),
        door({ id: 2, name: 'child-b', parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'child-a', parentId: 1, orderValue: 2 }),
      ]),
    );

    expect(snapshot.liveRoots[0].children.map((n) => n.name)).toEqual(['child-b', 'child-a']);
  });

  it('archived roots keep ordering by orderValue — they carry no meaningful globalOrderValue (§5 invariant)', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'archived-late', isArchived: true, orderValue: 2, globalOrderValue: null }),
        door({ id: 2, name: 'archived-early', isArchived: true, orderValue: 1, globalOrderValue: null }),
      ]),
    );

    expect(snapshot.archivedRoots.map((n) => n.name)).toEqual(['archived-early', 'archived-late']);
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
        door({ id: 1, name: 'sectionless', sectionId: null, orderValue: 1, globalOrderValue: 1 }),
        door({ id: 2, name: 'sectioned', sectionId: 5, orderValue: 2, globalOrderValue: 2 }),
      ]),
    );

    const allDoors = filterAbwabRootsBySection(snapshot.liveRoots, null);
    expect(allDoors.map((n) => n.name)).toEqual(['sectionless', 'sectioned']);

    const sectionFive = filterAbwabRootsBySection(snapshot.liveRoots, 5);
    expect(sectionFive.map((n) => n.name)).toEqual(['sectioned']);
  });

  it('T402 — re-sorts a specific section’s roots by their own orderValue, undoing the superset’s global order', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'first-in-section', sectionId: 9, orderValue: 1, globalOrderValue: 20 }),
        door({ id: 2, name: 'second-in-section', sectionId: 9, orderValue: 2, globalOrderValue: 5 }),
      ]),
    );

    // «كل الأبواب» order (by globalOrderValue): second-in-section (5) before first-in-section (20).
    expect(snapshot.liveRoots.map((n) => n.name)).toEqual(['second-in-section', 'first-in-section']);

    // Section 9's own tab re-sorts back to orderValue: first-in-section (1) before second-in-section (2).
    const sectionNine = filterAbwabRootsBySection(snapshot.liveRoots, 9);
    expect(sectionNine.map((n) => n.name)).toEqual(['first-in-section', 'second-in-section']);

    // The re-sort operates on a copy — the shared superset order is untouched.
    expect(snapshot.liveRoots.map((n) => n.name)).toEqual(['second-in-section', 'first-in-section']);
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

describe('countLiveAbwabDoors / countAbwabDoorsInOpenScope — item 17 stats bar (Slice B2, T1002)', () => {
  it('counts live doors only (excluding section-less doors are still counted, archived are not), and reads the open scope from doorsInScopeCount without asserting the two sum', () => {
    const snapshot = buildAbwabTreeSnapshot({
      doors: [
        door({ id: 1, name: 'in-section', sectionId: 9, orderValue: 1 }),
        door({ id: 2, name: 'section-less', sectionId: null, orderValue: 2 }),
        door({ id: 3, name: 'archived-in-section', sectionId: 9, isArchived: true, orderValue: 3 }),
      ],
      sections: [{ id: 9, name: 'قسم', orderValue: 1, version: 1, doorsInScopeCount: 1 }],
      version: 'v1',
    });

    const total = countLiveAbwabDoors(snapshot.byId);
    expect(total).toBe(2);

    // The section's own doorsInScopeCount (1) is intentionally LESS than the total (2) —
    // sectionId is nullable, so a live door (id 2) sits outside every section's count. The two
    // numbers deliberately do not sum to anything meaningful; this only checks each in isolation.
    expect(countAbwabDoorsInOpenScope(snapshot.sections, 9, total)).toBe(1);
    expect(countAbwabDoorsInOpenScope(snapshot.sections, 42, total)).toBe(0);

    // «كل الأبواب» (no active section) falls back to the same live-only total.
    expect(countAbwabDoorsInOpenScope(snapshot.sections, null, total)).toBe(total);
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
