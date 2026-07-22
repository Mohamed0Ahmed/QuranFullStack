import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Location } from '@angular/common';
import { SpyLocation, provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from '../../../app';
import { provideAuthTesting } from '../../../core/auth/auth.testing';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { RootDetailFrame } from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../shared/layout/breakpoints';
import { RootsExplorerPageComponent } from '../pages/roots-explorer-page/roots-explorer-page.component';

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
        provideAuthTesting(),
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

    overlay.startStack(ROOT_FRAME);
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-root-detail-overlay-adapter');
    await loadStep(fixture);

    const shellElement = shellQuery(fixture, 'qd-app-shell')!;
    const dialog = shellQuery(fixture, '[data-testid="detail-modal-shell"]')!;
    expect(shellElement.getAttribute('inert')).not.toBeNull();
    expect(shellElement.contains(dialog)).toBe(false);

    click(shellQuery(fixture, '[data-testid="detail-modal-shell"] [data-testid="root-lemma-item"]'));
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-lemma-detail-overlay-adapter');
    await loadStep(fixture);

    click(shellQuery(fixture, '[data-testid="lemma-details-tab-stems"]'));
    await loadStep(fixture);
    click(shellQuery(fixture, '[data-testid="detail-modal-shell"] [data-testid="lemma-stems-list-link"]'));
    await loadStep(fixture);
    await waitForSelector(fixture, 'qd-stem-detail-overlay-adapter');
    await loadStep(fixture);

    click(shellQuery(fixture, '[data-testid="stem-details-tab-ayahs"]'));
    await loadStep(fixture);
    await waitForSelector(fixture, '[data-testid="ayah-matches-open-mushaf"]');

    expect(router.url).toContain('/dashboard/words/roots');
    expect(router.url).toContain(encodeURIComponent(ROOT_SERIALIZED));
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).toContain(encodeURIComponent(STEM_AYAHS_SERIALIZED));

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

    const lastChange = location.urlChanges.at(-1) ?? '';
    expect(lastChange.startsWith('replace: /dashboard/mushaf')).toBe(true);

    click(shellQuery(fixture, '[data-testid="detail-modal-back"]'));
    await loadStep(fixture);
    expect(router.url).toContain('/dashboard/words/roots');
    expect(router.url).not.toContain('/dashboard/mushaf');
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).not.toContain(encodeURIComponent(STEM_AYAHS_SERIALIZED));
    expect(overlay.state().stack.map((frame) => frame.kind)).toEqual(['root', 'lemma']);
    expect(overlay.isOpen()).toBe(true);

    click(shellQuery(fixture, '[data-testid="detail-modal-close"]'));
    await loadStep(fixture);
    expect(router.url).toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(router.url).not.toContain('qdDetailOpen');
    expect(overlay.isRetainedClosed()).toBe(true);
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).toBeNull();
    expect(shellQuery(fixture, '[data-testid="detail-modal-restore"]')).not.toBeNull();
    expect(shellQuery(fixture, 'qd-app-shell')!.getAttribute('inert')).toBeNull();

    click(shellQuery(fixture, '[data-testid="detail-modal-restore"]'));
    await loadStep(fixture);
    expect(overlay.isOpen()).toBe(true);
    expect(overlay.state().stack).toHaveLength(2);
    expect(router.url).toContain('qdDetailOpen=1');
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).not.toBeNull();

    location.back();
    await loadStep(fixture);
    expect(overlay.isRetainedClosed()).toBe(true);
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).toBeNull();
    expect(shellQuery(fixture, '[data-testid="detail-modal-restore"]')).not.toBeNull();

    location.forward();
    await loadStep(fixture);
    expect(overlay.isOpen()).toBe(true);
    expect(overlay.state().stack).toHaveLength(2);
    expect(shellQuery(fixture, '[data-testid="detail-modal-shell"]')).not.toBeNull();

    overlay.navigateBaseWithOverlay('/dashboard/mushaf', {
      page: '92',
      ayah: '4:57',
      focusAyah: '4:57',
      panel: 'ayah',
    });
    await loadStep(fixture);
    expect(router.url).toContain('/dashboard/mushaf');
    expect(overlay.state().stack.map((frame) => frame.kind)).toEqual(['root', 'lemma']);

    location.back();
    await loadStep(fixture);
    const browserBackUrl = router.url;
    expect(browserBackUrl).toContain('/dashboard/words/roots');
    expect(browserBackUrl).toContain(encodeURIComponent(ROOT_SERIALIZED));
    expect(browserBackUrl).not.toContain(encodeURIComponent(LEMMA_STEMS_SERIALIZED));
    expect(overlay.state().stack.map((frame) => frame.kind)).toEqual(['root']);
    expect(overlay.isOpen()).toBe(true);

    location.forward();
    await loadStep(fixture);
    expect(router.url).toContain('/dashboard/mushaf');
    click(shellQuery(fixture, '[data-testid="detail-modal-back"]'));
    await loadStep(fixture);
    expect(router.url).toBe(browserBackUrl);
    expect(overlay.state().stack.map((frame) => frame.kind)).toEqual(['root']);
    expect(overlay.isOpen()).toBe(true);
  }, 30000);
});
