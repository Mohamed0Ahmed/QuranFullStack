import { NgTemplateOutlet } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  Injector,
  OnDestroy,
  OnInit,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs';

import { AppNavigationComponent, NAV_GROUP_ONLY_ROUTE } from '../app-navigation/app-navigation.component';
import { NavItem } from '../../navigation/nav-items';
import { NAV_MENU } from '../../navigation/nav-menu';
import { ThemeService } from '../../theme/theme.service';
import { QdActionDirective } from '../../../shared/ui/action/action.directive';
import { QdModalShellComponent } from '../../../shared/ui/modal-shell/modal-shell.component';
import { ScrollLockService } from '../../../shared/ui/modal-scroll-lock/scroll-lock.service';
import { QD_BP_WIDE_QUERY } from '../../../shared/layout/breakpoints';
import { AuthReturnLocationStore } from '../../auth/auth-return-location.store';
import { CurrentUserStore } from '../../auth/current-user.store';
import { LinkingAccessService } from '../../../features/linking/state/linking-access.service';
import { LinkingWorkspaceStore } from '../../../features/linking/state/linking-workspace.store';

const MORE_MENU_ITEM: NavItem = {
  key: 'more',
  labelAr: 'المزيد',
  labelEn: 'More',
  route: NAV_GROUP_ONLY_ROUTE,
  group: 'primary',
  children: NAV_MENU.filter((item) => item.group === 'more'),
};

@Component({
  selector: 'qd-top-navbar',
  standalone: true,
  imports: [
    NgTemplateOutlet,
    RouterLink,
    AppNavigationComponent,
    QdActionDirective,
    QdModalShellComponent,
  ],
  templateUrl: './top-navbar.component.html',
  styleUrls: ['./top-navbar.component.scss'],
})
export class TopNavbarComponent implements OnInit, OnDestroy {
  private readonly elementRef = inject(ElementRef);
  private readonly injector = inject(Injector);
  private readonly themeService = inject(ThemeService);
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly authReturnLocationStore = inject(AuthReturnLocationStore);
  private readonly scrollLock = inject(ScrollLockService);
  private readonly linkingAccess = inject(LinkingAccessService);
  private readonly linkingWorkspace = inject(LinkingWorkspaceStore);

  readonly sheetOpen = signal(false);

  protected readonly locked = this.scrollLock.isLocked;
  protected readonly toggleInert = computed(() => this.locked() && !this.sheetOpen());
  protected readonly openMenuKey = signal<string | null>(null);
  protected readonly wide = signal(true);
  protected readonly canUseLinking = this.linkingAccess.canUseLinking;
  protected readonly linkingItemCount = this.linkingWorkspace.itemCount;
  protected readonly linkingWorkspaceAriaLabel = computed(() => {
    const count = this.linkingItemCount();
    return count === 0 ? 'مساحة الربط' : `مساحة الربط، ${count} مصادر`;
  });

  private readonly isActiveOwner = computed(
    () =>
      this.currentUserStore.authStateKnown() &&
      this.currentUserStore.isActive() &&
      this.currentUserStore.isOwner(),
  );

  readonly allItems = computed(() => NAV_MENU.filter((item) => this.isItemVisible(item)));
  readonly desktopItems: NavItem[] = [
    ...NAV_MENU.filter((i) => i.group === 'primary'),
    MORE_MENU_ITEM,
  ];
  readonly actionItems = computed(() =>
    NAV_MENU.filter((item) => item.group === 'actions' && this.isItemVisible(item)),
  );

  private hoveredMenuKey: string | null = null;
  private wideQuery?: MediaQueryList;
  private readonly onWideChange = (event: MediaQueryListEvent): void =>
    this.applyWide(event.matches);

  protected readonly isDark = toSignal(this.themeService.isDark$, { initialValue: false });

  protected readonly isAuthenticated = toSignal(
    this.oidcSecurityService.isAuthenticated$.pipe(map((result) => result.isAuthenticated)),
    { initialValue: false },
  );

  ngOnInit(): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }
    this.wideQuery = window.matchMedia(QD_BP_WIDE_QUERY);
    this.applyWide(this.wideQuery.matches);
    this.wideQuery.addEventListener('change', this.onWideChange);
  }

  ngOnDestroy(): void {
    this.wideQuery?.removeEventListener('change', this.onWideChange);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    const open = this.openMenuKey();
    if (open !== null) {
      this.closeMenu(open);
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const el = this.elementRef.nativeElement as HTMLElement;
    const target = event.target as HTMLElement | null;
    const open = this.openMenuKey();
    if (!target || open === null) {
      return;
    }
    const openItem = el.querySelector(`[data-menu-key="${open}"]`);
    if (!openItem?.contains(target)) {
      this.closeMenu(open);
    }
  }

  openMenu(key: string): void {
    this.openMenuKey.set(key);
  }

  closeMenu(key: string): void {
    if (this.openMenuKey() !== key) {
      return;
    }
    const trigger = this.menuTrigger(key);
    const focusWasInsideMenu = this.menuHoldsFocus(key);
    this.openMenuKey.set(null);
    if (focusWasInsideMenu) {
      trigger?.focus();
    }
  }

  toggleMenu(key: string): void {
    if (this.openMenuKey() === key) {
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
    if (this.hoveredMenuKey === key && this.openMenuKey() === key) {
      this.hoveredMenuKey = null;
      this.openMenu(key);
      return;
    }
    this.toggleMenu(key);
  }

  openSheet(): void {
    this.openMenuKey.set(null);
    this.sheetOpen.set(true);
  }

  closeSheet(): void {
    this.sheetOpen.set(false);
  }

  openLinkingWorkspace(): void {
    if (this.wide()) {
      this.linkingWorkspace.openWorkspace();
      return;
    }

    this.closeSheet();
    afterNextRender(() => this.linkingWorkspace.openWorkspace(), { injector: this.injector });
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  signIn(): void {
    this.oidcSecurityService.authorize();
  }

  signOut(): void {
    this.currentUserStore.clear();
    this.authReturnLocationStore.clear();
    this.oidcSecurityService.logoff().subscribe();
  }

  private applyWide(matches: boolean): void {
    this.wide.set(matches);
    if (matches) {
      this.closeSheet();
      return;
    }
    this.openMenuKey.set(null);
  }

  private isItemVisible(item: NavItem): boolean {
    return item.key !== 'settings' || this.isActiveOwner();
  }

  private menuHost(key: string): HTMLElement | null {
    const host = this.elementRef.nativeElement as HTMLElement;
    return host.querySelector<HTMLElement>(`[data-menu-key="${key}"]`);
  }

  private menuTrigger(key: string): HTMLElement | null {
    return this.menuHost(key)?.querySelector<HTMLElement>('button') ?? null;
  }

  private menuHoldsFocus(key: string): boolean {
    const active = document.activeElement;
    return active instanceof HTMLElement && (this.menuHost(key)?.contains(active) ?? false);
  }
}
