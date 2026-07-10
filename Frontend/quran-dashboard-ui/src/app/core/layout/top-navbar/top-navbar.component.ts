import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NAV_ITEMS, NavItem } from '../../navigation/nav-items';
import { ThemeService } from '../../theme/theme.service';

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

  readonly allItems: NavItem[] = NAV_ITEMS;
  readonly primaryItems = NAV_ITEMS.filter((i) => i.group === 'primary');
  readonly moreItems = NAV_ITEMS.filter((i) => i.group === 'more');
  readonly actionItems = NAV_ITEMS.filter((i) => i.group === 'actions');

  moreOpen = false;
  mobileOpen = false;

  protected readonly isDark = toSignal(this.themeService.isDark$, { initialValue: false });

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.moreOpen) {
      this.closeMore();
    }
    if (this.mobileOpen) {
      this.closeMobile();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.moreOpen) {
      const el = this.elementRef.nativeElement as HTMLElement;
      const target = event.target as HTMLElement | null;
      if (target && !el.querySelector('.more-dropdown')?.contains(target)) {
        this.closeMore();
      }
    }
  }

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

  toggleTheme(): void {
    this.themeService.toggle();
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
