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
    sectionId: 1,
    sectionRetired: false,
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

  describe('audit item 14 — liveDescendantCount and maxRelativeDepth', () => {
    it('leaves both at 0 for a leaf', () => {
      const snapshot = buildAbwabTreeSnapshot(tree([door({ id: 1, name: 'ورقة' })]));

      expect(snapshot.liveRoots[0].liveDescendantCount).toBe(0);
      expect(snapshot.liveRoots[0].maxRelativeDepth).toBe(0);
    });

    // The worked example the audit pins: child + grandchild + great-grandchild ⇒ depth 3,
    // measured from the door, never from `node.depth`.
    it('measures depth relative to the door, so a three-link chain is 3 at the root and 1 at the grandchild', () => {
      const snapshot = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'جذر' }),
          door({ id: 2, name: 'ابن', parentId: 1 }),
          door({ id: 3, name: 'حفيد', parentId: 2 }),
          door({ id: 4, name: 'ابن الحفيد', parentId: 3 }),
        ]),
      );

      const root = snapshot.liveRoots[0];
      expect(root.maxRelativeDepth).toBe(3);
      expect(root.liveDescendantCount).toBe(3);

      const grandchild = snapshot.byId.get(3)!;
      expect(grandchild.depth).toBe(2); // absolute position in the tree…
      expect(grandchild.maxRelativeDepth).toBe(1); // …and one level of its own below it
      expect(grandchild.liveDescendantCount).toBe(1);
    });

    it('separates a deep chain from a wide fan — same descendant count, different depth', () => {
      const chain = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'سلسلة' }),
          door({ id: 2, name: 'س-1', parentId: 1 }),
          door({ id: 3, name: 'س-2', parentId: 2 }),
          door({ id: 4, name: 'س-3', parentId: 3 }),
        ]),
      ).liveRoots[0];
      const fan = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'مروحة' }),
          door({ id: 2, name: 'م-1', parentId: 1 }),
          door({ id: 3, name: 'م-2', parentId: 1 }),
          door({ id: 4, name: 'م-3', parentId: 1 }),
        ]),
      ).liveRoots[0];

      expect(chain.liveDescendantCount).toBe(fan.liveDescendantCount);
      expect(chain.maxRelativeDepth).toBe(3);
      expect(fan.maxRelativeDepth).toBe(1);
    });

    it('excludes an archived subtree from both counts', () => {
      const snapshot = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'جذر' }),
          door({ id: 2, name: 'ابن حي', parentId: 1 }),
          door({ id: 3, name: 'فرع مؤرشف', parentId: 1, isArchived: true }),
          door({ id: 4, name: 'تحت المؤرشف', parentId: 3, isArchived: true }),
          door({ id: 5, name: 'أعمق تحت المؤرشف', parentId: 4, isArchived: true }),
        ]),
      );

      // The archived branch is two levels deeper than the live one, so a build that counted it
      // would report 4 descendants and depth 3 instead of 1 and 1.
      const root = snapshot.liveRoots[0];
      expect(root.liveDescendantCount).toBe(1);
      expect(root.maxRelativeDepth).toBe(1);
    });
  });

  describe('item 19 — rootCountBySectionId', () => {
    it('counts roots only — a nested door in the section does not count', () => {
      const snapshot = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'root', sectionId: 5 }),
          door({ id: 2, name: 'child', parentId: 1, sectionId: 5 }),
        ]),
      );

      expect(snapshot.rootCountBySectionId.get(5)).toBe(1);
    });

    it('excludes archived roots', () => {
      const snapshot = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'live-root', sectionId: 5 }),
          door({ id: 2, name: 'archived-root', sectionId: 5, isArchived: true }),
        ]),
      );

      expect(snapshot.rootCountBySectionId.get(5)).toBe(1);
    });

    it('omits a section with no roots rather than zero-defaulting an entry', () => {
      const snapshot = buildAbwabTreeSnapshot(tree([door({ id: 1, name: 'root', sectionId: 5 })]));

      expect(snapshot.rootCountBySectionId.has(9)).toBe(false);
    });

    // Every root belongs to a section now, so Σ over the map DOES equal liveRoots.length — the
    // non-identity this used to assert is gone with the section-less state. «كل الأبواب» still
    // reads liveRoots.length: one number beats a sum reassembled here, and the two agreeing is a
    // fact worth pinning rather than a reason to derive one from the other.
    it('sums to liveRoots.length now that every root belongs to a section', () => {
      const snapshot = buildAbwabTreeSnapshot(
        tree([
          door({ id: 1, name: 'in-five', sectionId: 5 }),
          door({ id: 2, name: 'in-nine', sectionId: 9 }),
          door({ id: 3, name: 'also-in-five', sectionId: 5 }),
        ]),
      );

      expect(snapshot.liveRoots).toHaveLength(3);
      const total = Array.from(snapshot.rootCountBySectionId.values()).reduce((sum, n) => sum + n, 0);
      expect(total).toBe(snapshot.liveRoots.length);
      expect(snapshot.rootCountBySectionId.get(5)).toBe(2);
    });
  });
});

describe('filterAbwabRootsBySection — M3', () => {
  // The `null` argument is the ACTIVE TAB meaning «كل الأبواب», not a door's section — doors always
  // have one. It is every door across every section, which is what makes the tab a real superset.
  it('returns every root for «كل الأبواب» (null) and only its own for a specific section tab', () => {
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'in-one', sectionId: 1, orderValue: 1, globalOrderValue: 1 }),
        door({ id: 2, name: 'in-five', sectionId: 5, orderValue: 2, globalOrderValue: 2 }),
      ]),
    );

    const allDoors = filterAbwabRootsBySection(snapshot.liveRoots, null);
    expect(allDoors.map((n) => n.name)).toEqual(['in-one', 'in-five']);

    const sectionFive = filterAbwabRootsBySection(snapshot.liveRoots, 5);
    expect(sectionFive.map((n) => n.name)).toEqual(['in-five']);
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

// The walk has two consumers with different appetites (ux-slice-l): the tree reads `matchedIds`
// to mark rows and `autoExpandedIds` to seed branches open while keeping every row on screen;
// the cards and archive views read `visibleIds` and still prune to it. The exact set contents
// pinned below are therefore also the byte-identical fence for the walk's push/pop rewrite.
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

  it('walks a deep chain with siblings without leaking ancestors between branches', () => {
    // The stack case: a mis-popped ancestor stack shows up as ids from a sibling branch bleeding
    // into another branch's ancestor set, which only a chain deeper than three levels — with a
    // sibling subtree walked before the match — can expose.
    const snapshot = buildAbwabTreeSnapshot(
      tree([
        door({ id: 1, name: 'الجذر', orderValue: 1 }),
        door({ id: 2, name: 'فرع أول', parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'حفيد أول', parentId: 2, orderValue: 1 }),
        door({ id: 4, name: 'فرع ثان', parentId: 1, orderValue: 2 }),
        door({ id: 5, name: 'حفيد ثان', parentId: 4, orderValue: 1 }),
        door({ id: 6, name: 'الهدف العميق', parentId: 5, orderValue: 1 }),
      ]),
    );

    const result = searchAbwabNodes(snapshot.liveRoots, 'الهدف');

    expect(result.matchedIds).toEqual(new Set([6]));
    // Strictly the chain 1 → 4 → 5; the first branch (2, 3) contributed nothing.
    expect(result.visibleIds).toEqual(new Set([1, 4, 5, 6]));
    expect(result.autoExpandedIds).toEqual(new Set([1, 4, 5]));
  });

  it('reports isFiltering=false and empty sets for a blank query', () => {
    const snapshot = buildAbwabTreeSnapshot(tree([door({ id: 1, name: 'أي شيء', orderValue: 1 })]));
    const result = searchAbwabNodes(snapshot.liveRoots, '   ');

    expect(result.isFiltering).toBe(false);
    expect(result.matchedIds.size).toBe(0);
  });
});

describe('countLiveAbwabDoors / countAbwabDoorsInOpenScope — item 17 stats bar (Slice B2, T1002)', () => {
  it('counts live doors only (archived are not), and reads the open scope from doorsInScopeCount rather than recomputing it', () => {
    const snapshot = buildAbwabTreeSnapshot({
      doors: [
        door({ id: 1, name: 'in-nine', sectionId: 9, orderValue: 1 }),
        door({ id: 2, name: 'in-four', sectionId: 4, orderValue: 2 }),
        door({ id: 3, name: 'archived-in-nine', sectionId: 9, isArchived: true, orderValue: 3 }),
      ],
      sections: [
        { id: 9, name: 'قسم', orderValue: 1, version: 1, doorsInScopeCount: 1 },
        { id: 4, name: 'قسم آخر', orderValue: 2, version: 1, doorsInScopeCount: 1 },
      ],
      version: 'v1',
    });

    const total = countLiveAbwabDoors(snapshot.byId);
    expect(total).toBe(2);

    // Each section's own doorsInScopeCount is read as given, never derived here: it is the
    // backend's live-only count at any depth, and recomputing it client-side would fork from that
    // definition the moment the two drift. That the per-section counts now happen to reconcile
    // with the total is a consequence of every door having a section, not a reason to sum them.
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
