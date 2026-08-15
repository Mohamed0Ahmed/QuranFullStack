import { Injectable, computed, inject } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';

@Injectable({ providedIn: 'root' })
export class LinkingAccessService {
  private readonly currentUserStore = inject(CurrentUserStore);

  readonly isResolving = computed(
    () => !this.currentUserStore.authStateKnown() || this.currentUserStore.loadState() === 'loading',
  );

  readonly canUseLinking = computed(
    () =>
      this.currentUserStore.authStateKnown() &&
      this.currentUserStore.isAuthenticated() &&
      this.currentUserStore.isActive() &&
      this.currentUserStore.isOwner(),
  );
}
