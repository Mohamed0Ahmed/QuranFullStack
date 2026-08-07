import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, map, of, throwError } from 'rxjs';

import { AbwabWriteController, AbwabWriteOutcome } from './abwab-write.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabApi } from '../data-access/abwab.api';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabTreeDoorDto } from '../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabDoorDto } from '../../../core/api/generated/models/abwab-door-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { BulkMoveDoorsCommand } from '../../../core/api/generated/models/bulk-move-doors-command';
import { ReorderDoorBody } from '../../../core/api/generated/models/reorder-door-body';
import { ReorderSectionBody } from '../../../core/api/generated/models/reorder-section-body';
import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';
import { PERMISSION_CODES, PermissionCode } from '../../../core/auth/permission-code';

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
  sectionId: 1,
  orderValue: 1,
  globalOrderValue: 1,
  version: 2,
};

const REORDERED_SECTION: AbwabSectionDto = { id: 1, name: 'اللغة العربية', orderValue: 3, version: 4 };

function httpError(status: number, message: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { isSuccess: false, message, data: null } });
}

interface FakeApi {
  getTree: () => Observable<ApiResponse<AbwabTreeDto>>;
  updateDoor?: () => Observable<ApiResponse<AbwabDoorDto>>;
  createDoor?: () => Observable<ApiResponse<AbwabDoorDto>>;
  archiveDoor?: () => Observable<ApiResponse<unknown>>;
  moveDoor?: () => Observable<ApiResponse<AbwabDoorDto>>;
  restoreDoor?: () => Observable<ApiResponse<unknown>>;
  reorderDoor?: (id: number, body: ReorderDoorBody) => Observable<ApiResponse<AbwabDoorDto>>;
  createSection?: () => Observable<ApiResponse<AbwabSectionDto>>;
  deleteSection?: () => Observable<ApiResponse<unknown>>;
  renameSection?: () => Observable<ApiResponse<AbwabSectionDto>>;
  reorderSection?: (id: number, body: ReorderSectionBody) => Observable<ApiResponse<AbwabSectionDto>>;
  bulkMoveDoors?: (command: BulkMoveDoorsCommand) => Observable<ApiResponse<AbwabDoorDto[]>>;
  bulkArchiveDoors?: () => Observable<ApiResponse<number[]>>;
  addDoorRelations?: () => Observable<ApiResponse<unknown>>;
  deleteRelation?: () => Observable<ApiResponse<unknown>>;
}

function setup(
  fakeApi: FakeApi,
  options: {
    readonly permissions?: readonly PermissionCode[];
    readonly handleAuthFailure?: (error: unknown) => Promise<{ kind: 'unauthorized' | 'forbidden'; message: string | null } | null>;
  } = {},
) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabWriteController,
      AbwabSnapshotFacade,
      AbwabSelectionStore,
      { provide: CurrentUserStore, useValue: { can: (permission: PermissionCode) => (options.permissions ?? PERMISSION_CODES).includes(permission) } },
      { provide: WriteAuthFailureCoordinator, useValue: { handle: options.handleAuthFailure ?? (async () => null) } },
      // getTree observes the whole response now; the fakes stay envelope-shaped and are wrapped
      // headerless here, so no test below sends or stores a validator.
      {
        provide: AbwabApi,
        useValue: {
          ...fakeApi,
          getTree: () => fakeApi.getTree().pipe(map((envelope) => new HttpResponse({ body: envelope }))),
        },
      },
    ],
  });
  return {
    controller: TestBed.inject(AbwabWriteController),
    facade: TestBed.inject(AbwabSnapshotFacade),
    selection: TestBed.inject(AbwabSelectionStore),
  };
}

describe('AbwabWriteController', () => {
  describe('Phase 9 last-dispatch permission guard', () => {
    const deniedCases: readonly {
      readonly name: string;
      readonly request: (controller: AbwabWriteController) => Observable<unknown>;
      readonly fake: (call: ReturnType<typeof vi.fn>) => FakeApi;
    }[] = [
      { name: 'section create', request: (controller) => controller.createSection({ name: 'قسم' }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), createSection: call }) },
      { name: 'section rename', request: (controller) => controller.renameSection(1, { name: 'قسم', version: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), renameSection: call }) },
      { name: 'section delete', request: (controller) => controller.deleteSection(1), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), deleteSection: call }) },
      { name: 'section reorder', request: (controller) => controller.reorderSection(1, { position: 2, version: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), reorderSection: call }) },
      { name: 'door create', request: (controller) => controller.createDoor({ name: 'باب', description: null, representativeAyahText: null, aliases: [], parentId: null, sectionId: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), createDoor: call }) },
      { name: 'door edit', request: (controller) => controller.updateDoor(1, { name: 'باب', description: null, representativeAyahText: null, aliases: [], version: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), updateDoor: call }) },
      { name: 'door move', request: (controller) => controller.moveDoor(1, { targetParentId: null, targetSectionId: 1, version: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), moveDoor: call }) },
      { name: 'door reorder', request: (controller) => controller.reorderDoor(1, { position: 2, scope: 1, version: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), reorderDoor: call }) },
      { name: 'door archive', request: (controller) => controller.archiveDoor(1, 1), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), archiveDoor: call }) },
      { name: 'door restore', request: (controller) => controller.restoreDoor(1, { version: 1 }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), restoreDoor: call }) },
      { name: 'bulk move', request: (controller) => controller.bulkMoveDoors(null, 1), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), bulkMoveDoors: call }) },
      { name: 'bulk archive', request: (controller) => controller.bulkArchiveDoors(), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), bulkArchiveDoors: call }) },
      { name: 'relation add', request: (controller) => controller.addDoorRelations(1, { type: 1, direction: null, targetDoorIds: [2] }), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), addDoorRelations: call }) },
      { name: 'relation delete', request: (controller) => controller.deleteRelation(1), fake: (call) => ({ getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), deleteRelation: call }) },
    ];

    it.each(deniedCases)('does not dispatch $name for a read-only visitor', ({ request, fake }) => {
      const apiCall = vi.fn();
      const { controller } = setup(fake(apiCall), { permissions: [] });
      let outcome: unknown;

      request(controller).subscribe((value) => (outcome = value));

      expect(apiCall).not.toHaveBeenCalled();
      expect(outcome).toEqual({ kind: 'forbidden', message: ABWAB_LABELS.writePermissionDenied });
    });

    it('refreshes through the auth coordinator on a 403 without retrying the mutation', async () => {
      const createDoor = vi.fn().mockReturnValue(throwError(() => httpError(403, 'ممنوع')));
      const handleAuthFailure = vi.fn().mockResolvedValue({ kind: 'forbidden', message: 'ممنوع' });
      const { controller } = setup(
        { getTree: () => of(ok({ doors: [], sections: [], version: 'v' })), createDoor },
        { permissions: ['abwab.doors.create'], handleAuthFailure },
      );
      let outcome: unknown;

      controller
        .createDoor({ name: 'باب', description: null, representativeAyahText: null, aliases: [], parentId: null, sectionId: 1 })
        .subscribe((value) => (outcome = value));
      await Promise.resolve();
      await Promise.resolve();

      expect(createDoor).toHaveBeenCalledTimes(1);
      expect(handleAuthFailure).toHaveBeenCalledTimes(1);
      expect(outcome).toEqual({ kind: 'forbidden', message: 'ممنوع' });
    });
  });

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
      // F-51: a failure reaches exactly ONE live region. updateDoor is dispatched from the door
      // modal, which stays open on failure and renders the message in qd-state variant="error"
      // (role="alert") — abwab-door-fields-form.component.html:2. That element owns this failure,
      // so the polite announcer must NOT also carry it.
      expect(controller.announcement()).toBeNull();
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

  // F-51. The contract is "a failure reaches exactly ONE live region". Which region depends on
  // whether the operation has a visible error surface, so the two branches are pinned together —
  // asserting only the drop side would let a silenced failure pass.
  describe('F-51 — a write failure reaches exactly one live region', () => {
    it('keeps the announcer for a failed reorder, whose inline commit has no error surface at all', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        reorderDoor: () => throwError(() => httpError(409, 'تعذر إعادة الترتيب')),
      });

      controller.reorderDoor(1, { position: 2, scope: 1, version: 1 }).subscribe();

      // The tree's inline order editor closes on commit and nothing on the row binds the outcome,
      // so the polite announcer is the user's ONLY channel here. Dropping it would silence it.
      expect(controller.announcement()).toBe('تعذر إعادة الترتيب');
    });

    it('keeps the announcer for a failed move, whose picker closes before the request is sent', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        moveDoor: () => throwError(() => httpError(409, 'تعذر النقل')),
      });

      controller.moveDoor(1, { targetParentId: null, targetSectionId: 2, version: 1 }).subscribe();

      expect(controller.announcement()).toBe('تعذر النقل');
    });

    it('keeps the announcer for a failed archive, whose confirm inserts its alert rather than reserving it', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        archiveDoor: () => throwError(() => httpError(409, 'تعذر الأرشفة')),
      });

      controller.archiveDoor(1, 1).subscribe();

      expect(controller.announcement()).toBe('تعذر الأرشفة');
    });

    it('drops the announcer for a failed door edit, whose form reserves its alert region', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        updateDoor: () => throwError(() => httpError(400, 'اسم مكرر')),
      });

      controller
        .updateDoor(1, { name: 'x', description: null, representativeAyahText: null, aliases: null, version: 1 })
        .subscribe();

      // abwab-door-fields-form.component.html renders the failure as a qd-state role="alert"
      // inserted into a plain role="dialog", which announces on insertion. One region, not two.
      expect(controller.announcement()).toBeNull();
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
          sectionId: 1,
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

    // The submit filter drops archived ids, so the bulk set is a superset of what went to the
    // wire. The message must name the refs the request actually carried, never the live set.
    it('names only the doors the request carried, not the archived one the bulk set still holds', () => {
      const { controller, facade, selection } = setup({
        getTree: () =>
          of(
            ok<AbwabTreeDto>({
              doors: [door({ id: 1, name: 'باب مؤرشف', isArchived: true, version: 9 }), door({ id: 3, name: 'باب حي', version: 9 })],
              sections: [],
              version: 'v',
            }),
          ),
        bulkMoveDoors: () => throwError(() => httpError(409, 'stale')),
      });
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(1, 9);
      selection.toggleBulk(3, 9);

      let outcome: unknown;
      controller.bulkMoveDoors(null, null).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'conflict', message: ABWAB_LABELS.bulkConflictMessage('باب حي') });
    });
  });

  describe('the bulk-archive stale-id 404 (Slice D): archived ids never reach the wire, and a 404 names doors', () => {
    const parentAndChildBothArchived: AbwabTreeDto = {
      doors: [
        door({ id: 1, name: 'د-أب', isArchived: true, version: 9 }),
        door({ id: 2, name: 'د-ابن', parentId: 1, isArchived: true, version: 9 }),
        door({ id: 3, name: 'باب حي', version: 9 }),
      ],
      sections: [],
      version: 'v',
    };

    it('submits only doors the snapshot still shows live', () => {
      let captured: BulkMoveDoorsCommand | null = null;
      const { controller, facade, selection } = setup({
        getTree: () => of(ok<AbwabTreeDto>(parentAndChildBothArchived)),
        bulkMoveDoors: (command) => {
          captured = command;
          return of(ok<AbwabDoorDto[]>([]));
        },
      });
      facade.load();
      selection.setBulkMode(true);
      // Straight into the set, bypassing the rebind that would already have dropped them —
      // the filter has to hold on its own.
      selection.toggleBulk(1, 9);
      selection.toggleBulk(2, 9);
      selection.toggleBulk(3, 9);

      controller.bulkMoveDoors(null, null).subscribe();

      expect(captured).toEqual({ doors: [{ doorId: 3, version: 9 }], targetParentId: null, targetSectionId: null });
    });

    // The only 404 that can still reach the user: another client archived the doors, so this
    // client's snapshot still showed them live and the submit filter had no reason to drop them.
    it('names the vanished doors on a 404, reading them off the snapshot the failure refetches', () => {
      let getTreeCalls = 0;
      const { controller, facade, selection } = setup({
        getTree: () => {
          getTreeCalls += 1;
          return of(
            ok<AbwabTreeDto>(
              getTreeCalls === 1
                ? {
                    doors: [door({ id: 1, name: 'د-أب' }), door({ id: 2, name: 'د-ابن', parentId: 1 })],
                    sections: [],
                    version: 'v',
                  }
                : parentAndChildBothArchived,
            ),
          );
        },
        bulkArchiveDoors: () => throwError(() => httpError(404, 'الباب غير موجود')),
      });
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(1, 1);
      selection.toggleBulk(2, 1);

      let outcome: unknown;
      controller.bulkArchiveDoors().subscribe((result) => (outcome = result));

      const expected = ABWAB_LABELS.bulkVanishedMessage(2, 'د-أب، د-ابن');
      expect(outcome).toEqual({ kind: 'invalid', message: expected });
      // F-51: bulk archive KEEPS the announcer. Its confirm dialog does render the message, but
      // the surface is `@if`-inserted inside an already-focused role="alertdialog"
      // (abwab-page.component.html:292-293) rather than a reserved live region, so it is not
      // reliably announced — the polite announcer is the screen-reader user's real channel here.
      expect(controller.announcement()).toBe(expected);
      expect(selection.bulkSet().size).toBe(0); // the refresh's rebind dropped them
    });

    it('falls back to the backend message when the refreshed snapshot still shows every attempted door live', () => {
      const { controller, facade, selection } = setup({
        getTree: () =>
          of(ok<AbwabTreeDto>({ doors: [door({ id: 3, name: 'باب حي', version: 9 })], sections: [], version: 'v' })),
        bulkArchiveDoors: () => throwError(() => httpError(404, 'الباب غير موجود')),
      });
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(3, 9);

      let outcome: unknown;
      controller.bulkArchiveDoors().subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'invalid', message: 'الباب غير موجود' });
    });

    it('drops the archived ids from the bulk set after a successful bulk archive', () => {
      let getTreeCalls = 0;
      const { controller, facade, selection } = setup({
        getTree: () => {
          getTreeCalls += 1;
          return of(
            ok<AbwabTreeDto>(
              getTreeCalls === 1
                ? {
                    doors: [door({ id: 1, name: 'د-أب' }), door({ id: 2, name: 'د-ابن', parentId: 1 })],
                    sections: [],
                    version: 'v',
                  }
                : parentAndChildBothArchived,
            ),
          );
        },
        bulkArchiveDoors: () => of(ok<number[]>([1, 2])),
      });
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(1, 1);
      selection.toggleBulk(2, 1);

      controller.bulkArchiveDoors().subscribe();

      expect(selection.bulkSet().size).toBe(0);
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
      // Asserted through the confirm message the cell names, not just the helper behind it.
      expect(controller.archiveConfirmMessageFor(1)).toBe(ABWAB_LABELS.archiveConfirm(3));
      expect(controller.archiveConfirmMessageFor(1)).toContain('3 أبواب');
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

  describe('M19 — restore announces itself and passes its destination through', () => {
    it('announces the restore', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        restoreDoor: () => of(ok(EDITED_DOOR)),
      } as unknown as FakeApi);

      controller.restoreDoor(1, { version: 1 }).subscribe();
      expect(controller.announcement()).toBe(ABWAB_LABELS.restoreAnnouncement);
    });

    // The announcement belongs to the command, not to the payload. A 204 restore hands the
    // controller a null envelope and a 200 may carry null data; both are successes and both
    // must still speak, or a screen-reader user gets silence on a write that committed.
    it.each([
      ['a null envelope', null],
      ['an envelope carrying null data', ok(null)],
    ])('announces the restore when the backend answers with %s', (_shape, response) => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        restoreDoor: () => of(response),
      } as unknown as FakeApi);

      controller.restoreDoor(1, { version: 1 }).subscribe();

      expect(controller.announcement()).toBe(ABWAB_LABELS.restoreAnnouncement);
    });

    // Every public write op declares its own success phrase, so consecutive writes replace
    // each other's message rather than leaking the previous one into the region.
    it('announces each write with its own phrase, the newer replacing the older', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        restoreDoor: () => of(ok(EDITED_DOOR)),
        createDoor: () => of(ok(EDITED_DOOR)),
      } as unknown as FakeApi);

      controller.restoreDoor(1, { version: 1 }).subscribe();
      expect(controller.announcement()).toBe(ABWAB_LABELS.restoreAnnouncement);

      controller
        .createDoor({ name: 'ب', description: null, representativeAyahText: null, aliases: null, parentId: null, sectionId: 1 })
        .subscribe();

      expect(controller.announcement()).toBe(ABWAB_LABELS.doorCreatedAnnouncement);
    });

    // The destination is passed through untouched: an omitted key means "back where it came from",
    // and turning that into an explicit null would ask the backend for a section-less door.
    it('passes a stated destination section through, and omits the key when there is none', () => {
      const bodies: unknown[] = [];
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        restoreDoor: ((_id: number, body: unknown) => {
          bodies.push(body);
          return of(ok(EDITED_DOOR));
        }) as unknown as () => Observable<ApiResponse<unknown>>,
      } as unknown as FakeApi);

      controller.restoreDoor(1, { sectionId: 7, version: 1 }).subscribe();
      controller.restoreDoor(1, { version: 1 }).subscribe();

      expect(bodies).toEqual([{ sectionId: 7, version: 1 }, { version: 1 }]);
    });
  });

  describe('204 No Content — a payload-less success is not a transport error', () => {
    // Archive and section-delete answer 204, so HttpClient hands the controller a `null`
    // envelope rather than `{isSuccess, data}`. Mocking these with `ok(null)` — a well-formed
    // envelope carrying null *data* — passes while the real null *envelope* throws, which is
    // exactly how this shipped broken: the throw was swallowed as a transport failure while
    // the backend write had already committed.
    it('treats a null response to archiveDoor as a success and still refetches', () => {
      let treeCalls = 0;
      const { controller, selection } = setup({
        getTree: () => {
          treeCalls += 1;
          return of(ok<AbwabTreeDto>({ doors: [door({ id: 1, name: 'باق', version: 7 })], sections: [], version: 'v2' }));
        },
        archiveDoor: () => of(null),
      } as unknown as FakeApi);
      selection.select(1, 1);

      let outcome: unknown;
      controller.archiveDoor(1, 1).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'success', data: null });
      expect(controller.announcement()).toBe(ABWAB_LABELS.doorArchivedAnnouncement);
      expect(treeCalls).toBe(1);
      expect(selection.selectedVersion()).toBe(7);
    });

    it('treats a null response to deleteSection as a success and still refetches', () => {
      let treeCalls = 0;
      const { controller } = setup({
        getTree: () => {
          treeCalls += 1;
          return of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v2' }));
        },
        deleteSection: () => of(null),
      } as unknown as FakeApi);

      let outcome: unknown;
      controller.deleteSection(1).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'success', data: null });
      expect(controller.announcement()).toBe(ABWAB_LABELS.sectionDeletedAnnouncement);
      expect(treeCalls).toBe(1);
    });

    it('treats a null response to deleteRelation as a success and still refetches', () => {
      let treeCalls = 0;
      const { controller } = setup({
        getTree: () => {
          treeCalls += 1;
          return of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v2' }));
        },
        deleteRelation: () => of(null),
      } as unknown as FakeApi);

      let outcome: unknown;
      controller.deleteRelation(1).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'success', data: null });
      expect(controller.announcement()).toBe(ABWAB_LABELS.relationDeletedAnnouncement);
      expect(treeCalls).toBe(1);
    });
  });

  describe('T406 — reorderDoor forwards the scope on the wire unchanged', () => {
    it.each([
      ['Section', 1 as const],
      ['Global', 2 as const],
    ])('passes scope=%s straight through to the API body', (_label, scope) => {
      let capturedBody: ReorderDoorBody | null = null;
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        reorderDoor: (id, body) => {
          capturedBody = body;
          return of(ok<AbwabDoorDto>(EDITED_DOOR));
        },
      });

      controller.reorderDoor(1, { position: 3, scope, version: 1 }).subscribe();

      expect(capturedBody).toEqual({ position: 3, scope, version: 1 });
    });
  });

  describe('T502 — reorderSection: success refreshes, 409/400 map through the shared policy, no door selection cleared', () => {
    it('refreshes the facade snapshot on success', () => {
      let treeCalls = 0;
      const { controller, facade } = setup({
        getTree: () => {
          treeCalls += 1;
          return of(ok<AbwabTreeDto>({ doors: [], sections: [{ id: 1, name: 'اللغة العربية', orderValue: 3, version: 4, doorsInScopeCount: 0 }], version: 'v2' }));
        },
        reorderSection: () => of(ok<AbwabSectionDto>(REORDERED_SECTION)),
      });

      let outcome: unknown;
      controller.reorderSection(1, { position: 3, version: 3 }).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'success', data: REORDERED_SECTION });
      expect(treeCalls).toBe(1);
      expect(facade.snapshot()?.sections[0].orderValue).toBe(3);
    });

    it('maps a 409 to conflict with the backend message', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        reorderSection: () => throwError(() => httpError(409, 'تم تعديل القسم من مستخدم آخر')),
      });

      let outcome: unknown;
      controller.reorderSection(1, { position: 3, version: 1 }).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'conflict', message: 'تم تعديل القسم من مستخدم آخر' });
    });

    it('maps a 400 to invalid with the backend message', () => {
      const { controller } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        reorderSection: () => throwError(() => httpError(400, 'الترتيب المطلوب خارج نطاق الأقسام')),
      });

      let outcome: unknown;
      controller.reorderSection(1, { position: 99, version: 1 }).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'invalid', message: 'الترتيب المطلوب خارج نطاق الأقسام' });
    });

    it('does not clear the door selection on a 409 — a section write invalidates no door', () => {
      const { controller, selection } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' })),
        reorderSection: () => throwError(() => httpError(409, 'تعارض')),
      });
      selection.select(1, 1);

      controller.reorderSection(1, { position: 3, version: 1 }).subscribe();

      expect(selection.selectedDoorId()).toBe(1);
    });
  });

  // F-52. One success policy for the one announcer region: every write that declares a success
  // announcement speaks politely, and each op's phrase is its own. (createDoor, archiveDoor and
  // deleteSection are pinned silent by earlier tests in this file and are deliberately absent.)
  describe('F-52 — write successes announce politely, per operation', () => {
    const emptyTree = () => of(ok<AbwabTreeDto>({ doors: [], sections: [], version: 'v' }));

    it.each([
      [
        'createDoor',
        { createDoor: () => of(ok<AbwabDoorDto>(EDITED_DOOR)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> =>
          controller.createDoor({ name: 'ب', description: null, representativeAyahText: null, aliases: null, parentId: null, sectionId: 1 }),
        ABWAB_LABELS.doorCreatedAnnouncement,
      ],
      [
        'updateDoor',
        { updateDoor: () => of(ok<AbwabDoorDto>(EDITED_DOOR)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> =>
          controller.updateDoor(1, { name: 'x', description: null, representativeAyahText: null, aliases: null, version: 1 }),
        ABWAB_LABELS.doorUpdatedAnnouncement,
      ],
      [
        'archiveDoor',
        { archiveDoor: () => of(ok<unknown>(null)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.archiveDoor(1, 1),
        ABWAB_LABELS.doorArchivedAnnouncement,
      ],
      [
        'deleteSection',
        { deleteSection: () => of(ok<unknown>(null)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.deleteSection(1),
        ABWAB_LABELS.sectionDeletedAnnouncement,
      ],
      [
        'moveDoor',
        { moveDoor: () => of(ok<AbwabDoorDto>(EDITED_DOOR)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.moveDoor(1, { targetParentId: null, targetSectionId: 2, version: 1 }),
        ABWAB_LABELS.doorMovedAnnouncement,
      ],
      [
        'reorderDoor',
        { reorderDoor: () => of(ok<AbwabDoorDto>(EDITED_DOOR)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.reorderDoor(1, { position: 2, scope: 1, version: 1 }),
        ABWAB_LABELS.doorReorderedAnnouncement,
      ],
      [
        'createSection',
        { createSection: () => of(ok<AbwabSectionDto>(REORDERED_SECTION)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.createSection({ name: 'قسم' }),
        ABWAB_LABELS.sectionCreatedAnnouncement,
      ],
      [
        'renameSection',
        { renameSection: () => of(ok<AbwabSectionDto>(REORDERED_SECTION)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.renameSection(1, { name: 'قسم', version: 1 }),
        ABWAB_LABELS.sectionRenamedAnnouncement,
      ],
      [
        'reorderSection',
        { reorderSection: () => of(ok<AbwabSectionDto>(REORDERED_SECTION)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.reorderSection(1, { position: 2, version: 1 }),
        ABWAB_LABELS.sectionReorderedAnnouncement,
      ],
      [
        'addDoorRelations',
        { addDoorRelations: () => of(ok<unknown>([])) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> =>
          controller.addDoorRelations(1, { type: 1, direction: null, targetDoorIds: [2, 3, 4] }),
        ABWAB_LABELS.relationsAddedAnnouncement(3),
      ],
      [
        'deleteRelation',
        { deleteRelation: () => of(ok<unknown>(null)) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.deleteRelation(1),
        ABWAB_LABELS.relationDeletedAnnouncement,
      ],
    ] as const)('%s announces its own phrase on success', (_op, api, dispatch, expected) => {
      const { controller } = setup({ getTree: emptyTree, ...api });

      dispatch(controller).subscribe();

      expect(controller.announcement()).toBe(expected);
    });

    it.each([
      [
        'bulkArchiveDoors',
        { bulkArchiveDoors: () => of(ok<number[]>([1, 2])) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.bulkArchiveDoors(),
        ABWAB_LABELS.bulkArchiveAnnouncement(2),
      ],
      [
        'bulkMoveDoors',
        { bulkMoveDoors: () => of(ok<AbwabDoorDto[]>([])) },
        (controller: AbwabWriteController): Observable<AbwabWriteOutcome<unknown>> => controller.bulkMoveDoors(null, null),
        ABWAB_LABELS.bulkMoveAnnouncement(2),
      ],
    ] as const)('%s announces the counted phrase for the doors it carried', (_op, api, dispatch, expected) => {
      const { controller, facade, selection } = setup({
        getTree: () =>
          of(ok<AbwabTreeDto>({ doors: [door({ id: 1, name: 'أ' }), door({ id: 2, name: 'ب' })], sections: [], version: 'v' })),
        ...api,
      });
      facade.load();
      selection.setBulkMode(true);
      selection.toggleBulk(1, 1);
      selection.toggleBulk(2, 1);

      dispatch(controller).subscribe();

      expect(controller.announcement()).toBe(expected);
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
