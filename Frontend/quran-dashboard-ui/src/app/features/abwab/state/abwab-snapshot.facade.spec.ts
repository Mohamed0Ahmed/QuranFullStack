import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

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
  isArchived: false,
  parentId: null,
  sectionId: null,
  orderValue: 1,
  version: 1,
};

function treeResponse(overrides: Partial<AbwabTreeDto> = {}): ApiResponse<AbwabTreeDto> {
  return { isSuccess: true, message: 'تم', data: { doors: [DOOR], sections: [], version: 'v1', ...overrides } };
}

function setup(getTree$: Observable<ApiResponse<AbwabTreeDto>>): AbwabSnapshotFacade {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [AbwabSnapshotFacade, { provide: AbwabApi, useValue: { getTree: () => getTree$ } }],
  });
  return TestBed.inject(AbwabSnapshotFacade);
}

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
