import { Injectable, computed, inject } from '@angular/core';

import { CurrentUserStore } from '../../../core/auth/current-user.store';

@Injectable({ providedIn: 'root' })
export class LinkingAccessService {
  private readonly currentUserStore = inject(CurrentUserStore);

  readonly canUseLinking = computed(
    () =>
      this.currentUserStore.authStateKnown() &&
      this.currentUserStore.isAuthenticated() &&
      this.currentUserStore.isActive() &&
      this.currentUserStore.isOwner(),
  );
}
