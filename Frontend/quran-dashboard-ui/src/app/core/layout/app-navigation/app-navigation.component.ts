import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { NavItem } from '../../navigation/nav-items';
import { DASHBOARD_ROUTE_PATH } from '../../navigation/route-paths';
import { QdActionDirective } from '../../../shared/ui/action/action.directive';

export type QdAppNavigationMode = 'desktop' | 'sheet';

export const NAV_GROUP_ONLY_ROUTE = '';

@Component({
  selector: 'qd-app-navigation',
  standalone: true,
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive, QdActionDirective],
  templateUrl: './app-navigation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppNavigationComponent {
  private readonly router = inject(Router);

  readonly mode = input.required<QdAppNavigationMode>();
  readonly items = input.required<readonly NavItem[]>();
  readonly openMenuKey = input<string | null>(null);

  readonly menuToggled = output<string>();
  readonly menuPointerEntered = output<string>();
  readonly menuPointerLeft = output<string>();
  readonly menuLinkActivated = output<string>();
  readonly navigated = output<void>();

  protected readonly sheet = computed(() => this.mode() === 'sheet');

  protected hasMenu(item: NavItem): boolean {
    return !this.sheet() && item.children !== undefined;
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
    if (this.hasMenu(item)) {
      this.menuPointerEntered.emit(item.key);
    }
  }

  protected onPointerLeave(item: NavItem): void {
    if (this.hasMenu(item)) {
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

  private activeMatchRoutes(item: NavItem): string[] {
    return item.route === NAV_GROUP_ONLY_ROUTE
      ? (item.children ?? []).map((child) => child.route)
      : [item.route];
  }
}
