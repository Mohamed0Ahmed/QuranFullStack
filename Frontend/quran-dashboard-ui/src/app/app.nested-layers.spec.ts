import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from './app';
import { DetailOverlayHistoryService } from './core/navigation/detail-overlay/detail-overlay-history.service';
import { LemmaDetailFrame } from './core/navigation/detail-overlay/detail-overlay.models';
import { RootsExplorerPageComponent } from './features/words/pages/roots-explorer-page/roots-explorer-page.component';

/**
 * Mobile nested-layer invariants (Feature 029, B8, plan §5.9/§5.12 item 9):
 * with a responsive explorer drawer open UNDER the global detail overlay,
 * body scroll stays locked while either layer lives (reference-counted
 * ScrollLockService), and only the top dialog is interactive — the drawer sits
 * inside the inert app-shell subtree while the dialog is a shell sibling.
 */

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

const rootWordsPage = {
  page: 1,
  pageSize: 10,
  totalCount: 1,
  items: [{ displayText: 'كتاب', kind: 'simple', occurrencesCount: 3, uniqueWordId: 11 }],
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

const LEMMA_FRAME: LemmaDetailFrame = {
  kind: 'lemma',
  id: 100,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

describe('App nested layers on mobile (drawer under global overlay)', () => {
  let router: Router;
  let httpMock: HttpTestingController;
  let overlay: DetailOverlayHistoryService;

  beforeEach(() => {
    // jsdom lacks matchMedia/ResizeObserver; stubbed BEFORE TestBed. No media
    // query matches: the explorer renders its detail as a mobile drawer.
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: (query: string) => ({
        matches: false,
        media: query,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      }),
    });
    if (typeof globalThis.ResizeObserver === 'undefined') {
      globalThis.ResizeObserver = class {
        observe(): void {}
        unobserve(): void {}
        disconnect(): void {}
      } as unknown as typeof ResizeObserver;
    }

    document.body.style.overflow = '';

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'dashboard/words/roots', component: RootsExplorerPageComponent }]),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    router = TestBed.inject(Router);
    httpMock = TestBed.inject(HttpTestingController);
    overlay = TestBed.inject(DetailOverlayHistoryService);
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

  function flushPending(): void {
    for (const request of httpMock.match(() => true)) {
      const url = request.request.url;
      if (url.endsWith('/api/health')) {
        request.flush(ok({ status: 'healthy', checks: [] }));
      } else if (url.endsWith('/api/words/roots')) {
        request.flush(ok({ page: 1, pageSize: 100, totalCount: 1, items: [rootListItem] }));
      } else if (url.endsWith('/api/words/roots/1')) {
        request.flush(ok(rootListItem));
      } else if (url.endsWith('/api/words/roots/1/words/simple')) {
        request.flush(ok(rootWordsPage));
      } else if (url.endsWith('/api/words/lemmas/100')) {
        request.flush(ok(lemmaSummary));
      } else if (url.endsWith('/api/words/lemmas/100/words/simple')) {
        request.flush(ok(lemmaWordsPage));
      } else {
        throw new Error(`unexpected request in nested-layer spec: ${url}`);
      }
    }
  }

  async function loadStep(fixture: { detectChanges: () => void; nativeElement: unknown }): Promise<void> {
    // A flush can trigger a follow-up load (summary → view), so run three rounds.
    for (let round = 0; round < 3; round += 1) {
      await settle();
      fixture.detectChanges();
      flushPending();
    }
    await settle();
    fixture.detectChanges();
  }

  function query(fixture: { nativeElement: unknown }, selector: string): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector(selector);
  }

  function click(element: HTMLElement | null): void {
    expect(element).not.toBeNull();
    element!.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 }));
  }

  async function mountWithDrawerAndOverlay() {
    await router.navigateByUrl('/dashboard/words/roots?root=1');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    await loadStep(fixture);

    // The mobile drawer is open and alone holds the body scroll lock.
    expect(query(fixture, '[data-testid="root-details-modal"]')).not.toBeNull();
    expect(document.body.style.overflow).toBe('hidden');

    overlay.startStack(LEMMA_FRAME);
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-lemma-detail-overlay-adapter');
    await loadStep(fixture);
    expect(query(fixture, '[data-testid="detail-modal-shell"]')).not.toBeNull();
    expect(document.body.style.overflow).toBe('hidden');
    return fixture;
  }

  it('keeps the drawer inside the inert shell subtree while the dialog stays outside it', async () => {
    const fixture = await mountWithDrawerAndOverlay();

    const shell = query(fixture, 'qd-app-shell')!;
    const drawer = query(fixture, '[data-testid="root-details-modal"]')!;
    const dialog = query(fixture, '[data-testid="detail-modal-shell"]')!;

    expect(shell.getAttribute('inert')).not.toBeNull();
    expect(shell.getAttribute('aria-hidden')).toBe('true');
    expect(shell.contains(drawer)).toBe(true);
    expect(shell.contains(dialog)).toBe(false);

    // Closing the dialog lifts the inert state from the shell again.
    click(query(fixture, '[data-testid="detail-modal-close"]'));
    await loadStep(fixture);
    expect(query(fixture, 'qd-app-shell')!.getAttribute('inert')).toBeNull();
  });

  it('holds the body scroll lock until BOTH layers close: dialog first, then drawer', async () => {
    const fixture = await mountWithDrawerAndOverlay();

    click(query(fixture, '[data-testid="detail-modal-close"]'));
    await loadStep(fixture);
    expect(query(fixture, '[data-testid="detail-modal-shell"]')).toBeNull();
    // The drawer still lives: the reference-counted lock must survive.
    expect(query(fixture, '[data-testid="root-details-modal"]')).not.toBeNull();
    expect(document.body.style.overflow).toBe('hidden');

    click(query(fixture, '[data-testid="root-details-panel-close"]'));
    await loadStep(fixture);
    expect(query(fixture, '[data-testid="root-details-modal"]')).toBeNull();
    expect(document.body.style.overflow).toBe('');
  });

  it('holds the body scroll lock until BOTH layers close: drawer first, then dialog', async () => {
    const fixture = await mountWithDrawerAndOverlay();

    click(query(fixture, '[data-testid="root-details-panel-close"]'));
    await loadStep(fixture);
    expect(query(fixture, '[data-testid="root-details-modal"]')).toBeNull();
    // The dialog still lives: the lock must survive the drawer teardown.
    expect(query(fixture, '[data-testid="detail-modal-shell"]')).not.toBeNull();
    expect(document.body.style.overflow).toBe('hidden');

    click(query(fixture, '[data-testid="detail-modal-close"]'));
    await loadStep(fixture);
    expect(query(fixture, '[data-testid="detail-modal-shell"]')).toBeNull();
    expect(document.body.style.overflow).toBe('');
  }, 15000);
});
