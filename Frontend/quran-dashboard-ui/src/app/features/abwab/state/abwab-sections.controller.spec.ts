import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { Observable, map, of, throwError } from 'rxjs';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';

import { AbwabSectionsController } from './abwab-sections.controller';
import { AbwabWriteController } from './abwab-write.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabApi } from '../data-access/abwab.api';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { AbwabSectionDto } from '../../../core/api/generated/models/abwab-section-dto';
import { AbwabTreeSectionDto } from '../../../core/api/generated/models/abwab-tree-section-dto';
import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

function httpError(status: number, message: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { isSuccess: false, message, data: null } });
}

const SECTION: AbwabSectionDto = { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1 };
const TREE_SECTION: AbwabTreeSectionDto = { ...SECTION, doorsInScopeCount: 0 };

function setup(fakeApi: Partial<Record<keyof AbwabApi, (...args: unknown[]) => unknown>>) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AbwabSectionsController,
      AbwabWriteController,
      AbwabSnapshotFacade,
      AbwabSelectionStore,
      { provide: CurrentUserStore, useValue: { can: () => true } },
      { provide: WriteAuthFailureCoordinator, useValue: { handle: async () => null } },
      // getTree observes the whole response now; the fakes stay envelope-shaped and are wrapped
      // headerless here, so no test below sends or stores a validator.
      {
        provide: AbwabApi,
        useValue: {
          ...fakeApi,
          getTree: () =>
            (fakeApi.getTree!() as Observable<ApiResponse<AbwabTreeDto>>).pipe(
              map((envelope) => new HttpResponse({ body: envelope })),
            ),
        },
      },
    ],
  });
  return {
    controller: TestBed.inject(AbwabSectionsController),
    facade: TestBed.inject(AbwabSnapshotFacade),
  };
}

describe('AbwabSectionsController', () => {
  it('reads sections live from the facade snapshot, not a cached copy', () => {
    const { controller, facade } = setup({
      getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [TREE_SECTION], version: 'v' })),
    });
    facade.load();

    expect(controller.sections()).toEqual([TREE_SECTION]);
  });

  it('createSection delegates to the shared write policy and refreshes on success', () => {
    let capturedName: string | undefined;
    const { controller, facade } = setup({
      getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [TREE_SECTION], version: 'v' })),
      createSection: (...args: unknown[]) => {
        capturedName = (args[0] as { name: string }).name;
        return of(ok<AbwabSectionDto>(SECTION));
      },
    });
    facade.load();

    let outcome: unknown;
    controller.createSection('اللغة العربية').subscribe((result) => (outcome = result));

    expect(capturedName).toBe('اللغة العربية');
    expect(outcome).toEqual({ kind: 'success', data: SECTION });
  });

  describe('M27 — delete answers 409 and keeps the modal open (the actual backend copy, not the stale plan string)', () => {
    it('reports the conflict outcome unchanged, without closing anything itself', () => {
      // The wire string is Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:117
      // (AbwabSectionHasLiveDoors) «لا يمكن حذف القسم لاحتوائه على أبواب حالية»; the
      // controller reports it as a conflict, unchanged, rather than restating it locally.
      const backendMessage = 'لا يمكن حذف القسم لاحتوائه على أبواب حالية';
      const { controller, facade } = setup({
        getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [TREE_SECTION], version: 'v' })),
        deleteSection: () => throwError(() => httpError(409, backendMessage)),
      });
      facade.load();

      let outcome: unknown;
      controller.deleteSection(1).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'conflict', message: backendMessage });
    });
  });

  describe('M28 — deleting a section holding only archived doors succeeds and refetches', () => {
    it('reports success and the facade reflects the post-delete snapshot', () => {
      let getTreeCalls = 0;
      const { controller, facade } = setup({
        getTree: () => {
          getTreeCalls += 1;
          const sections = getTreeCalls === 1 ? [TREE_SECTION] : [];
          return of(ok<AbwabTreeDto>({ doors: [], sections, version: 'v' }));
        },
        deleteSection: () => of(ok(null)),
      });
      facade.load();

      let outcome: unknown;
      controller.deleteSection(1).subscribe((result) => (outcome = result));

      expect(outcome).toEqual({ kind: 'success', data: null });
      expect(facade.snapshot()?.sections).toEqual([]);
    });
  });

  it('renameSection delegates to the shared write policy with the given id/name/version', () => {
    let capturedArgs: unknown[] = [];
    const { controller, facade } = setup({
      getTree: () => of(ok<AbwabTreeDto>({ doors: [], sections: [TREE_SECTION], version: 'v' })),
      renameSection: (...args: unknown[]) => {
        capturedArgs = args;
        return of(ok<AbwabSectionDto>({ ...SECTION, name: 'اسم جديد' }));
      },
    });
    facade.load();

    controller.renameSection(1, 'اسم جديد', 1).subscribe();

    expect(capturedArgs).toEqual([1, { name: 'اسم جديد', version: 1 }]);
  });
});
