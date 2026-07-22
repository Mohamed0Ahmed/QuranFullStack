import { getTestBed, TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Subject, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LemmasApi } from '../data-access/lemmas.api';
import { LemmasCache } from './lemmas-cache';
import { LemmaSummaryDto } from '../models/lemmas.models';
import { LemmasDetailFacade } from './lemmas-detail.facade';
import { LemmasDetailViewLoader } from './lemmas-detail-view.loader';

describe('LemmasDetailFacade cleanup', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  it('unsubscribes pending detail load when route unbinds', () => {
    const detailSubscription = { unsubscribe: vi.fn() } as unknown as Subscription;
    const loadActiveView = vi.fn(() => detailSubscription);

    TestBed.configureTestingModule({
      providers: [
        LemmasDetailFacade,
        { provide: LemmasApi, useValue: {} },
        { provide: LemmasCache, useValue: {} },
        { provide: LemmasDetailViewLoader, useValue: { loadActiveView } },
      ],
    });

    const facade = TestBed.inject(LemmasDetailFacade);

    facade.selectLemma({ id: 42 } as LemmaSummaryDto);
    facade.unbindFromRoute();

    expect(loadActiveView).toHaveBeenCalledTimes(1);
    expect(detailSubscription.unsubscribe).toHaveBeenCalledTimes(1);
  });

  it('re-issues the summary load on rebind to the same URL state after a mid-flight unbind', () => {
    const queryParamMap$ = new BehaviorSubject(convertToParamMap({ lemma: '42', view: 'ayahs' }));
    const route = { queryParamMap: queryParamMap$.asObservable() } as unknown as ActivatedRoute;

    const summary$ = new Subject<ApiResponse<LemmaSummaryDto>>();
    const getLemmaSummary = vi.fn(() => summary$.asObservable());

    TestBed.configureTestingModule({
      providers: [
        LemmasDetailFacade,
        { provide: LemmasApi, useValue: { getLemmaSummary } },
        { provide: LemmasCache, useValue: { getOrLoad: (_key: string, load: () => unknown) => load() } },
        { provide: LemmasDetailViewLoader, useValue: { loadActiveView: () => undefined } },
      ],
    });

    const facade = TestBed.inject(LemmasDetailFacade);

    facade.bindToRoute(route);
    expect(getLemmaSummary).toHaveBeenCalledTimes(1);
    expect(facade.panelState().status).toBe('loading');

    facade.unbindFromRoute();

    facade.bindToRoute(route);

    expect(getLemmaSummary).toHaveBeenCalledTimes(2);
    expect(facade.panelState().status).toBe('loading');
  });
});
