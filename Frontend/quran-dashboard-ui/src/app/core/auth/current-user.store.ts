import { Injectable, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../data-access/api-response.model';
import { AccessApi } from './access.api';
import { CurrentUser } from './current-user.model';

/**
 * App-wide holder for the authenticated user's local account (Feature 033, Phase 1).
 *
 * `load()` is fired post-callback once Logto authentication is confirmed. It is
 * intentionally minimal — no polling, no caching — because the callback flow must not
 * block on it: navigation proceeds regardless of the result. A failure is captured as a
 * calm Arabic `errorMessage` (never thrown) so it can never crash the callback. Phase 2
 * consumes `currentUser` (role / status) to gate the pending-activation flow.
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
  private readonly accessApi = inject(AccessApi);

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly errorMessageSignal = signal<string | null>(null);

  /** The authenticated user's local account, or `null` before it loads / on failure. */
  readonly currentUser = this.currentUserSignal.asReadonly();
  /** A calm Arabic message when the load failed, else `null`. */
  readonly errorMessage = this.errorMessageSignal.asReadonly();

  load(): void {
    const fallbackMessage = 'تعذر تحميل بيانات المستخدم الحالي.';

    this.accessApi.getMe().subscribe({
      next: (response) => {
        if (response.isSuccess && response.data) {
          this.currentUserSignal.set(response.data);
          this.errorMessageSignal.set(null);
          return;
        }
        this.currentUserSignal.set(null);
        this.errorMessageSignal.set(response.message ?? fallbackMessage);
      },
      error: (error: unknown) => {
        this.currentUserSignal.set(null);
        this.errorMessageSignal.set(this.resolveErrorMessage(error, fallbackMessage));
      },
    });
  }

  private resolveErrorMessage(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      return this.readApiMessage(error.error) ?? fallbackMessage;
    }
    return fallbackMessage;
  }

  private readApiMessage(errorBody: unknown): string | null {
    if (typeof errorBody !== 'object' || errorBody === null) {
      return null;
    }

    const body = errorBody as Partial<ApiResponse<unknown>>;
    return typeof body.message === 'string' && body.message.trim().length > 0
      ? body.message
      : null;
  }
}
