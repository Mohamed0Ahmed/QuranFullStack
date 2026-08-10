import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';

import { AppShellComponent, QD_MAIN_CONTENT_ID } from './app-shell.component';
import { TopNavbarComponent } from '../top-navbar/top-navbar.component';
import { provideAuthTesting } from '../../auth/auth.testing';
import { QD_BP_WIDE_QUERY } from '../../../shared/layout/breakpoints';

function stubBand(wide: boolean): void {
  vi.stubGlobal('matchMedia', (query: string) => ({
    matches: query === QD_BP_WIDE_QUERY ? wide : !wide,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  }));
}

describe('AppShellComponent', () => {
  let fixture: ComponentFixture<AppShellComponent>;

  function mount(): ComponentFixture<AppShellComponent> {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        provideLocationMocks(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAuthTesting(),
      ],
    });
    const created = TestBed.createComponent(AppShellComponent);
    created.detectChanges();
    return created;
  }

  function root(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function navbar(): TopNavbarComponent {
    return fixture.debugElement.query(By.directive(TopNavbarComponent))
      .componentInstance as TopNavbarComponent;
  }

  beforeEach(() => {
    stubBand(false);
    fixture = mount();
  });

  it('offers a skip link that targets the single main content region', () => {
    const skip = root().querySelector<HTMLAnchorElement>('[data-testid="qd-skip-link"]');
    const mains = root().querySelectorAll('main');

    expect(mains).toHaveLength(1);
    expect(mains[0].id).toBe(QD_MAIN_CONTENT_ID);
    expect(mains[0].getAttribute('tabindex')).toBe('-1');
    expect(skip?.getAttribute('href')).toBe(`#${QD_MAIN_CONTENT_ID}`);
    // The skip link must precede the chrome it skips.
    expect(
      skip!.compareDocumentPosition(mains[0]) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it('keeps the nav-progress, navbar, main, footer order of the shell', () => {
    const order = Array.from(root().querySelectorAll('qd-nav-progress, qd-top-navbar, main, qd-footer')).map(
      (node) => node.tagName.toLowerCase(),
    );

    expect(order).toEqual(['qd-nav-progress', 'qd-top-navbar', 'main', 'qd-footer']);
  });

  it('inerts main and the footer while the navigation sheet is open, and restores them on close', () => {
    const main = root().querySelector('main')!;
    const footer = root().querySelector('qd-footer')!;

    expect(main.hasAttribute('inert')).toBe(false);
    expect(footer.hasAttribute('inert')).toBe(false);

    navbar().openSheet();
    fixture.detectChanges();

    expect(main.getAttribute('inert')).toBe('');
    expect(main.getAttribute('aria-hidden')).toBe('true');
    expect(footer.getAttribute('inert')).toBe('');
    expect(footer.getAttribute('aria-hidden')).toBe('true');

    navbar().closeSheet();
    fixture.detectChanges();

    expect(main.hasAttribute('inert')).toBe(false);
    expect(footer.hasAttribute('inert')).toBe(false);
  });

  it('keeps the open navigation sheet outside every subtree it inerts', () => {
    navbar().openSheet();
    fixture.detectChanges();

    const sheet = root().querySelector('[data-testid="app-navigation-sheet"]')!;
    expect(sheet).not.toBeNull();

    for (const inerted of Array.from(root().querySelectorAll('[inert]'))) {
      expect(inerted.contains(sheet)).toBe(false);
    }
    expect(sheet.closest('[inert]')).toBeNull();
  });
});
