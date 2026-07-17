import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

/** Product name; appended to every page title and shown alone on the home/root page. */
const BRAND_TITLE_AR = 'المنهج القرآني';
const TITLE_SEPARATOR = ' — ';

/**
 * Browser-tab title strategy: "<page> — المنهج القرآني", falling back to the brand alone
 * when a route defines no `title` (the dashboard/home landing). Page names come from each
 * route's own `title` (nav label or explorer page title) — never fabricated here.
 */
@Injectable({ providedIn: 'root' })
export class AppTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const pageTitle = this.buildTitle(snapshot);
    this.title.setTitle(
      pageTitle ? `${pageTitle}${TITLE_SEPARATOR}${BRAND_TITLE_AR}` : BRAND_TITLE_AR,
    );
  }
}
