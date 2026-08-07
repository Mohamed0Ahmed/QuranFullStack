import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

import { ApiResponse } from '../data-access/api-response.model';
import { AuthReturnLocationStore } from './auth-return-location.store';
import { CurrentUserStore } from './current-user.store';

export interface WriteAuthFailure {
  kind: 'unauthorized' | 'forbidden';
  message: string | null;
}

@Injectable({ providedIn: 'root' })
export class WriteAuthFailureCoordinator {
  private readonly router = inject(Router);
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly authReturnLocationStore = inject(AuthReturnLocationStore);
  private authorizationStarted = false;

  async handle(error: unknown): Promise<WriteAuthFailure | null> {
    if (!(error instanceof HttpErrorResponse)) {
      return null;
    }

    if (error.status === 401) {
      if (!this.authorizationStarted) {
        this.authorizationStarted = true;
        this.authReturnLocationStore.remember(this.router.url);
        this.currentUserStore.clear();
        this.oidcSecurityService.authorize();
      }
      return { kind: 'unauthorized', message: this.readMessage(error.error) };
    }

    if (error.status === 403) {
      await this.currentUserStore.refresh();
      return { kind: 'forbidden', message: this.readMessage(error.error) };
    }

    return null;
  }

  private readMessage(body: unknown): string | null {
    if (typeof body !== 'object' || body === null) {
      return null;
    }

    const response = body as Partial<ApiResponse<unknown>>;
    return typeof response.message === 'string' && response.message.trim() ? response.message : null;
  }
}
