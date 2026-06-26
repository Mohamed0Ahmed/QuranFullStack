import { getTestBed, TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { Subscription } from 'rxjs';

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
});
