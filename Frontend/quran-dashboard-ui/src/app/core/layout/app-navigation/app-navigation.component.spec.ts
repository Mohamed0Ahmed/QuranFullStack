import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { AppNavigationComponent, QdAppNavigationMode } from './app-navigation.component';
import { NavItem } from '../../navigation/nav-items';
import { NAV_MENU } from '../../navigation/nav-menu';

const WORDS = NAV_MENU.find((item) => item.key === 'words') as NavItem;
const SETTINGS = NAV_MENU.find((item) => item.key === 'settings') as NavItem;
const DASHBOARD = NAV_MENU.find((item) => item.key === 'dashboard') as NavItem;

function render(
  mode: QdAppNavigationMode,
  items: readonly NavItem[],
  openMenuKey: string | null = null,
): ComponentFixture<AppNavigationComponent> {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [AppNavigationComponent],
    providers: [provideRouter([{ path: '**', children: [] }]), provideLocationMocks()],
  });

  const fixture = TestBed.createComponent(AppNavigationComponent);
  fixture.componentRef.setInput('mode', mode);
  fixture.componentRef.setInput('items', items);
  fixture.componentRef.setInput('openMenuKey', openMenuKey);
  fixture.detectChanges();
  return fixture;
}

function host(fixture: ComponentFixture<AppNavigationComponent>): HTMLElement {
  return fixture.nativeElement as HTMLElement;
}

function hrefs(fixture: ComponentFixture<AppNavigationComponent>): string[] {
  return Array.from(host(fixture).querySelectorAll<HTMLAnchorElement>('a[href]')).map(
    (link) => link.getAttribute('href') ?? '',
  );
}

describe('AppNavigationComponent', () => {
  it('renders the same NAV_MENU routes in desktop and sheet mode', () => {
    const items = [DASHBOARD, WORDS];

    const desktop = render('desktop', items, 'words');
    const sheet = render('sheet', items);

    // Desktop reaches the children through the open dropdown; the sheet expands them inline.
    // Either way the reachable route set is one tree, not two hand-maintained ones.
    expect(new Set(hrefs(desktop))).toEqual(new Set(hrefs(sheet)));
    expect(hrefs(sheet)).toContain('/dashboard');
    expect(hrefs(sheet).length).toBe(1 + 1 + (WORDS.children?.length ?? 0));
  });

  it('exposes a dropdown trigger per parent in desktop mode and mounts its menu only when open', () => {
    const closed = render('desktop', [WORDS]);
    const trigger = host(closed).querySelector('[data-testid="nav-words-trigger"]');

    expect(trigger?.getAttribute('aria-expanded')).toBe('false');
    // The menu element only exists while open, so a closed trigger must not name it: an
    // aria-controls pointing at a missing id is a broken reference, not a hint.
    expect(trigger?.hasAttribute('aria-controls')).toBe(false);
    expect(host(closed).querySelector('#words-menu')).toBeNull();

    const open = render('desktop', [WORDS], 'words');
    const openTrigger = host(open).querySelector('[data-testid="nav-words-trigger"]');
    expect(openTrigger?.getAttribute('aria-expanded')).toBe('true');
    expect(openTrigger?.getAttribute('aria-controls')).toBe('words-menu');
    expect(host(open).querySelector('#words-menu')).not.toBeNull();
  });

  it('renders no dropdown trigger and no menu id in sheet mode', () => {
    const fixture = render('sheet', [WORDS, SETTINGS], 'words');

    expect(host(fixture).querySelector('[data-testid="nav-words-trigger"]')).toBeNull();
    expect(host(fixture).querySelector('#words-menu')).toBeNull();
    expect(host(fixture).querySelectorAll('.qd-nav__sublist')).toHaveLength(2);
  });

  it('keeps an auth-gated group parent a non-navigable label in sheet mode', () => {
    const fixture = render('sheet', [SETTINGS]);

    expect(host(fixture).querySelector('.qd-nav__group-label')?.textContent?.trim()).toBe(
      SETTINGS.labelAr,
    );
    expect(host(fixture).querySelector(`a[href="${SETTINGS.route}"]`)).toBeNull();
    expect(host(fixture).querySelector('a[href="/settings/access"]')).not.toBeNull();
  });

  it('carries the query-param nav entry through to its href', () => {
    const fixture = render('sheet', [NAV_MENU.find((item) => item.key === 'abwab') as NavItem]);

    expect(hrefs(fixture)).toContain('/abwab?archive=1');
  });

  it('emits pointer and click intent for menu parents only', () => {
    const fixture = render('desktop', [DASHBOARD, WORDS]);
    const entered: string[] = [];
    const toggled: string[] = [];
    fixture.componentInstance.menuPointerEntered.subscribe((key) => entered.push(key));
    fixture.componentInstance.menuToggled.subscribe((key) => toggled.push(key));

    const items = host(fixture).querySelectorAll<HTMLElement>('.qd-nav__item');
    items[0].dispatchEvent(new MouseEvent('mouseenter'));
    items[1].dispatchEvent(new MouseEvent('mouseenter'));
    host(fixture).querySelector<HTMLButtonElement>('[data-testid="nav-words-trigger"]')?.click();

    expect(entered).toEqual(['words']);
    expect(toggled).toEqual(['words']);
  });

  it('reports a sheet child activation as navigation and a desktop child activation as a menu close', () => {
    const sheet = render('sheet', [WORDS]);
    let navigated = 0;
    sheet.componentInstance.navigated.subscribe(() => (navigated += 1));
    sheet.debugElement.nativeElement
      .querySelector('.qd-nav__sublist a')
      .dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
    expect(navigated).toBe(1);

    const desktop = render('desktop', [WORDS], 'words');
    const closed: string[] = [];
    desktop.componentInstance.menuLinkActivated.subscribe((key) => closed.push(key));
    host(desktop)
      .querySelector<HTMLAnchorElement>('#words-menu a')
      ?.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
    expect(closed).toEqual(['words']);
  });

  it('gives every rendered link and trigger the shared action geometry contract', () => {
    const fixture = render('sheet', [DASHBOARD, WORDS]);

    const interactive = Array.from(
      host(fixture).querySelectorAll<HTMLElement>('.qd-nav__link'),
    );
    expect(interactive.length).toBeGreaterThan(0);
    expect(interactive.every((node) => node.classList.contains('qd-action'))).toBe(true);
  });
});
