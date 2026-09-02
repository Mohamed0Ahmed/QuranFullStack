import { Injectable, inject } from '@angular/core';

import { AuthSessionStore } from '../../../core/auth/auth-session.store';

@Injectable({ providedIn: 'root' })
export class LinkingAccessService {
  private readonly authSession = inject(AuthSessionStore);

  readonly isResolving = this.authSession.isResolving;
  readonly canUseLinking = this.authSession.isActiveOwner;
}
