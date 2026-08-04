import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpHeaders, HttpResponse, HttpStatusCode } from '@angular/common/http';
import { Observable, Subscriber, map, of, throwError } from 'rxjs';

import { AbwabApi } from '../data-access/abwab.api';
import { AbwabTreeDto } from '../../../core/api/generated/models/abwab-tree-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { ABWAB_LABELS } from '../models/abwab.labels';
import { AbwabSnapshotFacade } from './abwab-snapshot.facade';

const DOOR = {
  id: 1,
  name: 'العلم بالله',
  description: null,
  representativeAyahText: null,
  aliases: [],
  directChildCount: 0,
  relationCount: 0,
  isArchived: false,
  parentId: null,
  sectionId: 1,
  sectionRetired: false,
  orderValue: 1,
  globalOrderValue: 1,
  version: 1,
};

function treeResponse(overrides: Partial<AbwabTreeDto> = {}): ApiResponse<AbwabTreeDto> {
  return { isSuccess: true, message: 'تم', data: { doors: [DOOR], sections: [], version: 'v1', ...overrides } };
}

// The api observes the whole response so the facade can read the ETag header beside the envelope.
// The stubs stay envelope-shaped and are wrapped here; a headerless response keeps every assertion
// below exactly as unconditional as it was.
function setup(getTree$: Observable<ApiResponse<AbwabTreeDto>>): AbwabSnapshotFacade {
  getTestBed().resetTestingModule();
  const getTree = () => getTree$.pipe(map((envelope) => new HttpResponse({ body: envelope })));
  TestBed.configureTestingModule({
    providers: [AbwabSnapshotFacade, { provide: AbwabApi, useValue: { getTree } }],
  });
  return TestBed.inject(AbwabSnapshotFacade);
}

/** The half of `setup` that headerless stubs cannot reach: real `HttpResponse`s carrying an ETag,
 * and the `304` that arrives on the error channel because HttpClient treats only 2xx as ok. */
type ValidatorStep = ApiResponse<AbwabTreeDto> | { readonly etag: string } | { readonly fails: unknown };

function setupWithValidators(steps: readonly ValidatorStep[]) {
  getTestBed().resetTestingModule();
  let call = 0;
  const sent: (string | null)[] = [];
  const getTree = (etag: string | null) =>
    new Observable<HttpResponse<ApiResponse<AbwabTreeDto>>>((subscriber) => {
      sent.push(etag);
      const step = steps[Math.min(call, steps.length - 1)];
      call += 1;
      // Tagged rather than sniffed: `HttpErrorResponse` implements `Error` without extending it,
      // so an `instanceof Error` test would silently route a 304 down the success path.
      if ('fails' in step) {
        subscriber.error(step.fails);
        return;
      }
      if ('etag' in step) {
        subscriber.next(
          new HttpResponse({ body: treeResponse(), headers: new HttpHeaders({ ETag: step.etag }) }),
        );
        subscriber.complete();
        return;
      }
      subscriber.next(new HttpResponse({ body: step }));
      subscriber.complete();
    });

  TestBed.configureTestingModule({
    providers: [AbwabSnapshotFacade, { provide: AbwabApi, useValue: { getTree } }],
  });
  return { facade: TestBed.inject(AbwabSnapshotFacade), sent };
}

// TESTING_DEBT I3, snapshot-facade half. The validator is no longer only an `If-None-Match` header
// the facade sends itself: it is the identity the relations cache keys on, so "what it holds and
// when" is now observable behavior.
describe('AbwabSnapshotFacade — the ETag validator it holds', () => {
  it('stores the response ETag and sends it back on the next read', () => {
    const { facade, sent } = setupWithValidators([{ etag: 'W/"gen-7"' }]);

    facade.load();
    expect(facade.snapshotValidator()).toBe('W/"gen-7"');
    expect(sent).toEqual([null]);

    facade.refresh();
    expect(sent).toEqual([null, 'W/"gen-7"']);
  });

  it('keeps value and validator on a 304, reports no error, and ends loading', () => {
    const { facade } = setupWithValidators([
      { etag: 'W/"gen-7"' },
      { fails: new HttpErrorResponse({ status: HttpStatusCode.NotModified }) },
    ]);

    facade.load();
    const loaded = facade.snapshot();

    facade.refresh();

    expect(facade.snapshot()).toEqual(loaded);
    expect(facade.snapshotValidator()).toBe('W/"gen-7"');
    expect(facade.errorMessage()).toBeNull();
    expect(facade.isLoading()).toBe(false);
  });

  // A validator that outlived a failed refresh is still the one that matches the value on screen,
  // and the two are only ever replaced together.
  it('keeps the validator when a refresh fails outright', () => {
    const { facade } = setupWithValidators([{ etag: 'W/"gen-7"' }, { fails: new Error('network down') }]);

    facade.load();
    facade.refresh();

    expect(facade.snapshotValidator()).toBe('W/"gen-7"');
    expect(facade.errorMessage()).toBe(ABWAB_LABELS.loadErrorFallback);
    expect(facade.snapshot()).not.toBeNull();
  });

  it('holds no validator from a response that carried none', () => {
    const { facade } = setupWithValidators([treeResponse()]);

    facade.load();

    expect(facade.snapshotValidator()).toBeNull();
  });
});

describe('AbwabSnapshotFacade', () => {
  it('load() builds the tree view model from a successful response', () => {
    const facade = setup(of(treeResponse()));
    facade.load();

    expect(facade.isLoading()).toBe(false);
    expect(facade.errorMessage()).toBeNull();
    expect(facade.snapshot()?.liveRoots.map((n) => n.name)).toEqual(['العلم بالله']);
  });

  it('carries the DTO version on the view model for diagnostics only', () => {
    const facade = setup(of(treeResponse({ version: 'diag-42' })));
    facade.load();

    expect(facade.snapshot()?.version).toBe('diag-42');
  });

  it('isEmpty is true only once loaded with zero doors', () => {
    const empty = setup(of(treeResponse({ doors: [] })));
    empty.load();
    expect(empty.isEmpty()).toBe(true);

    const populated = setup(of(treeResponse()));
    populated.load();
    expect(populated.isEmpty()).toBe(false);
  });

  it('a backend failure response on the first load surfaces its message and stays empty', () => {
    const failure$ = of<ApiResponse<AbwabTreeDto>>({ isSuccess: false, message: 'تعذر التحميل', data: null });
    const facade = setup(failure$);
    facade.load();

    expect(facade.errorMessage()).toBe('تعذر التحميل');
    expect(facade.snapshot()).toBeNull();
  });

  it('a transport failure produces the controlled fallback error, never a silent blank state', () => {
    const facade = setup(throwError(() => new Error('network down')));
    facade.load();

    expect(facade.isLoading()).toBe(false);
    expect(facade.errorMessage()).toBe(ABWAB_LABELS.loadErrorFallback);
    expect(facade.snapshot()).toBeNull();
  });

  it('refresh() keeps a previously loaded snapshot visible when the refetch then fails', () => {
    let callCount = 0;
    const facade = setup(
      new Observable<ApiResponse<AbwabTreeDto>>((subscriber) => {
        callCount += 1;
        if (callCount === 1) {
          subscriber.next(treeResponse());
          subscriber.complete();
        } else {
          subscriber.next({ isSuccess: false, message: 'تعارض', data: null });
          subscriber.complete();
        }
      }),
    );

    facade.load();
    expect(facade.snapshot()).not.toBeNull();

    facade.refresh();
    expect(facade.errorMessage()).toBe('تعارض');
    expect(facade.snapshot()).not.toBeNull();
  });
});

// F-36. `pendingRequest?.unsubscribe()` under `shareReplay(1)` (refCount: false) never tore the
// HTTP source down, so a superseded refresh landing late overwrote the newer tree AND its ETag —
// the validator the whole write path rebinds against. Two shapes are pinned: the plain race
// (nothing else holds the old request, cancellation must be real) and the write-controller shape
// (`refresh().subscribe()` keeps the old request alive, so last-write-wins must hold anyway).
describe('AbwabSnapshotFacade — F-36: an out-of-order response cannot overwrite a newer tree', () => {
  function setupRace() {
    getTestBed().resetTestingModule();
    const pending: Subscriber<HttpResponse<ApiResponse<AbwabTreeDto>>>[] = [];
    const getTree = () =>
      new Observable<HttpResponse<ApiResponse<AbwabTreeDto>>>((subscriber) => {
        pending.push(subscriber);
      });
    TestBed.configureTestingModule({
      providers: [AbwabSnapshotFacade, { provide: AbwabApi, useValue: { getTree } }],
    });
    const resolve = (index: number, name: string, etag: string) => {
      pending[index].next(
        new HttpResponse({
          body: treeResponse({ doors: [{ ...DOOR, name }] }),
          headers: new HttpHeaders({ ETag: etag }),
        }),
      );
      pending[index].complete();
    };
    return { facade: TestBed.inject(AbwabSnapshotFacade), resolve };
  }

  it('the newer refresh wins even when the older response lands last', () => {
    const { facade, resolve } = setupRace();

    facade.load();
    facade.refresh().subscribe();

    resolve(1, 'الأحدث', 'W/"gen-B"');
    resolve(0, 'الأقدم', 'W/"gen-A"');

    expect(facade.snapshot()?.liveRoots.map((n) => n.name)).toEqual(['الأحدث']);
    expect(facade.snapshotValidator()).toBe('W/"gen-B"');
    expect(facade.errorMessage()).toBeNull();
    expect(facade.isLoading()).toBe(false);
  });

  it('a superseded refresh still held by its caller neither writes state nor hands back the older tree', () => {
    const { facade, resolve } = setupRace();
    const seen: (string | undefined)[] = [];

    facade.refresh().subscribe((snapshot) => seen.push(snapshot?.liveRoots[0]?.name));
    facade.refresh().subscribe();

    resolve(1, 'الأحدث', 'W/"gen-B"');
    resolve(0, 'الأقدم', 'W/"gen-A"');

    expect(facade.snapshot()?.liveRoots.map((n) => n.name)).toEqual(['الأحدث']);
    expect(facade.snapshotValidator()).toBe('W/"gen-B"');
    expect(seen).toEqual(['الأحدث']);
  });
});
