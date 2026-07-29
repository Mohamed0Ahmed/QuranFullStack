import { describe, expect, it } from 'vitest';

import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { buildAbwabTreeSnapshot } from '../../state/abwab-tree.builder';
import { flattenVisibleAbwabRows, resolveAbwabTreeKeyboardIntent } from './abwab-tree-keyboard.controller';

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

const SAMPLE_TREE = tree([
  door({ id: 1, name: 'جذر-1', orderValue: 1 }),
  door({ id: 2, name: 'ابن-1-1', parentId: 1, orderValue: 1 }),
  door({ id: 3, name: 'حفيد', parentId: 2, orderValue: 1 }),
  door({ id: 4, name: 'جذر-2', orderValue: 2 }),
]);

describe('flattenVisibleAbwabRows — M8 (collapsed subtrees are skipped)', () => {
  it('includes only the roots when nothing is expanded', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
    const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set());

    expect(rows.map((r) => r.id)).toEqual([1, 4]);
    expect(rows[0]).toEqual({ id: 1, parentId: null, hasChildren: true, isExpanded: false });
  });

  it('reveals children only for expanded branches, and grandchildren only when the child is also expanded', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);

    const oneLevel = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1]));
    expect(oneLevel.map((r) => r.id)).toEqual([1, 2, 4]);

    const twoLevels = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1, 2]));
    expect(twoLevels.map((r) => r.id)).toEqual([1, 2, 3, 4]);
  });
});

describe('resolveAbwabTreeKeyboardIntent — M8 (ArrowUp/Down/Home/End walk visible rows only)', () => {
  const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
  const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1])); // [1, 2, 4] — 3 (grandchild) hidden

  it('ArrowDown moves to the next visible row, skipping the collapsed grandchild entirely', () => {
    const intent = resolveAbwabTreeKeyboardIntent({
      key: 'ArrowDown',
      visibleRows: rows,
      focusedId: 2,
      direction: 'rtl',
      bulkMode: false,
      shiftKey: false,
    });
    expect(intent).toEqual({ type: 'focus', id: 4 });
  });

  it('ArrowUp moves to the previous visible row', () => {
    const intent = resolveAbwabTreeKeyboardIntent({
      key: 'ArrowUp',
      visibleRows: rows,
      focusedId: 4,
      direction: 'rtl',
      bulkMode: false,
      shiftKey: false,
    });
    expect(intent).toEqual({ type: 'focus', id: 2 });
  });

  it('ArrowDown/ArrowUp at the ends of the list is a no-op', () => {
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'ArrowDown', visibleRows: rows, focusedId: 4, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'none' });
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'ArrowUp', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'none' });
  });

  it('Home focuses the first row and End focuses the last, regardless of current focus', () => {
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'Home', visibleRows: rows, focusedId: 4, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'focus', id: 1 });
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'End', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'focus', id: 4 });
  });
});

describe('resolveAbwabTreeKeyboardIntent — M7 (RTL: ArrowLeft expands/enters, ArrowRight collapses/exits)', () => {
  it('RTL ArrowLeft on a collapsed branch expands it', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
    const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set());

    const intent = resolveAbwabTreeKeyboardIntent({
      key: 'ArrowLeft',
      visibleRows: rows,
      focusedId: 1,
      direction: 'rtl',
      bulkMode: false,
      shiftKey: false,
    });
    expect(intent).toEqual({ type: 'expand', id: 1 });
  });

  it('RTL ArrowLeft on an already-expanded branch moves focus into its first child', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
    const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1]));

    const intent = resolveAbwabTreeKeyboardIntent({
      key: 'ArrowLeft',
      visibleRows: rows,
      focusedId: 1,
      direction: 'rtl',
      bulkMode: false,
      shiftKey: false,
    });
    expect(intent).toEqual({ type: 'focus', id: 2 });
  });

  it('RTL ArrowRight on an expanded branch collapses it', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
    const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1]));

    const intent = resolveAbwabTreeKeyboardIntent({
      key: 'ArrowRight',
      visibleRows: rows,
      focusedId: 1,
      direction: 'rtl',
      bulkMode: false,
      shiftKey: false,
    });
    expect(intent).toEqual({ type: 'collapse', id: 1 });
  });

  it('RTL ArrowRight on a leaf/collapsed row moves focus to its parent', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
    const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1, 2]));

    const intent = resolveAbwabTreeKeyboardIntent({
      key: 'ArrowRight',
      visibleRows: rows,
      focusedId: 3, // the leaf grandchild
      direction: 'rtl',
      bulkMode: false,
      shiftKey: false,
    });
    expect(intent).toEqual({ type: 'focus', id: 2 });
  });

  it('mirrors under LTR: ArrowRight expands/enters, ArrowLeft collapses/exits', () => {
    const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
    const collapsed = flattenVisibleAbwabRows(snapshot.liveRoots, new Set());

    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'ArrowRight', visibleRows: collapsed, focusedId: 1, direction: 'ltr', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'expand', id: 1 });

    const expanded = flattenVisibleAbwabRows(snapshot.liveRoots, new Set([1]));
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'ArrowLeft', visibleRows: expanded, focusedId: 1, direction: 'ltr', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'collapse', id: 1 });
  });
});

describe('resolveAbwabTreeKeyboardIntent — selection, bulk, and menu keys', () => {
  const snapshot = buildAbwabTreeSnapshot(SAMPLE_TREE);
  const rows = flattenVisibleAbwabRows(snapshot.liveRoots, new Set());

  it('Enter selects the focused row', () => {
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'Enter', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'select', id: 1 });
  });

  it('Space toggles the bulk checkbox only in bulk mode', () => {
    expect(
      resolveAbwabTreeKeyboardIntent({ key: ' ', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: true, shiftKey: false }),
    ).toEqual({ type: 'toggleBulk', id: 1 });
    expect(
      resolveAbwabTreeKeyboardIntent({ key: ' ', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'none' });
  });

  it('ContextMenu opens the row menu; F10 only does so with Shift', () => {
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'ContextMenu', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'openMenu', id: 1 });
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'F10', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: true }),
    ).toEqual({ type: 'openMenu', id: 1 });
    expect(
      resolveAbwabTreeKeyboardIntent({ key: 'F10', visibleRows: rows, focusedId: 1, direction: 'rtl', bulkMode: false, shiftKey: false }),
    ).toEqual({ type: 'none' });
  });
});
