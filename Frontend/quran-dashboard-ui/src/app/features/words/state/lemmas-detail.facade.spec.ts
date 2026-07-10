import { getTestBed, TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { Subscription } from 'rxjs';

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
});
