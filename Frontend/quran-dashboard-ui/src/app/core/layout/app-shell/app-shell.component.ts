import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

import { isPageScrollShellLayout } from '../shell-layout.model';
import { TopNavbarComponent } from '../top-navbar/top-navbar.component';
import { FooterComponent } from '../footer/footer.component';
import { NavProgressComponent } from '../nav-progress/nav-progress.component';

@Component({
  selector: 'qd-app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, TopNavbarComponent, FooterComponent, NavProgressComponent],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  private readonly router = inject(Router);

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
