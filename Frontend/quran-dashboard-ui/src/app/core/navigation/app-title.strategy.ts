import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

const BRAND_TITLE_AR = 'المنهج القرآني';
const TITLE_SEPARATOR = ' — ';

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
