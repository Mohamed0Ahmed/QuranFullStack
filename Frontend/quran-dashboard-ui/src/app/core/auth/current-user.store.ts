import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { defaultIfEmpty, of, switchMap, take } from 'rxjs';

import { ApiResponse } from '../data-access/api-response.model';
import { AccessApi } from './access.api';
import { CurrentUser, toCurrentUser } from './current-user.model';
import { PermissionCode } from './permission-code';

export type CurrentUserLoadState = 'idle' | 'loading' | 'ready' | 'error';

@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
  private static readonly fallbackMessage = 'تعذر تحميل بيانات المستخدم الحالي.';

  private readonly accessApi = inject(AccessApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly oidcSecurityService = inject(OidcSecurityService, { optional: true });

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly errorMessageSignal = signal<string | null>(null);
  private readonly permissionsSignal = signal<ReadonlySet<PermissionCode>>(new Set());
  private readonly loadStateSignal = signal<CurrentUserLoadState>('idle');
  private readonly isAuthenticatedSignal = signal(false);
  private readonly authStateKnownSignal = signal(this.oidcSecurityService === undefined);
  private pendingLoad: Promise<void> | null = null;
  private requestVersion = 0;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly errorMessage = this.errorMessageSignal.asReadonly();
  readonly permissions = this.permissionsSignal.asReadonly();
  readonly loadState = this.loadStateSignal.asReadonly();
  readonly isAuthenticated = this.isAuthenticatedSignal.asReadonly();
  readonly authStateKnown = this.authStateKnownSignal.asReadonly();
  readonly isActive = computed(() => this.currentUserSignal()?.status === 'active');
  readonly isOwner = computed(() => this.currentUserSignal()?.isOwner === true);

  constructor() {
    this.oidcSecurityService?.isAuthenticated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ isAuthenticated }) => {
        this.authStateKnownSignal.set(true);
        this.isAuthenticatedSignal.set(isAuthenticated);
        if (isAuthenticated) {
          void this.refresh();
          return;
        }

        this.clear();
      });
  }

  load(): void {
    void this.refresh();
  }

  ensureLoaded(): Promise<void> {
    if (this.loadStateSignal() === 'ready') {
      return Promise.resolve();
    }

    return this.pendingLoad ?? this.startLoad();
  }

  refresh(): Promise<void> {
    return this.startLoad();
  }

  clear(): void {
    this.requestVersion += 1;
    this.pendingLoad = null;
    this.currentUserSignal.set(null);
    this.permissionsSignal.set(new Set());
    this.errorMessageSignal.set(null);
    this.loadStateSignal.set('idle');
  }

  can(permission: PermissionCode): boolean {
    return this.isActive() && (this.isOwner() || this.permissionsSignal().has(permission));
  }

  canAny(permissions: readonly PermissionCode[]): boolean {
    return permissions.some((permission) => this.can(permission));
  }

  private startLoad(): Promise<void> {
    const requestVersion = ++this.requestVersion;
    this.currentUserSignal.set(null);
    this.permissionsSignal.set(new Set());
    this.errorMessageSignal.set(null);
    this.loadStateSignal.set('loading');

    const settled = this.fetchIntoSignals(requestVersion);
    this.pendingLoad = settled;
    void settled.finally(() => {
      if (this.pendingLoad === settled) {
        this.pendingLoad = null;
      }
    });
    return settled;
  }

  private fetchIntoSignals(requestVersion: number): Promise<void> {
    const fallbackMessage = CurrentUserStore.fallbackMessage;

    const identityEvidenceToken = this.oidcSecurityService?.getIdToken() ?? of('');

    return new Promise<void>((resolve) => {
      identityEvidenceToken
        .pipe(
          take(1),
          defaultIfEmpty(''),
          switchMap((identityEvidenceToken) => this.accessApi.getMe(identityEvidenceToken)),
        )
        .subscribe({
          next: (response) => {
            if (requestVersion !== this.requestVersion) {
              resolve();
              return;
            }

            const currentUser =
              response.isSuccess && response.data ? toCurrentUser(response.data) : null;
            if (currentUser) {
              this.currentUserSignal.set(currentUser);
              this.permissionsSignal.set(new Set(currentUser.permissions));
              this.errorMessageSignal.set(null);
              this.loadStateSignal.set('ready');
            } else {
              this.currentUserSignal.set(null);
              this.permissionsSignal.set(new Set());
              this.errorMessageSignal.set(response.message ?? fallbackMessage);
              this.loadStateSignal.set('error');
            }

            resolve();
          },
          error: (error: unknown) => {
            if (requestVersion !== this.requestVersion) {
              resolve();
              return;
            }

            this.currentUserSignal.set(null);
            this.permissionsSignal.set(new Set());
            this.errorMessageSignal.set(this.resolveErrorMessage(error, fallbackMessage));
            this.loadStateSignal.set('error');
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
