import { Location } from '@angular/common';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { LemmaDetailFrame, RootDetailFrame } from './detail-overlay.models';
import { PROVENANCE_STATE_KEY } from './detail-overlay-provenance';
import { serializeDetailFrame } from './detail-overlay-url-codec';

@Component({ standalone: true, template: '' })
class BlankBootstrapPageComponent {}

const rootFrame: RootDetailFrame = {
  kind: 'root',
  id: 999,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
};

const lemmaFrame: LemmaDetailFrame = {
  kind: 'lemma',
  id: 555,
  view: 'ayahs',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

describe('DetailOverlayHistoryService bootstrap ownership', () => {
  let router: Router;
  let location: Location;
  let service: DetailOverlayHistoryService;

  function configureHarness(): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'dashboard/words/roots', component: BlankBootstrapPageComponent },
          { path: '**', component: BlankBootstrapPageComponent },
        ]),
        provideLocationMocks(),
      ],
    });
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    service = TestBed.inject(DetailOverlayHistoryService);
  }

  beforeEach(() => {
    configureHarness();
    sessionStorage.clear();
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

  it('re-seeds a fresh entry instead of inferring ownership from remembered URL data before initial navigation', async () => {
    const baseUrl = '/dashboard/words/roots';
    const deepLink =
      `${baseUrl}?qdDetail=${encodeURIComponent(serializeDetailFrame(rootFrame))}` +
      `&qdDetail=${encodeURIComponent(serializeDetailFrame(lemmaFrame))}` +
      '&qdDetailOpen=1';
    sessionStorage.setItem(`${PROVENANCE_STATE_KEY}:chain`, JSON.stringify([baseUrl, deepLink]));
    location.replaceState(deepLink);
    const goSpy = vi.spyOn(location, 'go');

    service.start();
    router.initialNavigation();
    await settle();

    const seededCalls = goSpy.mock.calls.filter(([, , state]) => {
      const record = state as Record<string, unknown> | undefined;
      return record?.[PROVENANCE_STATE_KEY] !== undefined;
    });
    expect(seededCalls).toHaveLength(2);
    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['root', 'lemma']);

    service.back();
    await settle();
    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['root']);
  });

  it('retains a top-replaced entry on reload when initial navigation preserves its entry provenance', async () => {
    const deepLink =
      '/dashboard/words/roots?root=5&qdDetail=' +
      encodeURIComponent(serializeDetailFrame(rootFrame)) +
      '&qdDetailOpen=1';
    const replacedFrame: RootDetailFrame = { ...rootFrame, view: 'ayahs', detailPage: 2 };
    location.replaceState(deepLink);
    service.start();
    router.initialNavigation();
    await settle();
    service.replaceTopFrame(replacedFrame);
    await settle();

    const replacedUrl = location.path();
    const persistedState = location.getState();
    const persistedProvenance = (persistedState as Record<string, unknown>)[PROVENANCE_STATE_KEY];
    expect(replacedUrl).not.toBe(deepLink);
    expect(persistedProvenance).toBeTruthy();

    TestBed.resetTestingModule();
    configureHarness();
    location.replaceState(replacedUrl, '', persistedState);
    const goSpy = vi.spyOn(location, 'go');

    service.start();
    router.initialNavigation();
    await settle();

    expect(goSpy).not.toHaveBeenCalled();
    expect(location.path()).toBe(replacedUrl);
    expect((location.getState() as Record<string, unknown>)[PROVENANCE_STATE_KEY]).toEqual(
      persistedProvenance,
    );
    expect(service.state().stack).toEqual([replacedFrame]);
  });
});
