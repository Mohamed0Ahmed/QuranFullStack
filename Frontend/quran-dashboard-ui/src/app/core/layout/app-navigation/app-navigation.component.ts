import { NgTemplateOutlet, isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  PLATFORM_ID,
  afterRenderEffect,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive, UrlTree } from '@angular/router';

import { NavItem } from '../../navigation/nav-items';
import { NavigationResumeService } from '../../navigation/navigation-resume.service';
import { DASHBOARD_ROUTE_PATH } from '../../navigation/route-paths';
import { QdActionDirective } from '../../../shared/ui/action/action.directive';
import { NavIconComponent } from '../nav-icon/nav-icon.component';
import {
  placeFloatingLayer,
  resolveFloatingDirection,
  resolveRootFontSize,
} from '../../../shared/ui/floating-layer/floating-layer-placement';

export type QdAppNavigationMode = 'desktop' | 'sheet';

export const NAV_GROUP_ONLY_ROUTE = '';

const NAV_MENU_ANCHOR_GAP = 0;
const SHEET_MORE_ITEM: NavItem = {
  key: 'sheet-more',
  labelAr: 'المزيد',
  labelEn: 'More',
  route: NAV_GROUP_ONLY_ROUTE,
  icon: 'more',
  group: 'primary',
  children: [],
};

@Component({
  selector: 'qd-app-navigation',
  standalone: true,
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive, QdActionDirective, NavIconComponent],
  templateUrl: './app-navigation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppNavigationComponent {
  private readonly router = inject(Router);
  private readonly navigationResume = inject(NavigationResumeService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);

  readonly mode = input.required<QdAppNavigationMode>();
  readonly items = input.required<readonly NavItem[]>();
  readonly openMenuKey = input<string | null>(null);
  readonly sheetVisible = input(false);

  readonly menuToggled = output<string>();
  readonly menuPointerEntered = output<string>();
  readonly menuPointerLeft = output<string>();
  readonly menuLinkActivated = output<string>();
  readonly navigated = output<void>();

  protected readonly sheet = computed(() => this.mode() === 'sheet');
  protected readonly renderedItems = computed(() =>
    this.sheet() ? this.groupSheetItems(this.items()) : this.items(),
  );
  private readonly sheetOpenMenuKey = signal<string | null>(null);

  private readonly menuSurface = viewChild<ElementRef<HTMLElement>>('menuSurface');

  constructor() {
    afterRenderEffect(() => this.placeMenuSurface());
    effect(() => {
      if (!this.sheet() || !this.sheetVisible()) {
        return;
      }
      const activeGroup = this.renderedItems().find(
        (item) => this.hasMenu(item) && this.isMenuActive(item),
      );
      untracked(() => this.sheetOpenMenuKey.set(activeGroup?.key ?? null));
    });

    if (isPlatformBrowser(this.platformId)) {
      const onViewportResize = (): void => this.placeMenuSurface();
      window.addEventListener('resize', onViewportResize);
      this.destroyRef.onDestroy(() => window.removeEventListener('resize', onViewportResize));
    }
  }

  protected hasMenu(item: NavItem): boolean {
    return (item.children?.length ?? 0) > 0;
  }

  protected isMenuOpen(item: NavItem): boolean {
    const openKey = this.sheet() ? this.sheetOpenMenuKey() : this.openMenuKey();
    return openKey === item.key;
  }

  protected toggleItemMenu(item: NavItem): void {
    if (!this.sheet()) {
      this.menuToggled.emit(item.key);
      return;
    }
    this.sheetOpenMenuKey.update((openKey) => openKey === item.key ? null : item.key);
  }

  protected menuId(item: NavItem): string {
    return `${item.key}-${this.mode()}-menu`;
  }

  protected rendersAsLabel(item: NavItem): boolean {
    return (
      item.route === NAV_GROUP_ONLY_ROUTE ||
      (item.children !== undefined && item.group === 'actions')
    );
  }

  protected exactFor(route: string): boolean {
    return route === DASHBOARD_ROUTE_PATH;
  }

  protected navigationTarget(item: NavItem): UrlTree {
    return this.navigationResume.targetFor(item);
  }

  protected isMenuActive(item: NavItem): boolean {
    return this.activeMatchRoutes(item).some((route) =>
      this.router.isActive(route, {
        paths: 'subset',
        queryParams: 'ignored',
        fragment: 'ignored',
        matrixParams: 'ignored',
      }),
    );
  }

  protected onPointerEnter(item: NavItem): void {
    if (!this.sheet() && this.hasMenu(item)) {
      this.menuPointerEntered.emit(item.key);
    }
  }

  protected onPointerLeave(item: NavItem): void {
    if (!this.sheet() && this.hasMenu(item)) {
      this.menuPointerLeft.emit(item.key);
    }
  }

  protected onChildActivated(parent: NavItem): void {
    if (this.sheet()) {
      this.navigated.emit();
      return;
    }
    this.menuLinkActivated.emit(parent.key);
  }

  private placeMenuSurface(): void {
    if (this.sheet()) {
      return;
    }
    const menu = this.menuSurface()?.nativeElement;
    const anchor = menu?.parentElement;
    const view = menu?.ownerDocument.defaultView;
    if (!menu || !anchor || !view) {
      return;
    }

    const placement = placeFloatingLayer(
      anchor.getBoundingClientRect(),
      { width: menu.offsetWidth, height: menu.scrollHeight },
      { width: view.innerWidth, height: view.innerHeight },
      resolveFloatingDirection(menu),
      resolveRootFontSize(menu),
      NAV_MENU_ANCHOR_GAP,
    );

    menu.style.setProperty('inset-block-start', `${placement.top}px`);
    menu.style.setProperty('left', `${placement.left}px`);
    menu.style.setProperty('max-block-size', `${placement.maxBlockSize}px`);
  }

  private activeMatchRoutes(item: NavItem): string[] {
    return item.route === NAV_GROUP_ONLY_ROUTE
      ? (item.children ?? []).map((child) => child.route)
      : [item.route];
  }

  private groupSheetItems(items: readonly NavItem[]): readonly NavItem[] {
    const primaryItems = items.filter((item) => item.group === 'primary');
    const moreItems = items
      .filter((item) => item.group !== 'primary')
      .flatMap((item) => item.children === undefined ? [item] : [item, ...item.children]);

    return moreItems.length === 0
      ? primaryItems
      : [...primaryItems, { ...SHEET_MORE_ITEM, children: moreItems }];
  }
}
