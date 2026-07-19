import { getTestBed, TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { StemsApi } from '../data-access/stems.api';
import { StemsCache } from './stems-cache';
import { StemSummaryDto } from '../models/stems.models';
import { StemsDetailFacade } from './stems-detail.facade';
import { StemsDetailViewLoader } from './stems-detail-view.loader';

describe('StemsDetailFacade cleanup', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  it('unsubscribes pending detail load when route unbinds', () => {
    const detailSubscription = { unsubscribe: vi.fn() } as unknown as Subscription;
    const loadActiveView = vi.fn(() => detailSubscription);

    TestBed.configureTestingModule({
      providers: [
        StemsDetailFacade,
        { provide: StemsApi, useValue: {} },
        { provide: StemsCache, useValue: {} },
        { provide: StemsDetailViewLoader, useValue: { loadActiveView } },
      ],
    });

    const facade = TestBed.inject(StemsDetailFacade);

    facade.selectStem({ id: 42 } as StemSummaryDto);
    facade.unbindFromRoute();

    expect(loadActiveView).toHaveBeenCalledTimes(1);
    expect(detailSubscription.unsubscribe).toHaveBeenCalledTimes(1);
  });

  // Finding M24: unbindFromRoute() used to call only cancelPendingLoads(), which left the
  // controller's activeUrlState set. Re-binding to the SAME query params then short-circuited
  // inside applyUrlState() (stemsDetailUrlStatesEqual matched) and never re-issued the load,
  // stranding the panel on its cancelled 'loading' status with no request in flight.
  it('re-issues the summary load on rebind to the same URL state after a mid-flight unbind', () => {
    const queryParamMap$ = new BehaviorSubject(convertToParamMap({ stem: '42', view: 'ayahs' }));
    const route = { queryParamMap: queryParamMap$.asObservable() } as unknown as ActivatedRoute;

    const summary$ = new Subject<ApiResponse<StemSummaryDto>>();
    const getStemSummary = vi.fn(() => summary$.asObservable());

    TestBed.configureTestingModule({
      providers: [
        StemsDetailFacade,
        { provide: StemsApi, useValue: { getStemSummary } },
        { provide: StemsCache, useValue: { getOrLoad: (_key: string, load: () => unknown) => load() } },
        { provide: StemsDetailViewLoader, useValue: { loadActiveView: () => undefined } },
      ],
    });

    const facade = TestBed.inject(StemsDetailFacade);

    facade.bindToRoute(route);
    expect(getStemSummary).toHaveBeenCalledTimes(1);
    expect(facade.panelState().status).toBe('loading');

    // Unbind while the summary request is still in flight (it never emitted).
    facade.unbindFromRoute();

    // Rebind to the exact same query params (e.g. remounting the page).
    facade.bindToRoute(route);

    expect(getStemSummary).toHaveBeenCalledTimes(2);
    expect(facade.panelState().status).toBe('loading');
  });
});
