import { Component, computed, inject, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

import { isPageScrollShellLayout } from '../shell-layout.model';
import { TopNavbarComponent } from '../top-navbar/top-navbar.component';
import { FooterComponent } from '../footer/footer.component';
import { NavProgressComponent } from '../nav-progress/nav-progress.component';

export const QD_MAIN_CONTENT_ID = 'qd-main-content';

@Component({
  selector: 'qd-app-shell',
  standalone: true,
  imports: [RouterOutlet, TopNavbarComponent, FooterComponent, NavProgressComponent],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly navbar = viewChild(TopNavbarComponent);

  protected readonly mainContentId = QD_MAIN_CONTENT_ID;

  protected readonly navigationSheetOpen = computed(() => this.navbar()?.sheetOpen() ?? false);

  private readonly navigationTick = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.routerState.snapshot),
      startWith(this.router.routerState.snapshot),
    ),
  );

  protected readonly pageScrollLayout = computed(() => {
    this.navigationTick();
    return this.activeRouteUsesPageScrollLayout();
  });

  private activeRouteUsesPageScrollLayout(): boolean {
    let route = this.router.routerState.root;

    while (route.firstChild) {
      route = route.firstChild;
    }

    return isPageScrollShellLayout(route.snapshot.data['shellLayout']);
  }
}
