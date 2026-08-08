import { beforeEach, describe, expect, it, vi } from 'vitest';
import { WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

import { TopNavbarComponent } from './top-navbar.component';
import { ScrollLockService } from '../../../shared/ui/modal-scroll-lock/scroll-lock.service';
import { AuthReturnLocationStore } from '../../auth/auth-return-location.store';
import { CurrentUserStore } from '../../auth/current-user.store';
import { ThemeService } from '../../theme/theme.service';

const DROPDOWN_KEYS = ['words', 'abwab', 'more', 'settings'] as const;

interface CurrentUserStoreMock {
  clear: ReturnType<typeof vi.fn>;
  authStateKnown: WritableSignal<boolean>;
  isActive: WritableSignal<boolean>;
  isOwner: WritableSignal<boolean>;
}

describe('TopNavbarComponent', () => {
  let currentUserStore: CurrentUserStoreMock;

  beforeEach(() => {
    currentUserStore = {
      clear: vi.fn(),
      authStateKnown: signal(true),
      isActive: signal(true),
      isOwner: signal(true),
    };
    TestBed.configureTestingModule({
      imports: [TopNavbarComponent],
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        provideLocationMocks(),
        { provide: ThemeService, useValue: { isDark$: of(false), toggle: vi.fn() } },
        { provide: CurrentUserStore, useValue: currentUserStore },
        { provide: AuthReturnLocationStore, useValue: { clear: vi.fn() } },
        {
          provide: OidcSecurityService,
          useValue: {
            isAuthenticated$: of({ isAuthenticated: false }),
            authorize: vi.fn(),
            logoff: vi.fn(() => of(null)),
          },
        },
      ],
    });
  });

  function mount(): ComponentFixture<TopNavbarComponent> {
    const fixture = TestBed.createComponent(TopNavbarComponent);
    fixture.detectChanges();
    return fixture;
  }

  function host(fixture: ComponentFixture<TopNavbarComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function dropdown(fixture: ComponentFixture<TopNavbarComponent>, key: string): HTMLElement {
    const li = host(fixture).querySelector<HTMLElement>(`.nav-dropdown[data-menu-key="${key}"]`);
    expect(li).not.toBeNull();
    return li as HTMLElement;
  }

  function trigger(fixture: ComponentFixture<TopNavbarComponent>, key: string): HTMLButtonElement {
    const button = host(fixture).querySelector<HTMLButtonElement>(
      `[data-testid="nav-${key}-trigger"]`,
    );
    expect(button).not.toBeNull();
    return button as HTMLButtonElement;
  }

  function menu(fixture: ComponentFixture<TopNavbarComponent>, key: string): HTMLElement | null {
    return host(fixture).querySelector<HTMLElement>(`#${key}-menu`);
  }

  function firstMenuLink(
    fixture: ComponentFixture<TopNavbarComponent>,
    key: string,
  ): HTMLAnchorElement {
    const link = menu(fixture, key)?.querySelector<HTMLAnchorElement>('a.dropdown-link');
    expect(link).not.toBeNull();
    return link as HTMLAnchorElement;
  }

  function pointerEnter(fixture: ComponentFixture<TopNavbarComponent>, key: string): void {
    dropdown(fixture, key).dispatchEvent(new MouseEvent('mouseenter'));
    fixture.detectChanges();
  }

  function openByTriggerClick(fixture: ComponentFixture<TopNavbarComponent>, key: string): void {
    trigger(fixture, key).focus();
    trigger(fixture, key).click();
    fixture.detectChanges();
  }

  function addOutsideButton(): HTMLButtonElement {
    const outside = document.createElement('button');
    outside.textContent = 'خارج القائمة';
    document.body.appendChild(outside);
    return outside;
  }

  it.each(DROPDOWN_KEYS)(
    'opens the %s dropdown when the trigger is clicked after the pointer already entered the item',
    (key) => {
      const fixture = mount();

      pointerEnter(fixture, key);
      trigger(fixture, key).click();
      fixture.detectChanges();

      expect(trigger(fixture, key).getAttribute('aria-expanded')).toBe('true');
      expect(menu(fixture, key)).not.toBeNull();
    },
  );

  it.each(DROPDOWN_KEYS)(
    'still closes the %s dropdown on the next trigger click after a hover-then-click open',
    (key) => {
      const fixture = mount();

      pointerEnter(fixture, key);
      trigger(fixture, key).click();
      fixture.detectChanges();
      trigger(fixture, key).click();
      fixture.detectChanges();

      expect(trigger(fixture, key).getAttribute('aria-expanded')).toBe('false');
      expect(menu(fixture, key)).toBeNull();
    },
  );

  it.each(DROPDOWN_KEYS)('opens the %s dropdown on a trigger click with no pointer at all', (key) => {
    const fixture = mount();

    trigger(fixture, key).click();
    fixture.detectChanges();

    expect(trigger(fixture, key).getAttribute('aria-expanded')).toBe('true');
  });

  it.each(DROPDOWN_KEYS)(
    'returns focus to the %s trigger when Escape closes the menu from inside it',
    (key) => {
      const fixture = mount();
      openByTriggerClick(fixture, key);
      const link = firstMenuLink(fixture, key);
      link.focus();
      expect(document.activeElement).toBe(link);

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();

      expect(menu(fixture, key)).toBeNull();
      expect(document.activeElement).toBe(trigger(fixture, key));
    },
  );

  it.each(DROPDOWN_KEYS)(
    'returns focus to the %s trigger when the menu closes while focus is still inside it (real browsers usually move focus out before the click handler runs; this pins the still-inside branch)',
    (key) => {
      const fixture = mount();
      const outside = addOutsideButton();
      openByTriggerClick(fixture, key);
      const link = firstMenuLink(fixture, key);
      link.focus();

      outside.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();

      expect(menu(fixture, key)).toBeNull();
      expect(document.activeElement).toBe(trigger(fixture, key));
      outside.remove();
    },
  );

  it.each(DROPDOWN_KEYS)(
    'returns focus to the %s trigger when activating a menu link closes the menu',
    (key) => {
      const fixture = mount();
      openByTriggerClick(fixture, key);
      const link = firstMenuLink(fixture, key);
      link.focus();

      link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
      fixture.detectChanges();

      expect(menu(fixture, key)).toBeNull();
      expect(document.activeElement).toBe(trigger(fixture, key));
    },
  );

  it('leaves focus where it is when a menu the user never focused is closed by an outside click', () => {
    const fixture = mount();
    const outside = addOutsideButton();

    pointerEnter(fixture, 'words');
    outside.focus();
    outside.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(menu(fixture, 'words')).toBeNull();
    expect(document.activeElement).toBe(outside);
    outside.remove();
  });

  it('shows the settings entry to an active Owner in the desktop actions and the mobile panel', () => {
    const fixture = mount();

    openByTriggerClick(fixture, 'settings');
    const accessLink = menu(fixture, 'settings')?.querySelector<HTMLAnchorElement>(
      '[data-testid="nav-menu-link--settings-access"]',
    );
    expect(accessLink?.getAttribute('href')).toBe('/settings/access');

    host(fixture).querySelector<HTMLButtonElement>('.menu-toggle')?.click();
    fixture.detectChanges();

    expect(
      host(fixture).querySelector('.mobile-menu-panel a[href="/settings/access"]'),
    ).not.toBeNull();
    expect(host(fixture).querySelector('.mobile-menu-panel a[href="/settings"]')).toBeNull();
    expect(
      host(fixture).querySelector('.mobile-menu-panel .mobile-group-label')?.textContent,
    ).toContain('الإعدادات');
  });

  const hiddenSettingsStates: ReadonlyArray<[string, (store: CurrentUserStoreMock) => void]> = [
    ['the auth state is not yet known', (store) => store.authStateKnown.set(false)],
    ['the user is not active', (store) => store.isActive.set(false)],
    ['the user is not an Owner', (store) => store.isOwner.set(false)],
  ];

  it.each(hiddenSettingsStates)('hides the settings entry when %s', (_state, arrange) => {
    arrange(currentUserStore);
    const fixture = mount();

    expect(host(fixture).querySelector('[data-testid="nav-settings-trigger"]')).toBeNull();

    host(fixture).querySelector<HTMLButtonElement>('.menu-toggle')?.click();
    fixture.detectChanges();

    expect(host(fixture).querySelector('.mobile-menu-panel a[href="/settings/access"]')).toBeNull();
    expect(host(fixture).querySelector('.mobile-menu-panel a[href="/settings"]')).toBeNull();
  });

  it('shows and hides the settings entry as the auth signals change after mount', () => {
    currentUserStore.authStateKnown.set(false);
    const fixture = mount();
    host(fixture).querySelector<HTMLButtonElement>('.menu-toggle')?.click();
    fixture.detectChanges();

    expect(host(fixture).querySelector('[data-testid="nav-settings-trigger"]')).toBeNull();
    expect(host(fixture).querySelector('.mobile-menu-panel a[href="/settings/access"]')).toBeNull();

    currentUserStore.authStateKnown.set(true);
    currentUserStore.isActive.set(true);
    currentUserStore.isOwner.set(true);
    fixture.detectChanges();

    expect(host(fixture).querySelector('[data-testid="nav-settings-trigger"]')).not.toBeNull();
    expect(
      host(fixture).querySelector('.mobile-menu-panel a[href="/settings/access"]'),
    ).not.toBeNull();

    currentUserStore.authStateKnown.set(false);
    currentUserStore.isActive.set(false);
    currentUserStore.isOwner.set(false);
    fixture.detectChanges();

    expect(host(fixture).querySelector('[data-testid="nav-settings-trigger"]')).toBeNull();
    expect(host(fixture).querySelector('.mobile-menu-panel a[href="/settings/access"]')).toBeNull();
  });

  it('inerts the mobile menu overlay while a modal holds the scroll lock', () => {
    const fixture = mount();
    const scrollLock = TestBed.inject(ScrollLockService);
    host(fixture).querySelector<HTMLButtonElement>('.menu-toggle')?.click();
    fixture.detectChanges();

    expect(host(fixture).querySelector('.mobile-menu')?.hasAttribute('inert')).toBe(false);

    scrollLock.acquire();
    fixture.detectChanges();

    const overlay = host(fixture).querySelector('.mobile-menu');
    expect(overlay?.hasAttribute('inert')).toBe(true);
    expect(overlay?.getAttribute('aria-hidden')).toBe('true');

    scrollLock.release();
  });
});
