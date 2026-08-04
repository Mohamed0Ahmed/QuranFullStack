import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs';
import { NavItem } from '../../navigation/nav-items';
import { NAV_MENU } from '../../navigation/nav-menu';
import { ThemeService } from '../../theme/theme.service';
import { ScrollLockService } from '../../../shared/ui/modal-scroll-lock/scroll-lock.service';

@Component({
  selector: 'qd-top-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './top-navbar.component.html',
  styleUrls: ['./top-navbar.component.scss'],
})
export class TopNavbarComponent {
  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef);
  private readonly themeService = inject(ThemeService);
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly scrollLock = inject(ScrollLockService);
  protected readonly locked = this.scrollLock.isLocked;

  readonly allItems: NavItem[] = NAV_MENU;
  readonly primaryItems = NAV_MENU.filter((i) => i.group === 'primary');
  readonly moreItems = NAV_MENU.filter((i) => i.group === 'more');
  readonly actionItems = NAV_MENU.filter((i) => i.group === 'actions');

  openMenuKey: string | null = null;
  mobileOpen = false;
  private hoveredMenuKey: string | null = null;

  protected readonly isDark = toSignal(this.themeService.isDark$, { initialValue: false });

  protected readonly isAuthenticated = toSignal(
    this.oidcSecurityService.isAuthenticated$.pipe(map((result) => result.isAuthenticated)),
    { initialValue: false },
  );

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.openMenuKey) {
      this.closeMenu(this.openMenuKey);
    }
    if (this.mobileOpen) {
      this.closeMobile();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const el = this.elementRef.nativeElement as HTMLElement;
    const target = event.target as HTMLElement | null;
    if (!target || !this.openMenuKey) {
      return;
    }
    const openLi = el.querySelector(`.nav-dropdown[data-menu-key="${this.openMenuKey}"]`);
    if (!openLi?.contains(target)) {
      this.closeMenu(this.openMenuKey);
    }
  }

  openMenu(key: string): void {
    this.openMenuKey = key;
  }

  closeMenu(key: string): void {
    if (this.openMenuKey !== key) {
      return;
    }
    const trigger = this.menuTrigger(key);
    const focusWasInsideMenu = this.menuHoldsFocus(key);
    this.openMenuKey = null;
    if (focusWasInsideMenu) {
      trigger?.focus();
    }
  }

  toggleMenu(key: string): void {
    if (this.openMenuKey === key) {
      this.closeMenu(key);
      return;
    }
    this.openMenu(key);
  }

  onMenuPointerEnter(key: string): void {
    this.hoveredMenuKey = key;
    this.openMenu(key);
  }

  onMenuPointerLeave(key: string): void {
    if (this.hoveredMenuKey === key) {
      this.hoveredMenuKey = null;
    }
    this.closeMenu(key);
  }

  onTriggerClick(key: string): void {
    if (this.hoveredMenuKey === key && this.openMenuKey === key) {
      this.hoveredMenuKey = null;
      this.openMenu(key);
      return;
    }
    this.toggleMenu(key);
  }

  private menuHost(key: string): HTMLElement | null {
    const host = this.elementRef.nativeElement as HTMLElement;
    return host.querySelector<HTMLElement>(`.nav-dropdown[data-menu-key="${key}"]`);
  }

  private menuTrigger(key: string): HTMLElement | null {
    return this.menuHost(key)?.querySelector<HTMLElement>('button') ?? null;
  }

  private menuHoldsFocus(key: string): boolean {
    const active = document.activeElement;
    return active instanceof HTMLElement && (this.menuHost(key)?.contains(active) ?? false);
  }

  toggleMobile(): void {
    this.mobileOpen = !this.mobileOpen;
    if (!this.mobileOpen) {
      this.openMenuKey = null;
    }
  }

  closeMobile(): void {
    this.mobileOpen = false;
    this.openMenuKey = null;
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  signIn(): void {
    this.oidcSecurityService.authorize();
  }

  signOut(): void {
    this.oidcSecurityService.logoff().subscribe();
  }

  isMoreActive(): boolean {
    return this.moreItems.some((item) =>
      this.router.isActive(item.route, {
        paths: 'exact',
        queryParams: 'ignored',
        fragment: 'ignored',
        matrixParams: 'ignored',
      }),
    );
  }

  isMenuActive(item: NavItem): boolean {
    return this.router.isActive(item.route, {
      paths: 'subset',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored',
    });
  }
}
