import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { AbwabWriteController } from './abwab-write.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabApi } from '../data-access/abwab.api';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { BulkMoveDoorsCommand } from '../../../core/api/generated/models/bulk-move-doors-command';

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

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

const EDITED_DOOR: AbwabDoorDto = {
  id: 1,
  name: 'الألوهية',
  description: null,
  representativeAyahText: null,
  aliases: [],
  parentId: null,
  sectionId: null,
  orderValue: 1,
  version: 2,
};

function httpError(status: number, message: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { isSuccess: false, message, data: null } });
}

interface FakeApi {
  getTree: () => Observable<ApiResponse<AbwabTreeDto>>;
  updateDoor?: () => Observable<ApiResponse<AbwabDoorDto>>;
  createDoor?: () => Observable<ApiResponse<AbwabDoorDto>>;
  archiveDoor?: () => Observable<ApiResponse<unknown>>;
  restoreDoor?: () => Observable<ApiResponse<unknown>>;
  bulkMoveDoors?: (command: BulkMoveDoorsCommand) => Observable<ApiResponse<AbwabDoorDto[]>>;
  bulkArchiveDoors?: () => Observable<ApiResponse<number[]>>;
}

function setup(fakeApi: FakeApi) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabWriteController,
      AbwabSnapshotFacade,
      AbwabSelectionStore,
      { provide: AbwabApi, useValue: fakeApi },
    ],
  });
  return {
    controller: TestBed.inject(AbwabWriteController),
    facade: TestBed.inject(AbwabSnapshotFacade),
    selection: TestBed.inject(AbwabSelectionStore),
  };
}

describe('AbwabWriteController', () => {
  describe('M14 — a 409 keeps input, keeps valid context, clears the invalidated selection, shows the message, never auto-retries', () => {
    it('reports the conflict outcome and clears only the door that was under conflict', () => {
      const { controller, selection } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        updateDoor: () => throwError(() => httpError(409, 'تم تعديل الباب من مستخدم آخر')),
      });
      selection.select(1, 1);

      let callCount = 0;
      controller
        .updateDoor(1, { name: 'x', description: null, representativeAyahText: null, aliases: null, version: 1 })
        .subscribe((outcome) => {
          callCount += 1;
          expect(outcome).toEqual({ kind: 'conflict', message: 'تم تعديل الباب من مستخدم آخر' });
        });

      expect(callCount).toBe(1); // never auto-retried
      expect(selection.selectedDoorId()).toBeNull(); // the conflicted door's selection is invalidated
      expect(controller.announcement()).toBe('تم تعديل الباب من مستخدم آخر');
    });

    it('does not clear selection belonging to an unrelated door', () => {
      const { controller, selection } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        updateDoor: () => throwError(() => httpError(409, 'تعارض')),
      });
      selection.select(99, 5); // a different door than the one being edited

      controller
        .updateDoor(1, { name: 'x', description: null, representativeAyahText: null, aliases: null, version: 1 })
        .subscribe();

      expect(selection.selectedDoorId()).toBe(99);
    });
  });

  describe('M15 — every successful write refetches the snapshot and rebinds cached version tokens', () => {
    it('refreshes the facade and rebinds the selection store after a successful edit', () => {
      const { controller, facade, selection } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [door({ id: 1, name: 'بعد التحديث', version: 9 })], sections: [], version: 'v2' })),
        updateDoor: () => of(ok<AbwabDoorDto>(EDITED_DOOR)),
      });
      selection.select(1, 1); // stale pre-write version

      controller
        .updateDoor(1, { name: 'الألوهية', description: null, representativeAyahText: null, aliases: null, version: 1 })
        .subscribe();

      expect(facade.snapshot()?.liveRoots[0].name).toBe('بعد التحديث');
      expect(selection.selectedVersion()).toBe(9);
    });
  });

  describe('M16 — bulk move after a create in the same scope sends fresh tokens (the resequencing trap)', () => {
    it('reads the bulk selection at call time, after the create refreshed and rebound it', () => {
      let getTreeCalls = 0;
      let capturedCommand: BulkMoveDoorsCommand | null = null;

      const { controller, facade, selection } = setup({
        getTree: () => {
          getTreeCalls += 1;
          const version = getTreeCalls === 1 ? 1 : 99; // resequencing bumped the sibling's xmin
          return of(ok<AbwabTreeDto>({ doors: [door({ id: 2, name: 'شقيق', version })], sections: [], version: 'v' }));
        },
        createDoor: () => of(ok<AbwabDoorDto>(EDITED_DOOR)),
        bulkMoveDoors: (command) => {
          capturedCommand = command;
          return of(ok<AbwabDoorDto[]>([]));
        },
      });

      // Initial page load (call #1, version 1) — then the user bulk-selects that sibling.
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(2, 1);

      controller
        .createDoor({
          name: 'جديد',
          description: null,
          representativeAyahText: null,
          aliases: null,
          parentId: null,
          sectionId: null,
        })
        .subscribe();

      controller.bulkMoveDoors(null, 5).subscribe();

      expect(capturedCommand).toEqual({
        doors: [{ doorId: 2, version: 99 }],
        targetParentId: null,
        targetSectionId: 5,
      });
    });
  });

  describe('M17 — a bulk 409 reports the locked conflict message and preserves still-valid selection', () => {
    it('names the doors from the current bulk selection and leaves the bulk set intact', () => {
      const { controller, facade, selection } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [door({ id: 1, name: 'الألوهية' }), door({ id: 2, name: 'الربوبية' })], sections: [], version: 'v' })),
        bulkMoveDoors: () => throwError(() => httpError(409, 'stale')),
      });
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(1, 1);
      selection.toggleBulk(2, 1);

      let outcome: unknown;
      controller.bulkMoveDoors(null, null).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({
        kind: 'conflict',
        message: ABWAB_LABELS.bulkConflictMessage('الألوهية، الربوبية'),
      });
      expect(selection.bulkSet().size).toBe(2); // preserved, not cleared
    });
  });

  describe('M18 — archive confirms with the live-subtree count derived from the snapshot', () => {
    it('counts the door itself plus every live descendant, two levels deep', () => {
      const { controller, facade } = setup({
        getTree: () =>
          of(
            ok<AbwabTreeDto>({
              doors: [
                door({ id: 1, name: 'جذر' }),
                door({ id: 2, name: 'ابن', parentId: 1 }),
                door({ id: 3, name: 'حفيد', parentId: 2 }),
                door({ id: 4, name: 'ابن مؤرشف', parentId: 1, isArchived: true }),
              ],
              sections: [],
              version: 'v',
            }),
          ),
      });
      facade.load();

      // root(1) + live child(2) + live grandchild(3) = 3; the archived sibling(4) does not count.
      expect(controller.liveSubtreeCountFor(1)).toBe(3);
    });
  });

  describe('T504 — bulk-archive count is a union, not a sum, over an ancestor+descendant selection', () => {
    it('counts each door once even when a selected door is an ancestor of another selected door', () => {
      const { controller, facade } = setup({
        getTree: () =>
          of(
            ok<AbwabTreeDto>({
              doors: [
                door({ id: 1, name: 'جذر' }),
                door({ id: 2, name: 'ابن', parentId: 1 }),
                door({ id: 3, name: 'حفيد', parentId: 2 }),
                door({ id: 4, name: 'شقيق منفصل' }),
              ],
              sections: [],
              version: 'v',
            }),
          ),
      });
      facade.load();

      // Selecting both the root (1) and its own descendant (2): summing
      // liveSubtreeCountFor(1)=3 + liveSubtreeCountFor(2)=2 would wrongly report 5.
      // The correct union is {1,2,3,4} = 4.
      expect(controller.bulkArchiveConfirmMessage([1, 2, 4])).toBe(ABWAB_LABELS.archiveConfirm(4));
    });
  });

  describe('M19 — restore maps detachedFromArchivedSection to its announcement', () => {
    it('announces the detach message when the flag is true', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        restoreDoor: () => of(ok({ door: EDITED_DOOR, detachedFromArchivedSection: true })),
      } as unknown as FakeApi);

      controller.restoreDoor(1, 1).subscribe();
      expect(controller.announcement()).toBe(ABWAB_LABELS.restoreDetachedAnnouncement);
    });

    it('does not announce a detach message when the flag is false', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        restoreDoor: () => of(ok({ door: EDITED_DOOR, detachedFromArchivedSection: false })),
      } as unknown as FakeApi);

      controller.restoreDoor(1, 1).subscribe();
      expect(controller.announcement()).toBeNull();
    });
  });

  describe('transport failure', () => {
    it('maps a non-HttpErrorResponse failure to the controlled transport fallback message', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        updateDoor: () => throwError(() => new Error('network down')),
      });

      let outcome: unknown;
      controller
        .updateDoor(1, { name: 'x', description: null, representativeAyahText: null, aliases: null, version: 1 })
        .subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'error', message: ABWAB_LABELS.writeTransportFallback });
    });
  });
});
