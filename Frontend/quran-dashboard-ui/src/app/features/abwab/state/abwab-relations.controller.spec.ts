import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpHeaders, HttpResponse, HttpStatusCode } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { AbwabRelationsController } from './abwab-relations.controller';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';
import { AbwabWriteController } from './abwab-write.controller';
import { AbwabSelectionStore } from './abwab-selection.store';
import { AbwabApi } from '../data-access/abwab.api';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabDoorRelationDto } from '../../../core/api/generated/models/abwab-door-relation-dto';
import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../core/auth/write-auth-failure.coordinator';

const DOOR = {
  id: 1,
  name: 'العلم بالله',
  description: null,
  representativeAyahText: null,
  aliases: [],
  directChildCount: 0,
  relationCount: 1,
  isArchived: false,
  parentId: null,
  sectionId: 1,
  sectionRetired: false,
  orderValue: 1,
  globalOrderValue: 1,
  version: 1,
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

function relationDto(id: number, otherDoorId: number, otherDoorName: string): AbwabDoorRelationDto {
  return { id, otherDoorId, otherDoorName, type: 1, direction: null };
}

/**
 * The validator is driven the way production drives it: the fake tree route stamps whatever ETag
 * the test has set, and the facade stores it on a 200. Nothing here writes the controller's cache
 * key by hand, so every "the cache was evicted" claim below rests on the real chain
 * write → snapshot refetch → new ETag → eviction.
 */
function setup(options: { readonly etag?: string | null } = {}) {
  getTestBed().resetTestingModule();
  let etag: string | null = options.etag === undefined ? 'W/"boot-1"' : options.etag;
  let treeOutcome: 'ok' | 'not-modified' | 'failure' = 'ok';
  let relations: readonly AbwabDoorRelationDto[] = [relationDto(10, 2, 'الصبر')];

  const getTree = vi.fn(
    () =>
      new Observable<HttpResponse<ApiResponse<unknown>>>((subscriber) => {
        if (treeOutcome === 'not-modified') {
          subscriber.error(new HttpErrorResponse({ status: HttpStatusCode.NotModified }));
          return;
        }
        if (treeOutcome === 'failure') {
          subscriber.error(new HttpErrorResponse({ status: 500 }));
          return;
        }
        subscriber.next(
          new HttpResponse({
            body: ok({ doors: [DOOR], sections: [], version: 'v1' }),
            headers: etag === null ? new HttpHeaders() : new HttpHeaders({ ETag: etag }),
          }),
        );
        subscriber.complete();
      }),
  );

  const getDoorRelations = vi.fn(() => of(ok([...relations])));
  const api = {
    getTree,
    getDoorRelations,
    addDoorRelations: vi.fn(() => of(ok([relationDto(11, 3, 'الشكر')]))),
    deleteRelation: vi.fn(() => of(ok(null))),
    updateDoor: vi.fn(() => of(ok({ ...DOOR, name: 'العلم بالله تعالى' }))),
    archiveDoor: vi.fn(() => of(ok(null))),
  };

  TestBed.configureTestingModule({
    providers: [
      AbwabRelationsController,
      AbwabSnapshotFacade,
      AbwabWriteController,
      AbwabSelectionStore,
      { provide: CurrentUserStore, useValue: { can: () => true } },
      { provide: WriteAuthFailureCoordinator, useValue: { handle: async () => null } },
      { provide: AbwabApi, useValue: api },
    ],
  });

  const facade = TestBed.inject(AbwabSnapshotFacade);
  const controller = TestBed.inject(AbwabRelationsController);
  const writes = TestBed.inject(AbwabWriteController);

  const load = (doorId = 1) => {
    let result: readonly string[] = [];
    controller.loadFor(doorId).subscribe((outcome) => {
      result = outcome.kind === 'success' ? outcome.relations.map((r) => r.otherDoorName) : ['error'];
    });
    return result;
  };

  return {
    controller,
    facade,
    writes,
    getDoorRelations,
    load,
    /** What a server-side change looks like from here: the next tree read carries a new generation. */
    bumpTreeGeneration: (next: string) => {
      etag = next;
    },
    setTreeOutcome: (outcome: 'ok' | 'not-modified' | 'failure') => {
      treeOutcome = outcome;
    },
    setServerRelations: (next: readonly AbwabDoorRelationDto[]) => {
      relations = next;
    },
  };
}

describe('AbwabRelationsController — the client-side relations cache', () => {
  it('serves a second read of the same door without touching the network', () => {
    const { facade, load, getDoorRelations } = setup();
    facade.load();

    expect(load()).toEqual(['الصبر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(1);

    expect(load()).toEqual(['الصبر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(1);
  });

  it('caches per door, not one list for all of them', () => {
    const { facade, load, getDoorRelations } = setup();
    facade.load();

    load(1);
    load(2);
    expect(getDoorRelations).toHaveBeenCalledTimes(2);

    load(1);
    expect(getDoorRelations).toHaveBeenCalledTimes(2);
  });

  // The eviction rule is deliberately source-blind: it asks whether the tree the lists were read
  // under is still the tree the client holds, and never which write moved it.
  it('drops every entry when the snapshot validator changes, whatever moved it', () => {
    const { facade, load, getDoorRelations, bumpTreeGeneration } = setup();
    facade.load();
    load(1);
    load(2);
    expect(getDoorRelations).toHaveBeenCalledTimes(2);

    bumpTreeGeneration('W/"boot-2"');
    facade.load();

    load(1);
    load(2);
    expect(getDoorRelations).toHaveBeenCalledTimes(4);
  });

  it('keeps serving hits when the refresh comes back 304 — nothing changed server-side', () => {
    const { facade, load, getDoorRelations, setTreeOutcome } = setup();
    facade.load();
    load();
    expect(getDoorRelations).toHaveBeenCalledTimes(1);

    setTreeOutcome('not-modified');
    facade.load();

    expect(load()).toEqual(['الصبر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(1);
  });

  // The facade keeps the value AND its validator on failure, so the pair on hand is still the
  // consistent one it was a moment ago. Wiping the cache here would punish a network blip.
  it('keeps the cache when a refresh fails outright', () => {
    const { facade, load, getDoorRelations, setTreeOutcome } = setup();
    facade.load();
    load();

    setTreeOutcome('failure');
    facade.load();

    expect(load()).toEqual(['الصبر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(1);
  });

  // No validator means no identity to be right about — including before the very first tree load.
  it('never serves from cache while no validator is held', () => {
    const { load, getDoorRelations } = setup();

    load();
    load();

    expect(getDoorRelations).toHaveBeenCalledTimes(2);
  });

  it('stores nothing from a failed read', () => {
    const { facade, controller, getDoorRelations } = setup();
    facade.load();
    getDoorRelations.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 500 })));

    let firstKind = '';
    controller.loadFor(1).subscribe((outcome) => {
      firstKind = outcome.kind;
    });
    expect(firstKind).toBe('error');

    controller.loadFor(1).subscribe();
    expect(getDoorRelations).toHaveBeenCalledTimes(2);
  });

  describe('writes evict through their own snapshot refresh', () => {
    it('adding a relation makes the next read of that door fetch again', () => {
      const { facade, controller, load, getDoorRelations, bumpTreeGeneration, setServerRelations } = setup();
      facade.load();
      load();
      expect(getDoorRelations).toHaveBeenCalledTimes(1);

      bumpTreeGeneration('W/"boot-2"');
      setServerRelations([relationDto(10, 2, 'الصبر'), relationDto(11, 3, 'الشكر')]);
      controller.addRelations(1, 'similarity', null, [3]).subscribe();

      expect(load()).toEqual(['الصبر', 'الشكر']);
      expect(getDoorRelations).toHaveBeenCalledTimes(2);
    });

    it('deleting a relation never serves the removed row back', () => {
      const { facade, controller, load, bumpTreeGeneration, setServerRelations } = setup();
      facade.load();
      expect(load()).toEqual(['الصبر']);

      bumpTreeGeneration('W/"boot-2"');
      setServerRelations([]);
      controller.deleteRelation(10).subscribe();

      expect(load()).toEqual([]);
    });

    it('archiving a door drops the lists that were read before it went dormant', () => {
      const { facade, writes, load, getDoorRelations, bumpTreeGeneration, setServerRelations } = setup();
      facade.load();
      load();

      bumpTreeGeneration('W/"boot-2"');
      setServerRelations([]);
      writes.archiveDoor(2, 1).subscribe();

      expect(load()).toEqual([]);
      expect(getDoorRelations).toHaveBeenCalledTimes(2);
    });

    // REGRESSION GUARD, not a discriminating test. Under today's clear-everything-on-validator-
    // change rule this passes for the same reason the source-agnostic case does. It is here to
    // fail the day a finer-grained invalidation is introduced and forgets that a rename rewrites
    // the PARTNER's list text while moving no count anywhere — see the rename pin in
    // `features/abwab/README.md` and `Persistence/Reads/Abwab/README.md`, which is what binds
    // the requirement today.
    it('evicts a cached list that merely MENTIONS a renamed door', () => {
      const { facade, writes, load, bumpTreeGeneration, setServerRelations } = setup();
      facade.load();
      expect(load()).toEqual(['الصبر']);

      bumpTreeGeneration('W/"boot-2"');
      setServerRelations([relationDto(10, 2, 'الصبر الجميل')]);
      writes
        .updateDoor(2, {
          name: 'الصبر الجميل',
          description: null,
          aliases: null,
          representativeAyahText: null,
          version: 1,
        })
        .subscribe();

      expect(load()).toEqual(['الصبر الجميل']);
    });
  });

  // The forced path exists because the write's own snapshot refetch is fire-and-forget
  // (`abwab-write.controller.ts`): when the modal reloads right after a write, the validator has
  // usually not moved yet, so the cache-aware read would hand back the pre-write list.
  it('refetchFor bypasses a live cache entry and replaces it', () => {
    const { facade, controller, load, getDoorRelations, setServerRelations } = setup();
    facade.load();
    expect(load()).toEqual(['الصبر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(1);

    setServerRelations([relationDto(11, 3, 'الشكر')]);
    let refetched: readonly string[] = [];
    controller.refetchFor(1).subscribe((outcome) => {
      refetched = outcome.kind === 'success' ? outcome.relations.map((r) => r.otherDoorName) : ['error'];
    });

    expect(refetched).toEqual(['الشكر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(2);
    // And what it wrote back is what a later cache-aware read serves.
    expect(load()).toEqual(['الشكر']);
    expect(getDoorRelations).toHaveBeenCalledTimes(2);
  });
});
