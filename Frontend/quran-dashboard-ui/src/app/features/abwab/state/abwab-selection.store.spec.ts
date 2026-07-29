import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabSelectionStore } from './abwab-selection.store';
import { buildAbwabTreeSnapshot } from './abwab-tree.builder';
import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';

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

function setup(): AbwabSelectionStore {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ providers: [AbwabSelectionStore] });
  return TestBed.inject(AbwabSelectionStore);
}

describe('AbwabSelectionStore', () => {
  it('selects a single door and clears it', () => {
    const store = setup();
    store.select(1, 3);

    expect(store.selectedDoorId()).toBe(1);
    expect(store.selectedVersion()).toBe(3);

    store.clearSelection();
    expect(store.selectedDoorId()).toBeNull();
    expect(store.selectedVersion()).toBeNull();
  });

  it('toggles doors in and out of the bulk set', () => {
    const store = setup();
    store.setBulkMode(true);
    store.toggleBulk(1, 2);
    store.toggleBulk(2, 5);
    expect(store.bulkSet()).toEqual(new Map([[1, 2], [2, 5]]));

    store.toggleBulk(1, 2);
    expect(store.bulkSet()).toEqual(new Map([[2, 5]]));
  });

  it('entering bulk mode clears the single selection; leaving it clears the bulk set', () => {
    const store = setup();
    store.select(1, 2);
    store.setBulkMode(true);
    expect(store.selectedDoorId()).toBeNull();

    store.toggleBulk(3, 1);
    store.setBulkMode(false);
    expect(store.bulkSet().size).toBe(0);
  });

  describe('M23 — bulk mode is unavailable in the archive view', () => {
    it('refuses to enter bulk mode while the archive view is active', () => {
      const store = setup();
      store.setArchiveViewActive(true);
      store.setBulkMode(true);

      expect(store.bulkMode()).toBe(false);
    });

    it('turns bulk mode off when the archive view activates mid-session', () => {
      const store = setup();
      store.setBulkMode(true);
      expect(store.bulkMode()).toBe(true);

      store.setArchiveViewActive(true);
      expect(store.bulkMode()).toBe(false);
    });

    it('allows bulk mode again once the archive view deactivates', () => {
      const store = setup();
      store.setArchiveViewActive(true);
      store.setArchiveViewActive(false);
      store.setBulkMode(true);

      expect(store.bulkMode()).toBe(true);
    });
  });

  describe('M24 — rebindTo(snapshot) rebinds by id and drops vanished ids', () => {
    it('refreshes the single selection version and clears it if the door vanished', () => {
      const store = setup();
      store.select(1, 1);

      const refreshed = buildAbwabTreeSnapshot(tree([door({ id: 1, name: 'باب', version: 7 })]));
      store.rebindTo(refreshed);
      expect(store.selectedDoorId()).toBe(1);
      expect(store.selectedVersion()).toBe(7);

      const withoutDoor1 = buildAbwabTreeSnapshot(tree([door({ id: 2, name: 'آخر', version: 1 })]));
      store.rebindTo(withoutDoor1);
      expect(store.selectedDoorId()).toBeNull();
      expect(store.selectedVersion()).toBeNull();
    });

    it('refreshes every bulk token by id and drops ids that vanished', () => {
      const store = setup();
      store.setBulkMode(true);
      store.toggleBulk(1, 1);
      store.toggleBulk(2, 1);

      const refreshed = buildAbwabTreeSnapshot(
        tree([door({ id: 1, name: 'باب-1', version: 9 })]),
      );
      store.rebindTo(refreshed);

      expect(store.bulkSet()).toEqual(new Map([[1, 9]]));
    });
  });
});
