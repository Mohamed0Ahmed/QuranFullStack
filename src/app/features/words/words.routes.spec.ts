import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router } from '@angular/router';
import { RouterOutlet } from '@angular/router';
import { Component } from '@angular/core';

import { WORDS_ROUTES } from './words.routes';
import { NAV_ITEMS } from '../../core/navigation/nav-items';

/**
 * Routes test for the Words feature.
 *
 * The app mounts `WORDS_ROUTES` under `/dashboard/words`. Rather than drive
 * real navigation through `RouterTestingHarness` (which creates a persistent
 * root outlet that can leak into sibling specs sharing the test fork), we
 * render a tiny host with `RouterOutlet` and assert on the resolved route
 * component plus the route configuration. Each test resets the shared
 * injector and tears down after itself.
 */
@Component({
  selector: 'qd-test-host',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
class TestHostComponent {}

function mountRoutes() {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    imports: [TestHostComponent],
    providers: [
      provideRouter([{ path: 'dashboard/words', loadChildren: () => WORDS_ROUTES }]),
    ],
    teardown: { destroyAfterEach: true },
  });
}

describe('WORDS_ROUTES hub route', () => {
  it('renders the words hub page at `/dashboard/words`', async () => {
    mountRoutes();
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/dashboard/words');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('qd-words-hub-page')).toBeTruthy();
  });

  it('redirects `unique` to `unique/tashkeel` as the default mode (config)', () => {
    const uniqueRedirect = WORDS_ROUTES.find((r) => r.path === 'unique');
    expect(uniqueRedirect).toBeTruthy();
    expect(uniqueRedirect?.redirectTo).toBe('unique/tashkeel');
    expect(uniqueRedirect?.pathMatch).toBe('full');
  });

  it('does not register a wildcard route that could swallow the words area', () => {
    // The words area must own its own routes; no catch-all lives inside it.
    const wildcard = WORDS_ROUTES.find((r) => r.path === '**');
    expect(wildcard).toBeUndefined();
  });
});

describe('WORDS_ROUTES unique mode segment', () => {
  function deepestSnapshot(root: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    let node = root;
    while (node.firstChild) {
      node = node.firstChild;
    }
    return node;
  }

  function mountForNavigation() {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'dashboard/words', loadChildren: () => WORDS_ROUTES }]),
      ],
      teardown: { destroyAfterEach: true },
    });
  }

  // Regression: the explorer reads the active mode from `paramMap.get('mode')`,
  // so the route must expose a `:mode` segment. Literal `unique/tashkeel` /
  // `unique/simple` routes would leave `mode` null and silently fall back to
  // tashkeel, making the simple mode unreachable.
  it('resolves mode="tashkeel" for /dashboard/words/unique/tashkeel', async () => {
    mountForNavigation();
    const router = TestBed.inject(Router);

    const navigated = await router.navigateByUrl('/dashboard/words/unique/tashkeel');

    expect(navigated).toBe(true);
    expect(deepestSnapshot(router.routerState.snapshot.root).paramMap.get('mode')).toBe('tashkeel');
  });

  it('resolves mode="simple" for /dashboard/words/unique/simple', async () => {
    mountForNavigation();
    const router = TestBed.inject(Router);

    const navigated = await router.navigateByUrl('/dashboard/words/unique/simple');

    expect(navigated).toBe(true);
    expect(deepestSnapshot(router.routerState.snapshot.root).paramMap.get('mode')).toBe('simple');
  });
});

describe('words fallback-route exclusion', () => {
  it('excludes the words nav item from placeholder fallback routes', () => {
    // The placeholder fallback is generated for nav items that are neither
    // dashboard, mushaf, nor words. After this feature, `words` is a real
    // routeable area and must never fall through to the placeholder page.
    const placeholderKeys = NAV_ITEMS.filter(
      (item) => item.key !== 'dashboard' && item.key !== 'mushaf' && item.key !== 'words',
    ).map((item) => item.key);

    expect(placeholderKeys).not.toContain('words');
  });

  it('points the words nav item at the dashboard words hub', () => {
    const words = NAV_ITEMS.find((item) => item.key === 'words');
    expect(words?.route).toBe('/dashboard/words');
  });
});
