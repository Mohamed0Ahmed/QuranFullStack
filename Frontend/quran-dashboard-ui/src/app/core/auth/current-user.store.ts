import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom, take } from 'rxjs';

import { CurrentUserResponse } from '../api/generated/models/current-user-response';
import { ApiResponse } from '../data-access/api-response.model';
import { AccessApi } from './access.api';
import { CurrentUser, toCurrentUser } from './current-user.model';
import { PermissionCode } from './permission-code';

export type CurrentUserLoadState = 'idle' | 'loading' | 'ready' | 'error';

@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
  private static readonly fallbackMessage = 'تعذر تحميل بيانات المستخدم الحالي.';

  private readonly accessApi = inject(AccessApi);
  private readonly oidcSecurityService = inject(OidcSecurityService, { optional: true });

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly errorMessageSignal = signal<string | null>(null);
  private readonly permissionsSignal = signal<ReadonlySet<PermissionCode>>(new Set());
  private readonly loadStateSignal = signal<CurrentUserLoadState>('idle');
  private readonly isAuthenticatedSignal = signal(false);
  private readonly authStateKnownSignal = signal(false);
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
    void this.refresh();
  }

  load(): void {
    void this.refresh();
  }

  ensureLoaded(): Promise<void> {
    if (this.loadStateSignal() === 'ready') {
      return Promise.resolve();
    }

    return this.pendingLoad ?? this.startLoad(false);
  }

  refresh(): Promise<void> {
    return this.pendingLoad ?? this.startLoad(false);
  }

  completeInteractiveSignIn(): Promise<void> {
    return this.startLoad(true);
  }

  async revokeCurrentSession(): Promise<void> {
    try {
      if (this.isAuthenticatedSignal()) {
        await firstValueFrom(this.accessApi.revokeCurrentSession());
      }
    } finally {
      this.clear();
    }
  }

  clear(): void {
    this.requestVersion += 1;
    this.pendingLoad = null;
    this.currentUserSignal.set(null);
    this.permissionsSignal.set(new Set());
    this.errorMessageSignal.set(null);
    this.loadStateSignal.set('ready');
    this.isAuthenticatedSignal.set(false);
    this.authStateKnownSignal.set(true);
  }

  can(permission: PermissionCode): boolean {
    return this.isActive() && (this.isOwner() || this.permissionsSignal().has(permission));
  }

  canAny(permissions: readonly PermissionCode[]): boolean {
    return permissions.some((permission) => this.can(permission));
  }

  private startLoad(forceIdentityProviderSession: boolean): Promise<void> {
    const requestVersion = ++this.requestVersion;
    this.errorMessageSignal.set(null);
    this.loadStateSignal.set('loading');
    this.authStateKnownSignal.set(false);

    const settled = this.fetchIntoSignals(requestVersion, forceIdentityProviderSession);
    this.pendingLoad = settled;
    void settled.finally(() => {
      if (this.pendingLoad === settled) {
        this.pendingLoad = null;
      }
    });
    return settled;
  }

  private async fetchIntoSignals(
    requestVersion: number,
    forceIdentityProviderSession: boolean,
  ): Promise<void> {
    try {
      if (forceIdentityProviderSession) {
        await this.createDeviceSession();
      }

      let response: ApiResponse<CurrentUserResponse>;
      try {
        response = await firstValueFrom(this.accessApi.getMe());
      } catch (error: unknown) {
        if (!forceIdentityProviderSession && this.isUnauthorized(error) && await this.hasIdentityProviderSession()) {
          await this.createDeviceSession();
          response = await firstValueFrom(this.accessApi.getMe());
        } else {
          throw error;
        }
      }

      if (requestVersion !== this.requestVersion) {
        return;
      }

      const currentUser = response.isSuccess && response.data ? toCurrentUser(response.data) : null;
      if (!currentUser) {
        this.markError(response.message ?? CurrentUserStore.fallbackMessage);
        return;
      }

      this.currentUserSignal.set(currentUser);
      this.permissionsSignal.set(new Set(currentUser.permissions));
      this.errorMessageSignal.set(null);
      this.loadStateSignal.set('ready');
      this.isAuthenticatedSignal.set(true);
      this.authStateKnownSignal.set(true);
    } catch (error: unknown) {
      if (requestVersion !== this.requestVersion) {
        return;
      }

      if (this.isUnauthorized(error)) {
        this.clearAnonymous();
        return;
      }

      this.markError(this.resolveErrorMessage(error, CurrentUserStore.fallbackMessage));
    }
  }

  private async createDeviceSession(): Promise<void> {
    if (!this.oidcSecurityService) {
      throw new Error('Identity provider is unavailable.');
    }

    const [accessToken, identityEvidenceToken] = await Promise.all([
      firstValueFrom(this.oidcSecurityService.getAccessToken().pipe(take(1))),
      firstValueFrom(this.oidcSecurityService.getIdToken().pipe(take(1))),
    ]);
    if (!accessToken) {
      throw new Error('Identity provider access token is unavailable.');
    }

    const response = await firstValueFrom(
      this.accessApi.createDeviceSession(accessToken, identityEvidenceToken),
    );
    if (!response.isSuccess) {
      throw new Error(response.message ?? CurrentUserStore.fallbackMessage);
    }
  }

  private async hasIdentityProviderSession(): Promise<boolean> {
    if (!this.oidcSecurityService) {
      return false;
    }

    const result = await firstValueFrom(this.oidcSecurityService.isAuthenticated$.pipe(take(1)));
    return result.isAuthenticated;
  }

  private clearAnonymous(): void {
    this.currentUserSignal.set(null);
    this.permissionsSignal.set(new Set());
    this.errorMessageSignal.set(null);
    this.loadStateSignal.set('ready');
    this.isAuthenticatedSignal.set(false);
    this.authStateKnownSignal.set(true);
  }

  private markError(message: string): void {
    if (this.currentUserSignal() === null) {
      this.permissionsSignal.set(new Set());
      this.isAuthenticatedSignal.set(false);
    }
    this.errorMessageSignal.set(message);
    this.loadStateSignal.set('error');
    this.authStateKnownSignal.set(true);
  }

  private isUnauthorized(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 401;
  }

  private resolveErrorMessage(error: unknown, fallbackMessage: string): string {
    if (error instanceof HttpErrorResponse) {
      return this.readApiMessage(error.error) ?? fallbackMessage;
    }
    if (error instanceof Error && error.message.trim()) {
      return error.message;
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
