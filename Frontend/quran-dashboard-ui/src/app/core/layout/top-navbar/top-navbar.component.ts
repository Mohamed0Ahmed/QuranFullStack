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
    if (this.openMenuKey === key) {
      this.openMenuKey = null;
    }
  }

  toggleMenu(key: string): void {
    this.openMenuKey = this.openMenuKey === key ? null : key;
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
