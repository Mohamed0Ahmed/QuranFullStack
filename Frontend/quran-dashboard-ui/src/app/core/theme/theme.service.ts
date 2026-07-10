import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, map } from 'rxjs';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'qd-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly themeSubject = new BehaviorSubject<Theme>(this.resolveInitialTheme());
  readonly theme$ = this.themeSubject.asObservable();
  readonly isDark$ = this.theme$.pipe(map((t) => t === 'dark'));

  constructor() {
    this.init();
  }

  private init(): void {
    if (this.isBrowser) {
      document.documentElement.setAttribute('data-theme', this.themeSubject.value);
    }
  }

  toggle(): void {
    const next: Theme = this.themeSubject.value === 'dark' ? 'light' : 'dark';
    this.themeSubject.next(next);
    if (this.isBrowser) {
      this.writeStoredTheme(next);
      document.documentElement.setAttribute('data-theme', next);
    }
  }

  private resolveInitialTheme(): Theme {
    if (!this.isBrowser) {
      return 'light';
    }
    const stored = this.readStoredTheme();
    if (stored === 'dark' || stored === 'light') {
      return stored;
    }
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }
    return 'light';
  }

  private readStoredTheme(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }

  private writeStoredTheme(theme: Theme): void {
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {

    }
  }
}
