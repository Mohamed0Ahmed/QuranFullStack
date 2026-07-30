import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs';
import { NAV_ITEMS, NavItem } from '../../navigation/nav-items';
import { WORDS_ROUTE_PATH } from '../../navigation/route-paths';
import { WORDS_MENU_ITEMS } from '../../navigation/words-nav-items';
import { ThemeService } from '../../theme/theme.service';
import { ScrollLockService } from '../../../shared/ui/modal-scroll-lock/scroll-lock.service';

// The navbar goes inert while any modal dialog holds the scroll lock (Slice B2, T904 —
// UI_STYLE_SYSTEM.md §17 "Chrome-inert rule"). Sticky chrome (T901) would otherwise stay
// keyboard-reachable under a dialog it renders alongside (nine surfaces today: four abwab
// modals + five words drawers/dialogs), which is `app.ts:14`'s shell-inert precedent applied
// at the one level that does not also inert the dialog itself.
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

  readonly allItems: NavItem[] = NAV_ITEMS;
  readonly primaryItems = NAV_ITEMS.filter((i) => i.group === 'primary');
  readonly moreItems = NAV_ITEMS.filter((i) => i.group === 'more');
  readonly actionItems = NAV_ITEMS.filter((i) => i.group === 'actions');

  // The primary "words" item renders as a dropdown of the Words-section pages.
  readonly wordsMenuItems = WORDS_MENU_ITEMS;
  readonly wordsHubRoute = WORDS_ROUTE_PATH;

  moreOpen = false;
  wordsOpen = false;
  mobileOpen = false;

  protected readonly isDark = toSignal(this.themeService.isDark$, { initialValue: false });

  // Drives the sign-in ⇄ sign-out swap in the actions area (Feature 033).
  protected readonly isAuthenticated = toSignal(
    this.oidcSecurityService.isAuthenticated$.pipe(map((result) => result.isAuthenticated)),
    { initialValue: false },
  );

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.moreOpen) {
      this.closeMore();
    }
    if (this.wordsOpen) {
      this.closeWords();
    }
    if (this.mobileOpen) {
      this.closeMobile();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const el = this.elementRef.nativeElement as HTMLElement;
    const target = event.target as HTMLElement | null;
    if (!target) {
      return;
    }
    if (this.moreOpen && !el.querySelector('.more-dropdown')?.contains(target)) {
      this.closeMore();
    }
    if (this.wordsOpen && !el.querySelector('.words-dropdown')?.contains(target)) {
      this.closeWords();
    }
  }

  toggleMore(): void {
    this.moreOpen = !this.moreOpen;
    this.wordsOpen = false;
  }

  closeMore(): void {
    this.moreOpen = false;
  }

  toggleWords(): void {
    this.wordsOpen = !this.wordsOpen;
    this.moreOpen = false;
  }

  openWords(): void {
    this.wordsOpen = true;
    this.moreOpen = false;
  }

  closeWords(): void {
    this.wordsOpen = false;
  }

  toggleMobile(): void {
    this.mobileOpen = !this.mobileOpen;
    if (!this.mobileOpen) {
      this.moreOpen = false;
      this.wordsOpen = false;
    }
  }

  closeMobile(): void {
    this.mobileOpen = false;
    this.moreOpen = false;
    this.wordsOpen = false;
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  signIn(): void {
    this.oidcSecurityService.authorize();
  }

  signOut(): void {
    // postLogoutRedirectUri is configured in app.config.ts; logoff() redirects there.
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

  isWordsActive(): boolean {
    return this.router.isActive(this.wordsHubRoute, {
      paths: 'subset',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored',
    });
  }
}
