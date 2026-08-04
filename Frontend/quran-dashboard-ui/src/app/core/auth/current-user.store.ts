import { Injectable, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../data-access/api-response.model';
import { AccessApi } from './access.api';
import { CurrentUser } from './current-user.model';

@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
  private static readonly fallbackMessage = 'تعذر تحميل بيانات المستخدم الحالي.';

  private readonly accessApi = inject(AccessApi);

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly errorMessageSignal = signal<string | null>(null);
  private ensureLoadedPromise: Promise<void> | null = null;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly errorMessage = this.errorMessageSignal.asReadonly();

  load(): void {
    this.ensureLoadedPromise = this.fetchAndCache();
  }

  ensureLoaded(): Promise<void> {
    return (this.ensureLoadedPromise ??= this.fetchAndCache());
  }

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
