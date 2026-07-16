import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from '../../../app';
import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import {
  DetailFrame,
  RootDetailFrame,
} from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { ENTITY_DETAIL_KIND_TITLES } from './entity-detail-overlay.labels';

const rootFrame: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const lemmaFrame: DetailFrame = {
  kind: 'lemma',
  id: 55,
  view: 'ayahs',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

const stemFrame: DetailFrame = {
  kind: 'stem',
  id: 66,
  view: 'ayahs',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

const uniqueFrame: DetailFrame = {
  kind: 'unique',
  mode: 'simple',
  id: 77,
  view: 'ayahs',
  ayahPage: 1,
};

const wordTypeFrame: DetailFrame = {
  kind: 'wordType',
  tashkeelWordId: 88,
  contextCode: 'noun',
  case: 'all',
  tense: 'all',
  voice: 'all',
  view: 'ayahs',
  detailPage: 1,
};

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

describe('EntityDetailOverlayHostComponent (composition root)', () => {
  let router: Router;
  let service: DetailOverlayHistoryService;

  beforeEach(() => {
    // jsdom lacks matchMedia; the app shell's theme service reads it on init.
    if (typeof window.matchMedia !== 'function') {
      Object.defineProperty(window, 'matchMedia', {
        configurable: true,
        writable: true,
        value: () => ({
          matches: false,
          addEventListener: () => undefined,
          removeEventListener: () => undefined,
          addListener: () => undefined,
          removeListener: () => undefined,
        }),
      });
    }

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '**', component: BlankPageComponent }]),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    router = TestBed.inject(Router);
    service = TestBed.inject(DetailOverlayHistoryService);
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

  async function createApp() {
    await router.navigateByUrl('/dashboard/words/roots');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('mounts the persistent host beside the shell with no dialog while the URL has no overlay', async () => {
    const fixture = await createApp();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('qd-entity-detail-overlay-host')).not.toBeNull();
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).toBeNull();
    expect(root.querySelector('qd-app-shell')!.hasAttribute('inert')).toBe(false);
  });

  it('opens the dialog with the entity-kind title and makes only the app shell inert', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
    expect(dialog.textContent).toContain(ENTITY_DETAIL_KIND_TITLES.root);

    const shell = root.querySelector('qd-app-shell')!;
    expect(shell.hasAttribute('inert')).toBe(true);
    expect(shell.getAttribute('aria-hidden')).toBe('true');
    expect(dialog.closest('[inert]')).toBeNull();
  });

  it('Escape closes into the restore control and restore reopens the same stack', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    root
      .querySelector('[data-testid="detail-modal-shell"]')!
      .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await settle();
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="detail-modal-shell"]')).toBeNull();
    const restore = root.querySelector('[data-testid="detail-modal-restore"]') as HTMLButtonElement;
    expect(restore.getAttribute('aria-label')).toContain(ENTITY_DETAIL_KIND_TITLES.root);
    expect(root.querySelector('qd-app-shell')!.hasAttribute('inert')).toBe(false);

    restore.click();
    await settle();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).not.toBeNull();
    expect(service.isOpen()).toBe(true);
  });

  it('dialog Back pops one frame while browser semantics stay URL-authoritative', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    service.appendFrame({ ...rootFrame, kind: 'root', id: 1000 });
    await settle();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="detail-modal-back"]') as HTMLButtonElement).click();
    await settle();
    fixture.detectChanges();

    expect(service.state().stack).toHaveLength(1);
    expect(root.querySelector('[data-testid="detail-modal-back"]')).toBeNull();
    expect(root.querySelector('[data-testid="detail-modal-shell"]')).not.toBeNull();
  });

  /** Deferred adapter chunks load asynchronously; poll until the selector renders. */
  async function waitForSelector(fixture: { detectChanges: () => void; nativeElement: unknown }, selector: string): Promise<void> {
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

  it('mounts the root adapter frameless inside the shell (no nested dialog chrome)', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    await waitForSelector(fixture, 'qd-root-detail-overlay-adapter');

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('qd-root-detail-overlay-adapter')).not.toBeNull();
    expect(root.querySelector('[data-testid="root-details-panel-frameless"]')).not.toBeNull();
    // The shell owns the single dialog: the panel contributes no chrome of its own.
    expect(root.querySelectorAll('[role="dialog"]')).toHaveLength(1);
    expect(root.querySelector('[data-testid="root-details-modal"]')).toBeNull();
    expect(root.querySelector('[data-testid="root-details-panel-close"]')).toBeNull();
    expect(root.querySelector('[data-testid="root-details-panel-label"]')).toBeNull();
  });

  it('mounts the matching real adapter for each non-root frame kind', async () => {
    const fixture = await createApp();
    const root = fixture.nativeElement as HTMLElement;
    const cases: readonly { frame: DetailFrame; selector: string }[] = [
      { frame: lemmaFrame, selector: 'qd-lemma-detail-overlay-adapter' },
      { frame: stemFrame, selector: 'qd-stem-detail-overlay-adapter' },
      { frame: uniqueFrame, selector: 'qd-unique-detail-overlay-adapter' },
      { frame: wordTypeFrame, selector: 'qd-word-type-detail-overlay-adapter' },
    ];

    service.startStack(lemmaFrame);
    for (const { frame, selector } of cases) {
      if (frame !== lemmaFrame) {
        service.appendFrame(frame);
      }
      await settle();
      await waitForSelector(fixture, selector);
      expect(root.querySelector(selector)).not.toBeNull();
      // The shell stays the single dialog regardless of the mounted adapter.
      expect(root.querySelectorAll('[role="dialog"]')).toHaveLength(1);
    }
  });

  it('shows the loaded entity title and falls back to the kind label after a frame change', async () => {
    const fixture = await createApp();
    const httpMock = TestBed.inject(HttpTestingController);
    service.startStack(rootFrame);
    await settle();
    await waitForSelector(fixture, 'qd-root-detail-overlay-adapter');

    const summaryRequest = httpMock.expectOne((req) => req.url.endsWith('/api/words/roots/999'));
    summaryRequest.flush({
      isSuccess: true,
      data: {
        id: 999,
        rootText: 'كتب',
        occurrencesCount: 5,
        ayahsCount: 4,
        surahsCount: 3,
        simpleWordsCount: 2,
        tashkeelWordsCount: 2,
        lemmasCount: 1,
        stemsCount: 1,
      },
      message: null,
      errors: null,
    });
    await settle();

    const root = fixture.nativeElement as HTMLElement;
    const title = () => root.querySelector('.detail-modal-shell__title')?.textContent ?? '';
    await vi.waitFor(
      () => {
        fixture.detectChanges();
        if (!title().includes('كتب')) {
          throw new Error('still waiting for the loaded entity title');
        }
      },
      { timeout: 5000, interval: 25 },
    );
    expect(title()).toContain('كتب');

    // A cross-entity append destroys the root adapter, which clears its title;
    // the lemma adapter starts its own summary load (left pending here), so the
    // heading falls back to the lemma kind label.
    service.appendFrame(lemmaFrame);
    await settle();
    await waitForSelector(fixture, 'qd-lemma-detail-overlay-adapter');
    expect(title()).toContain(ENTITY_DETAIL_KIND_TITLES.lemma);
    expect(title()).not.toContain('كتب');
  });

  it('announces the Arabic cap status when a ninth append is refused', async () => {
    const fixture = await createApp();
    service.startStack(rootFrame);
    await settle();
    for (let id = 1; id <= 7; id += 1) {
      service.appendFrame({ ...rootFrame, id });
      await settle();
    }
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-modal-live-status"]')).toBeNull();

    service.appendFrame({ ...rootFrame, id: 4242 });
    await settle();
    fixture.detectChanges();

    const status = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="detail-modal-live-status"]');
    expect(status?.textContent).toContain('ثماني');
    expect(service.state().stack).toHaveLength(8);
  });
});
