import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from '../../../app';
import { provideAuthTesting } from '../../../core/auth/auth.testing';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../shared/layout/breakpoints';
import { RootsExplorerPageComponent } from '../pages/roots-explorer-page/roots-explorer-page.component';
import { RootsDetailFacade } from '../state/roots-detail.facade';

function ok<T>(data: T) {
  return { isSuccess: true, data, message: null, errors: null };
}

const rootListItem = {
  id: 1,
  rootText: 'كتب',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 3,
  simpleWordsCount: 2,
  tashkeelWordsCount: 2,
  lemmasCount: 1,
  stemsCount: 1,
};

const rootLemmasResponse = {
  lemmas: [{ lemmaId: 100, lemmaText: 'كِتاب', occurrencesCount: 3 }],
};

const lemmaSummary = {
  id: 100,
  lemmaText: 'كِتاب',
  occurrencesCount: 3,
  ayahsCount: 2,
  surahsCount: 1,
  rootId: 1,
  rootText: 'كتب',
  simpleWordsCount: 1,
  tashkeelWordsCount: 1,
  stemsCount: 1,
  typeDistribution: [],
};

const lemmaWordsPage = {
  page: 1,
  pageSize: 10,
  totalCount: 1,
  items: [{ uniqueWordId: 7, displayText: 'كِتاب', occurrencesCount: 3 }],
};

describe('Entity detail overlay explorer invariant', () => {
  let router: Router;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    // jsdom lacks matchMedia; the roots page reads the desktop breakpoint (the side panel must
    // render inline) and the theme service reads color-scheme. The test-setup safety net undoes
    // this stub even when a test throws.
    vi.stubGlobal('matchMedia', (query: string) => ({
      matches: query === QD_BP_DESKTOP_MIN_QUERY,
      media: query,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
    }));

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'dashboard/words/roots', component: RootsExplorerPageComponent }]),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAuthTesting(),
      ],
    });
    router = TestBed.inject(Router);
    httpMock = TestBed.inject(HttpTestingController);
    sessionStorage.clear();
    router.initialNavigation();
  });

  async function settle(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 10));
    await vi.waitFor(() => {
      if (router.getCurrentNavigation() !== null) {
        throw new Error('navigation in flight');
      }
    });
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  // Deferred adapter chunks load asynchronously; poll until the selector renders.
  async function waitForSelector(
    fixture: { detectChanges: () => void; nativeElement: unknown },
    selector: string,
  ): Promise<void> {
    await vi.waitFor(
      () => {
        fixture.detectChanges();
        if ((fixture.nativeElement as HTMLElement).querySelector(selector) === null) {
          throw new Error(`still waiting for ${selector}`);
        }
      },
      { timeout: 5000, interval: 25 },
    );
  }

  it('starts a one-frame overlay stack from a side-panel lemma link without touching the page selection, requests, or query keys', async () => {
    await router.navigateByUrl('/dashboard/words/roots?root=1&view=lemmas');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    // The app shell issues a one-time backend health probe; irrelevant to the
    // invariant, so it is flushed and excluded from the request accounting.
    for (const health of httpMock.match((req) => req.url.endsWith('/api/health'))) {
      health.flush(ok({ status: 'healthy', checks: [] }));
    }

    // Page load: roots list + selected-root summary, then the lemmas view.
    httpMock.expectOne((req) => req.url.endsWith('/api/words/roots')).flush(
      ok({ page: 1, pageSize: 100, totalCount: 1, items: [rootListItem] }),
    );
    httpMock.expectOne((req) => req.url.endsWith('/api/words/roots/1')).flush(ok(rootListItem));
    await settle();
    httpMock.expectOne((req) => req.url.endsWith('/api/words/roots/1/lemmas')).flush(ok(rootLemmasResponse));
    await settle();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const facade = TestBed.inject(RootsDetailFacade);
    const panelBefore = facade.panelState();
    expect(panelBefore.selectedRootId).toBe(1);
    expect(panelBefore.view).toBe('lemmas');
    expect(httpMock.match(() => true).map((request) => request.request.url)).toEqual([]);

    const lemmaLink = root.querySelector('[data-testid="root-lemma-item"]') as HTMLAnchorElement;
    expect(lemmaLink).toBeTruthy();
    expect(lemmaLink.getAttribute('target')).toBeNull();
    lemmaLink.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 }));
    await settle();
    await waitForSelector(fixture, 'qd-lemma-detail-overlay-adapter');

    // (a) The overlay dialog opened with the lemma adapter mounted.
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).not.toBeNull();
    expect(root.querySelector('qd-lemma-detail-overlay-adapter')).not.toBeNull();

    // (c) Every new request belongs to the overlay's lemma detail — the page
    // issued NO additional roots list/detail requests.
    const overlayRequests = httpMock.match(() => true);
    expect(overlayRequests.length).toBeGreaterThan(0);
    for (const request of overlayRequests) {
      expect(request.request.url).toContain('/api/words/lemmas/100');
      expect(request.request.url).not.toContain('/api/words/roots');
    }
    overlayRequests
      .find((request) => request.request.url.endsWith('/api/words/lemmas/100'))!
      .flush(ok(lemmaSummary));
    await settle();
    httpMock
      .expectOne((req) => req.url.endsWith('/api/words/lemmas/100/words/simple'))
      .flush(ok(lemmaWordsPage));
    await settle();
    fixture.detectChanges();

    // (b) The page facade's panel state is untouched: same object identity,
    // same selection, same view.
    expect(facade.panelState()).toBe(panelBefore);
    expect(facade.selectedRootId()).toBe(1);
    expect(facade.view()).toBe('lemmas');

    // Still no page requests after the overlay finished loading.
    expect(httpMock.match((req) => req.url.includes('/api/words/roots'))).toHaveLength(0);

    // (d) The page's selection query keys survive alongside the overlay keys.
    const url = router.url;
    expect(url).toContain('root=1');
    expect(url).toContain('view=lemmas');
    expect(url).toContain('qdDetail=v1~lemma~100~words~simple~mentioned~1~-');
    expect(url).toContain('qdDetailOpen=1');
  }, 15000);
});
