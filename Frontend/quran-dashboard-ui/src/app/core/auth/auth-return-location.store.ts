import { Injectable } from '@angular/core';

import { DASHBOARD_ROUTE_PATH } from '../navigation/route-paths';

@Injectable({ providedIn: 'root' })
export class AuthReturnLocationStore {
  private static readonly storageKey = 'quran-dashboard.auth.return-location';

  remember(url: string): void {
    if (!this.isSafeInternalUrl(url)) {
      return;
    }

    sessionStorage.setItem(AuthReturnLocationStore.storageKey, url);
  }

  consume(fallback = DASHBOARD_ROUTE_PATH): string {
    const stored = sessionStorage.getItem(AuthReturnLocationStore.storageKey);
    sessionStorage.removeItem(AuthReturnLocationStore.storageKey);
    return stored && this.isSafeInternalUrl(stored) ? stored : fallback;
  }

  clear(): void {
    sessionStorage.removeItem(AuthReturnLocationStore.storageKey);
  }

  private isSafeInternalUrl(url: string): boolean {
    return url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');
  }
}
