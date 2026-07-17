import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Location } from '@angular/common';
import { SpyLocation, provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from '../../../app';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { RootDetailFrame } from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../shared/layout/breakpoints';
import { RootsExplorerPageComponent } from '../pages/roots-explorer-page/roots-explorer-page.component';

/**
 * End-to-end overlay/ayah-continuity integration (Feature 029, B7/B8, plan
 * §5.8/§5.12 item 10): a three-frame stack built over an explorer page rides an
 * ayah click onto the Mushaf base with replace semantics, then dialog Back,
 * Close, Restore, and browser Back/Forward all behave per provenance. Asserted
 * via the router URL and the persistent overlay host DOM.
 */

@Component({ standalone: true, template: '' })
class BlankMushafPageComponent {}

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

const lemmaStemsResponse = {
  stems: [{ stemId: 55, stemText: 'مكتوب', occurrencesCount: 2 }],
};

const stemSummary = {
  id: 55,
  stemText: 'مكتوب',
  occurrencesCount: 2,
  ayahsCount: 1,
  surahsCount: 1,
  rootId: 1,
  rootText: 'كتب',
  lemmaId: 100,
  lemmaText: 'كِتاب',
  simpleWordsCount: 1,
  tashkeelWordsCount: 1,
  typeDistribution: [],
};

const stemWordsPage = {
  page: 1,
  pageSize: 10,
  totalCount: 1,
  items: [{ uniqueWordId: 9, displayText: 'مكتوب', occurrencesCount: 2 }],
};

const stemAyahsPage = {
  page: 1,
  pageSize: 100,
  totalCount: 1,
  items: [
    {
      ayahId: 7001,
      pageNumber: 92,
      surahNameArabic: 'النساء',
      verseKey: '4:57',
      words: [{ isMatched: true, textUthmani: 'كلمة-تجريبية' }],
    },
  ],
};

const ROOT_FRAME: RootDetailFrame = {
  kind: 'root',
  id: 1,
  view: 'lemmas',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const ROOT_SERIALIZED = 'v1~root~1~lemmas~simple~mentioned~1';
const LEMMA_STEMS_SERIALIZED = 'v1~lemma~100~stems~simple~mentioned~1~-';
const STEM_AYAHS_SERIALIZED = 'v1~stem~55~ayahs~simple~mentioned~1~-';

describe('Entity detail overlay ayah continuity (B7/B8)', () => {
  let router: Router;
  let location: SpyLocation;
  let httpMock: HttpTestingController;
  let overlay: DetailOverlayHistoryService;

  beforeEach(() => {
    // jsdom lacks matchMedia; stubbed BEFORE TestBed so the shell/pages can read it.
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: (query: string) => ({
        matches: query === QD_BP_DESKTOP_MIN_QUERY,
        media: query,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      }),
    });

    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'dashboard/words/roots', component: RootsExplorerPageComponent },
          { path: 'dashboard/mushaf', component: BlankMushafPageComponent },
        ]),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    router = TestBed.inject(Router);
    location = TestBed.inject(Location) as SpyLocation;
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

  /** Deferred adapter chunks load asynchronously; poll until the selector renders. */
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

  /** Flushes every outstanding request against the fixture catalogue (cache hits simply match nothing). */
  function flushPending(): void {
    for (const request of httpMock.match(() => true)) {
      const url = request.request.url;
      if (url.endsWith('/api/health')) {
        request.flush(ok({ status: 'healthy', checks: [] }));
      } else if (url.endsWith('/api/words/roots')) {
        request.flush(ok({ page: 1, pageSize: 100, totalCount: 1, items: [rootListItem] }));
      } else if (url.endsWith('/api/words/roots/1')) {
        request.flush(ok(rootListItem));
      } else if (url.endsWith('/api/words/roots/1/lemmas')) {
        request.flush(ok(rootLemmasResponse));
      } else if (url.endsWith('/api/words/lemmas/100')) {
        request.flush(ok(lemmaSummary));
      } else if (url.endsWith('/api/words/lemmas/100/words/simple')) {
        request.flush(ok(lemmaWordsPage));
      } else if (url.endsWith('/api/words/lemmas/100/stems')) {
        request.flush(ok(lemmaStemsResponse));
      } else if (url.endsWith('/api/words/stems/55')) {
        request.flush(ok(stemSummary));
      } else if (url.endsWith('/api/words/stems/55/words/simple')) {
        request.flush(ok(stemWordsPage));
      } else if (url.endsWith('/api/words/stems/55/ayahs')) {
        request.flush(ok(stemAyahsPage));
      } else {
        throw new Error(`unexpected request in continuity spec: ${url}`);
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

  function shellQuery(fixture: { nativeElement: unknown }, selector: string): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector(selector);
  }

  function click(element: HTMLElement | null): void {
    expect(element).not.toBeNull();
    element!.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 }));
  }

  it('carries a three-frame stack onto the Mushaf as a replace, then Back/Close/Restore/history behave per provenance', async () => {
    await router.navigateByUrl('/dashboard/words/roots');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    await loadStep(fixture);

    // Open a root overlay (the same API a Mushaf entity link invokes).
    overlay.startStack(ROOT_FRAME);
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-root-detail-overlay-adapter');
    await loadStep(fixture);

    // While the dialog is open, the app shell is inert and the dialog is outside it.
    const shellElement = shellQuery(fixture, 'qd-app-shell')!;
    const dialog = shellQuery(fixture, '[data-testid="detail-modal-shell"]')!;
    expect(shellElement.getAttribute('inert')).not.toBeNull();
    expect(shellElement.contains(dialog)).toBe(false);

    // Append a lemma frame via the real overlay anchor (append mode).
    click(shellQuery(fixture, '[data-testid="detail-modal-shell"] [data-testid="root-lemma-item"]'));
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-lemma-detail-overlay-adapter');
    await loadStep(fixture);

    // Switch the lemma detail to its stems view and append a stem frame.
    click(shellQuery(fixture, '[data-testid="lemma-details-tab-stems"]'));
    await loadStep(fixture);
    click(shellQuery(fixture, '[data-testid="detail-modal-shell"] [data-testid="lemma-stems-list-link"]'));
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-stem-detail-overlay-adapter');
    await loadStep(fixture);

    // Open the stem's ayahs view: the ayah continuity link renders inside the detail.
    click(shellQuery(fixture, '[data-testid="stem-details-tab-ayahs"]'));
    await loadStep(fixture);
    await waitForSelector(fixture, '[data-testid="ayah-matches-open-mushaf"]');

    expect(router.url).toContain('/dashboard/words/roots');
    expect(router.url).toContain(encodeURIComponent(ROOT_SERIALIZED));
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).toContain(encodeURIComponent(STEM_AYAHS_SERIALIZED));

    // Click the ayah link: land on the Mushaf base with the SAME stack, open.
    click(shellQuery(fixture, '[data-testid="ayah-matches-open-mushaf"]'));
    await loadStep(fixture);

    expect(router.url).toContain('/dashboard/mushaf');
    expect(router.url).toContain('page=92');
    expect(router.url).toContain('panel=ayah');
    expect(router.url).toContain(encodeURIComponent(ROOT_SERIALIZED));
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).toContain(encodeURIComponent(STEM_AYAHS_SERIALIZED));
    expect(router.url).toContain('qdDetailOpen=1');
    expect(overlay.isOpen()).toBe(true);
    expect(overlay.state().stack).toHaveLength(3);

    // Replace semantics: the continuity step rewrote the current entry instead
    // of pushing a new one (SpyLocation logs replaceState as 'replace: <url>').
    const lastChange = location.urlChanges.at(-1) ?? '';
    expect(lastChange.startsWith('replace: /dashboard/mushaf')).toBe(true);

    // Dialog Back converges with browser Back (B7/B8): the continuity step
    // replaced this entry without moving the one below it, so the parent card is
    // still adjacent and Back pops to it together with its historical Words base
    // — it does not strand the user on the Mushaf.
    click(shellQuery(fixture, '[data-testid="detail-modal-back"]'));
    await loadStep(fixture);
    expect(router.url).toContain('/dashboard/words/roots');
    expect(router.url).not.toContain('/dashboard/mushaf');
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).not.toContain(encodeURIComponent(STEM_AYAHS_SERIALIZED));
    expect(overlay.state().stack.map((frame) => frame.kind)).toEqual(['root', 'lemma']);
    expect(overlay.isOpen()).toBe(true);

    // Close keeps the retained stack in the URL and shows the restore control.
    click(shellQuery(fixture, '[data-testid="detail-modal-close"]'));
    await loadStep(fixture);
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).not.toContain('qdDetailOpen');
    expect(overlay.isRetainedClosed()).toBe(true);
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).toBeNull();
    expect(shellQuery(fixture, '[data-testid="detail-modal-restore"]')).not.toBeNull();
    expect(shellQuery(fixture, 'qd-app-shell')!.getAttribute('inert')).toBeNull();

    // Restore reopens the exact stack as a push.
    click(shellQuery(fixture, '[data-testid="detail-modal-restore"]'));
    await loadStep(fixture);
    expect(overlay.isOpen()).toBe(true);
    expect(overlay.state().stack).toHaveLength(2);
    expect(router.url).toContain('qdDetailOpen=1');
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).not.toBeNull();

    // Browser Back returns to the closed/restorable entry (restore was a push)...
    location.back();
    await loadStep(fixture);
    expect(overlay.isRetainedClosed()).toBe(true);
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).toBeNull();
    expect(shellQuery(fixture, '[data-testid="detail-modal-restore"]')).not.toBeNull();

    // ...and browser Forward converges back onto the open state.
    location.forward();
    await loadStep(fixture);
    expect(overlay.isOpen()).toBe(true);
    expect(overlay.state().stack).toHaveLength(2);
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).not.toBeNull();
  }, 30000);
});
