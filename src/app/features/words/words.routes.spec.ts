import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router } from '@angular/router';
import { RouterOutlet } from '@angular/router';
import { Component } from '@angular/core';

import { WORDS_ROUTES } from './words.routes';
import { NAV_ITEMS } from '../../core/navigation/nav-items';

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

    const wildcard = WORDS_ROUTES.find((r) => r.path === '**');
    expect(wildcard).toBeUndefined();
  });

  it('registers the lemmas and stems explorer child routes', () => {
    expect(WORDS_ROUTES.some((r) => r.path === 'lemmas')).toBe(true);
    expect(WORDS_ROUTES.some((r) => r.path === 'stems')).toBe(true);
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
