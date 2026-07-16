import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailFrame, RootDetailFrame, LemmaDetailFrame, StemDetailFrame } from './detail-overlay.models';

@Component({ standalone: true, template: '' })
class BlankPageComponent {}

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
const stemFrame: StemDetailFrame = {
  kind: 'stem',
  id: 77,
  view: 'words',
  wordView: 'simple',
  surahView: 'mentioned',
  detailPage: 1,
  typeCode: null,
};

const ROOT_SERIALIZED = 'v1~root~999~words~simple~mentioned~1';
const LEMMA_SERIALIZED = 'v1~lemma~555~ayahs~simple~mentioned~1~-';

describe('DetailOverlayHistoryService', () => {
  let router: Router;
  let location: Location;
  let service: DetailOverlayHistoryService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'dashboard/words/roots', component: BlankPageComponent },
          { path: 'dashboard/mushaf', component: BlankPageComponent },
          { path: '**', component: BlankPageComponent },
        ]),
        provideLocationMocks(),
      ],
    });
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    service = TestBed.inject(DetailOverlayHistoryService);
    sessionStorage.clear();
    // Wire the router's popstate listener so SpyLocation back/forward drive navigations.
    router.initialNavigation();
  });

  async function settle(): Promise<void> {
    // The router schedules popstate-driven navigations in a setTimeout(0);
    // yield a macrotask first so an about-to-start navigation is observable.
    await new Promise((resolve) => setTimeout(resolve, 10));
    await vi.waitFor(() => {
      if (router.getCurrentNavigation() !== null) {
        throw new Error('navigation in flight');
      }
    });
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  async function startAt(url: string): Promise<void> {
    await router.navigateByUrl(url);
    service.start();
    await settle();
  }

  it('parses no overlay on a bare explorer URL', async () => {
    await startAt('/dashboard/words/roots?root=5');

    expect(service.state().visibility).toBe('closed');
    expect(service.state().stack).toHaveLength(0);
    expect(service.isOpen()).toBe(false);
  });

  it('first entity open pushes one frame and preserves existing base query state', async () => {
    await startAt('/dashboard/words/roots?root=5');

    service.startStack(rootFrame);
    await settle();

    expect(location.path()).toContain('root=5');
    expect(location.path()).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(location.path()).toContain('qdDetailOpen=1');
    expect(service.isOpen()).toBe(true);
    expect(service.state().stack).toHaveLength(1);
  });

  it('browser Back after the first open returns to the same base with no overlay', async () => {
    await startAt('/dashboard/words/roots?root=5');

    service.startStack(rootFrame);
    await settle();
    location.back();
    await settle();

    expect(location.path()).toBe('/dashboard/words/roots?root=5');
    expect(service.state().stack).toHaveLength(0);
  });

  it('cross-entity append pushes a second frame; identical-top append is a no-op', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();

    expect(service.appendFrame(lemmaFrame)).toBe(true);
    await settle();
    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['root', 'lemma']);

    const urlBefore = location.path();
    expect(service.appendFrame({ ...lemmaFrame })).toBe(false);
    await settle();
    expect(location.path()).toBe(urlBefore);
  });

  it('refuses a ninth append, leaves state untouched, and counts the rejection', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();
    for (let id = 1; id <= 7; id += 1) {
      service.appendFrame({ ...lemmaFrame, id });
      await settle();
    }
    expect(service.state().stack).toHaveLength(8);

    const urlBefore = location.path();
    expect(service.appendFrame(stemFrame)).toBe(false);
    await settle();

    expect(service.state().stack).toHaveLength(8);
    expect(location.path()).toBe(urlBefore);
    expect(service.capRejectionCount()).toBe(1);
  });

  it('top-frame replacement rewrites the current entry without adding history', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();

    service.replaceTopFrame({ ...rootFrame, view: 'ayahs', detailPage: 2 });
    await settle();

    expect(service.state().stack).toHaveLength(1);
    expect((service.state().stack[0] as RootDetailFrame).view).toBe('ayahs');

    // The replaced entry still proves its parent: dialog Back uses browser Back.
    service.appendFrame(lemmaFrame);
    await settle();
    const backSpy = vi.spyOn(location, 'back');
    service.back();
    await settle();
    expect(backSpy).toHaveBeenCalledTimes(1);
    expect((service.state().stack.at(-1) as RootDetailFrame).view).toBe('ayahs');
  });

  it('dialog Back uses browser Back when provenance proves the parent entry', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();
    service.appendFrame(lemmaFrame);
    await settle();

    const backSpy = vi.spyOn(location, 'back');
    service.back();
    await settle();

    expect(backSpy).toHaveBeenCalledTimes(1);
    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['root']);
    expect(service.isOpen()).toBe(true);
  });

  it('close retains the stack in the URL and restore reopens it as a push', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();
    service.appendFrame(lemmaFrame);
    await settle();

    service.close();
    await settle();
    expect(service.isRetainedClosed()).toBe(true);
    expect(location.path()).toContain('qdDetail=');
    expect(location.path()).not.toContain('qdDetailOpen');

    service.restore();
    await settle();
    expect(service.isOpen()).toBe(true);
    expect(service.state().stack).toHaveLength(2);

    // Browser Back returns to the closed/restorable state.
    location.back();
    await settle();
    expect(service.isRetainedClosed()).toBe(true);
  });

  it('dialog Back after restore falls back to a deterministic replace (no browser Back)', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();
    service.appendFrame(lemmaFrame);
    await settle();
    service.close();
    await settle();
    service.restore();
    await settle();

    const backSpy = vi.spyOn(location, 'back');
    service.back();
    await settle();

    expect(backSpy).not.toHaveBeenCalled();
    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['root']);
    expect(service.isOpen()).toBe(true);
  });

  it('a new entity click while closed starts a new one-frame stack', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();
    service.close();
    await settle();

    service.startStack(stemFrame);
    await settle();

    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['stem']);
    expect(service.isOpen()).toBe(true);
  });

  it('browser Back/Forward converge on the same parsed state machine', async () => {
    await startAt('/dashboard/words/roots');
    service.startStack(rootFrame);
    await settle();
    service.appendFrame(lemmaFrame);
    await settle();

    location.back();
    await settle();
    expect(service.state().stack).toHaveLength(1);

    location.forward();
    await settle();
    expect(service.state().stack).toHaveLength(2);
    expect(service.state().stack[1].kind).toBe('lemma');
  });

  it('seeds prefix history entries once for a fresh deep link', async () => {
    const deepLink =
      '/dashboard/words/roots?qdDetail=' +
      encodeURIComponent(ROOT_SERIALIZED) +
      '&qdDetail=' +
      encodeURIComponent(LEMMA_SERIALIZED) +
      '&qdDetailOpen=1';
    await router.navigateByUrl(deepLink);
    const goSpy = vi.spyOn(location, 'go');
    service.start();
    await settle();

    // One Location.go per stack prefix (depth 1 and depth 2), ending at the original URL.
    const seededUrls = goSpy.mock.calls.map(([url]) => url);
    expect(seededUrls).toHaveLength(2);
    expect(seededUrls[0]).toContain(encodeURIComponent(ROOT_SERIALIZED));
    expect(seededUrls[0]).not.toContain(encodeURIComponent(LEMMA_SERIALIZED));
    expect(seededUrls[1]).toBe(location.path());
    expect(service.state().stack).toHaveLength(2);

    // Browser Back now pops the deep-linked stack instead of leaving the app.
    location.back();
    await settle();
    expect(service.state().stack).toHaveLength(1);
    expect(service.isOpen()).toBe(true);
  });

  it('does not reseed on reload even after the router rewrites history.state (reload idempotence)', async () => {
    const deepLink = '/dashboard/words/roots?qdDetail=' + encodeURIComponent(ROOT_SERIALIZED) + '&qdDetailOpen=1';
    await startAt(deepLink);

    // Simulate a reload: the initial navigation clobbers our history.state marker.
    location.replaceState(location.path());
    const goSpy = vi.spyOn(location, 'go');
    await router.navigateByUrl(location.path(), { onSameUrlNavigation: 'reload' });
    await settle();

    expect(goSpy).not.toHaveBeenCalled();
    // The provenance marker is restored so dialog Back proves its parent again.
    const state = location.getState() as Record<string, unknown>;
    expect(state['qdDetailNav']).toBeTruthy();
  });

  it('canonicalizes corrupted overlay params once with replace semantics', async () => {
    await startAt('/dashboard/words/roots?qdDetail=v1~garbage&qdDetailOpen=1');

    expect(service.state().stack).toHaveLength(0);
    expect(location.path()).toBe('/dashboard/words/roots');
  });

  it('canonicalizes a malformed later frame by truncating the stack before it', async () => {
    await startAt(
      '/dashboard/words/roots?qdDetail=' + encodeURIComponent(ROOT_SERIALIZED) + '&qdDetail=v1~broken&qdDetailOpen=1',
    );

    expect(service.state().stack.map((frame) => frame.kind)).toEqual(['root']);
    expect(location.path()).not.toContain('broken');
    expect(location.path()).toContain('qdDetailOpen=1');
  });

  it('builds start and append hrefs that retain the current base route and query', async () => {
    await startAt('/dashboard/words/roots?root=5');

    const startHref = service.buildFrameHref(rootFrame, 'start');
    expect(startHref).toContain('/dashboard/words/roots');
    expect(startHref).toContain('root=5');
    expect(startHref).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(startHref).toContain('qdDetailOpen=1');

    service.startStack(rootFrame);
    await settle();
    const appendHref = service.buildFrameHref(lemmaFrame, 'append');
    expect(appendHref).toContain(`qdDetail=${encodeURIComponent(ROOT_SERIALIZED)}`);
    expect(appendHref).toContain(`qdDetail=${encodeURIComponent(LEMMA_SERIALIZED)}`);
  });

  it('exposes frames of a shared closed stack without opening the dialog', async () => {
    await startAt('/dashboard/words/roots?qdDetail=' + encodeURIComponent(ROOT_SERIALIZED));

    expect(service.isOpen()).toBe(false);
    expect(service.isRetainedClosed()).toBe(true);
    expect(service.state().stack).toHaveLength(1);
  });
});

type _FrameCheck = DetailFrame;
