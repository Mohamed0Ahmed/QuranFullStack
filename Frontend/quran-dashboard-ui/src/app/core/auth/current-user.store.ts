import { Injectable, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../data-access/api-response.model';
import { AccessApi } from './access.api';
import { CurrentUser } from './current-user.model';

/**
 * App-wide holder for the authenticated user's local account (Feature 033).
 *
 * `load()` is fired post-callback once Logto authentication is confirmed. It is
 * intentionally minimal — no polling — because the callback flow must not block on it:
 * navigation proceeds regardless of the result, and each call issues a fresh request. A
 * failure is captured as a calm Arabic `errorMessage` (never thrown) so it can never crash
 * the callback.
 *
 * `ensureLoaded()` (Phase 2) is the awaitable, load-once path the reusable `roleGuard`
 * uses: it triggers a single `GET /api/access/me` and caches the promise, so repeated
 * guard evaluations reuse the same round-trip. The cache is **success-only** — a load that
 * settles without a user (envelope/HTTP failure) drops the cached promise so the next
 * `ensureLoaded()` retries, rather than pinning a transient failure until a page reload.
 * `load()` seeds this same cache, so one login = one request (the callback's in-flight load
 * is reused by a guard's first `ensureLoaded()`). Like `load()`, it never rejects.
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
  private static readonly fallbackMessage = 'تعذر تحميل بيانات المستخدم الحالي.';

  private readonly accessApi = inject(AccessApi);

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly errorMessageSignal = signal<string | null>(null);
  private ensureLoadedPromise: Promise<void> | null = null;

  /** The authenticated user's local account, or `null` before it loads / on failure. */
  readonly currentUser = this.currentUserSignal.asReadonly();
  /** A calm Arabic message when the load failed, else `null`. */
  readonly errorMessage = this.errorMessageSignal.asReadonly();

  /**
   * Fire-and-forget load fired post-callback; issues a fresh request and seeds the shared
   * cache so a guard's first `ensureLoaded()` reuses this in-flight round-trip instead of
   * issuing a second `GET /api/access/me` (one login = one request).
   */
  load(): void {
    this.ensureLoadedPromise = this.fetchAndCache();
  }

  /**
   * Loads the current user once and caches the result. Callers awaiting this share a
   * single `GET /api/access/me`; a later call resolves against the cached promise without
   * re-fetching — but only when the load succeeded. A failed load is not cached (see
   * {@link fetchAndCache}), so the next `ensureLoaded()` retries. Never rejects — a failure
   * leaves `currentUser` null with a calm Arabic `errorMessage`, read from the signals
   * after awaiting.
   */
  ensureLoaded(): Promise<void> {
    return (this.ensureLoadedPromise ??= this.fetchAndCache());
  }

  /**
   * Starts a load and manages the shared cache with a **cache-success-only** rule: once the
   * load settles, if no user was loaded (envelope/HTTP failure), the cached promise is
   * cleared so the next `ensureLoaded()` retries. The identity check keeps a newer in-flight
   * load (e.g. a fresh `load()`) from being cleared by an earlier one.
   */
  private fetchAndCache(): Promise<void> {
    const settled = this.fetchIntoSignals();
    void settled.then(() => {
      if (this.ensureLoadedPromise === settled && this.currentUserSignal() === null) {
        this.ensureLoadedPromise = null;
      }
    });
    return settled;
  }

  private fetchIntoSignals(): Promise<void> {
    const fallbackMessage = CurrentUserStore.fallbackMessage;

    return new Promise<void>((resolve) => {
      this.accessApi.getMe().subscribe({
        next: (response) => {
          if (response.isSuccess && response.data) {
            this.currentUserSignal.set(response.data);
            this.errorMessageSignal.set(null);
          } else {
            this.currentUserSignal.set(null);
            this.errorMessageSignal.set(response.message ?? fallbackMessage);
          }
          resolve();
        },
        error: (error: unknown) => {
          this.currentUserSignal.set(null);
          this.errorMessageSignal.set(this.resolveErrorMessage(error, fallbackMessage));
          resolve();
        },
      });
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
