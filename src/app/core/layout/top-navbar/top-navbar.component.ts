import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NAV_ITEMS, NavItem } from '../../navigation/nav-items';
import { ThemeService } from '../../theme/theme.service';

@Component({
  selector: 'qd-top-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './top-navbar.component.html',
  styleUrls: ['./top-navbar.component.scss'],
})
export class TopNavbarComponent {
  private readonly router = inject(Router);
  readonly themeService = inject(ThemeService);

  readonly allItems: NavItem[] = NAV_ITEMS;
  readonly primaryItems = NAV_ITEMS.filter((i) => i.group === 'primary');
  readonly moreItems = NAV_ITEMS.filter((i) => i.group === 'more');
  readonly actionItems = NAV_ITEMS.filter((i) => i.group === 'actions');

  moreOpen = false;
  mobileOpen = false;

  toggleMore(): void {
    this.moreOpen = !this.moreOpen;
  }

  closeMore(): void {
    this.moreOpen = false;
  }

  toggleMobile(): void {
    this.mobileOpen = !this.mobileOpen;
    if (!this.mobileOpen) {
      this.moreOpen = false;
    }
  }

  closeMobile(): void {
    this.mobileOpen = false;
    this.moreOpen = false;
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
}
