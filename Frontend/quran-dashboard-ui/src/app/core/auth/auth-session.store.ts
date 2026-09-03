import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom, take } from 'rxjs';

import { CurrentUserResponse } from '../api/generated/models/current-user-response';
import { ApiResponse } from '../data-access/api-response.model';
import { DASHBOARD_ROUTE_PATH } from '../navigation/route-paths';
import { AccessApi } from './access.api';
import { CurrentUser, toCurrentUser } from './current-user.model';
import { PermissionCode } from './permission-code';

type AuthResolutionState = 'idle' | 'resolving' | 'authenticated' | 'anonymous' | 'error';
type AuthResolutionMode = 'passive' | 'interactive' | 'refresh';

@Injectable({ providedIn: 'root' })
export class AuthSessionStore {
  private static readonly fallbackMessage = 'تعذر تحميل بيانات المستخدم الحالي.';
  private static readonly returnLocationKey = 'quran-dashboard.auth.return-location';

  private readonly accessApi = inject(AccessApi);
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly router = inject(Router);

  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly permissionsSignal = signal<ReadonlySet<PermissionCode>>(new Set());
  private readonly resolutionStateSignal = signal<AuthResolutionState>('idle');
  private readonly isAuthenticatedSignal = signal(false);
  private pendingResolution: Promise<void> | null = null;
  private pendingExchange: Promise<void> | null = null;
  private authorizationStarted = false;
  private callbackMayStartInteractiveResolution = false;
  private requestVersion = 0;

  readonly subject = computed(() => this.currentUserSignal()?.sub ?? null);
  readonly isResolving = computed(() => {
    const state = this.resolutionStateSignal();
    return state === 'idle' || state === 'resolving';
  });
  readonly isAuthenticated = this.isAuthenticatedSignal.asReadonly();
  readonly isActiveOwner = computed(() => {
    const user = this.currentUserSignal();
    return user?.status === 'active' && user.isOwner;
  });

  constructor() {
    void this.ensureResolved();
  }

  ensureResolved(): Promise<void> {
    const state = this.resolutionStateSignal();
    if (state === 'authenticated' || state === 'anonymous') {
      return Promise.resolve();
    }

    return this.pendingResolution ?? this.startResolution('passive');
  }

  startSignIn(returnUrl: string = this.router.url): void {
    this.rememberReturnLocation(returnUrl);
    this.callbackMayStartInteractiveResolution = true;
    this.startAuthorization();
  }

  retrySignIn(): void {
    this.authorizationStarted = true;
    this.callbackMayStartInteractiveResolution = true;
    this.oidcSecurityService.authorize();
  }

  async completeCallback(): Promise<boolean> {
    if (this.pendingResolution) {
      await this.pendingResolution;
    }

    if (!await this.hasIdentityProviderSession()) {
      this.authorizationStarted = false;
      const callbackUrl = this.router.parseUrl(this.router.url);
      if (callbackUrl.queryParamMap.has('error') || callbackUrl.queryParamMap.has('code')) {
        return false;
      }

      this.clearReturnLocation();
      await this.router.navigateByUrl(DASHBOARD_ROUTE_PATH);
      return true;
    }

    // A callback may arrive after startup resolved anonymously, before the IdP session existed.
    if (
      this.callbackMayStartInteractiveResolution
      && (this.resolutionStateSignal() === 'anonymous' || this.resolutionStateSignal() === 'error')
    ) {
      this.callbackMayStartInteractiveResolution = false;
      await this.startResolution('interactive');
    }

    if (!this.isAuthenticatedSignal()) {
      this.authorizationStarted = false;
      return false;
    }

    this.authorizationStarted = false;
    this.callbackMayStartInteractiveResolution = false;
    await this.router.navigateByUrl(this.consumeReturnLocation());
    return true;
  }

  async handleWriteAuthFailure(
    error: unknown,
  ): Promise<'unauthorized' | 'forbidden' | null> {
    if (!(error instanceof HttpErrorResponse)) {
      return null;
    }

    if (error.status === 401) {
      this.rememberReturnLocation(this.router.url);
      this.clearPublishedSession();
      if (!this.authorizationStarted) {
        this.callbackMayStartInteractiveResolution = true;
        this.startAuthorization();
      }
      return 'unauthorized';
    }

    if (error.status === 403) {
      await (this.pendingResolution ?? this.startResolution('refresh'));
      return 'forbidden';
    }

    return null;
  }

  async signOut(): Promise<void> {
    try {
      await firstValueFrom(this.accessApi.revokeCurrentSession());
    } catch {
      // Backend revocation is best effort; cookie evidence must remain available until it settles.
    } finally {
      this.clearPublishedSession();
      this.clearReturnLocation();
      this.authorizationStarted = false;
      this.callbackMayStartInteractiveResolution = false;
      this.oidcSecurityService.logoff().subscribe({ error: () => undefined });
    }
  }

  can(permission: PermissionCode): boolean {
    const user = this.currentUserSignal();
    return user?.status === 'active' && (user.isOwner || this.permissionsSignal().has(permission));
  }

  canAny(permissions: readonly PermissionCode[]): boolean {
    return permissions.some((permission) => this.can(permission));
  }

  private startResolution(mode: AuthResolutionMode): Promise<void> {
    if (this.pendingResolution) {
      return this.pendingResolution;
    }

    const requestVersion = ++this.requestVersion;
    this.resolutionStateSignal.set('resolving');
    const resolution = this.resolve(requestVersion, mode);
    this.pendingResolution = resolution;
    void resolution.finally(() => {
      if (this.pendingResolution === resolution) {
        this.pendingResolution = null;
      }
    });
    return resolution;
  }

  private async resolve(requestVersion: number, mode: AuthResolutionMode): Promise<void> {
    let exchangedDeviceSession = false;
    try {
      if (mode === 'interactive') {
        exchangedDeviceSession = true;
        await this.exchangeDeviceSession();
      }

      let response: ApiResponse<CurrentUserResponse>;
      try {
        response = await firstValueFrom(this.accessApi.getMe());
      } catch (error: unknown) {
        if (
          mode === 'passive'
          && this.isUnauthorized(error)
          && await this.hasIdentityProviderSession()
        ) {
          exchangedDeviceSession = true;
          await this.exchangeDeviceSession();
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
        this.markResolutionError();
        return;
      }

      this.currentUserSignal.set(currentUser);
      this.permissionsSignal.set(new Set(currentUser.permissions));
      this.isAuthenticatedSignal.set(true);
      this.resolutionStateSignal.set('authenticated');
      this.callbackMayStartInteractiveResolution = false;
    } catch (error: unknown) {
      if (requestVersion !== this.requestVersion) {
        return;
      }

      if (this.isUnauthorized(error)) {
        this.callbackMayStartInteractiveResolution = mode === 'passive' && !exchangedDeviceSession;
        this.clearPublishedSession();
        return;
      }

      this.callbackMayStartInteractiveResolution = false;
      this.markResolutionError();
    }
  }

  private exchangeDeviceSession(): Promise<void> {
    if (this.pendingExchange) {
      return this.pendingExchange;
    }

    const exchange = this.createDeviceSession();
    this.pendingExchange = exchange;
    void exchange.then(() => {
      if (this.pendingExchange === exchange) {
        this.pendingExchange = null;
      }
    }, () => {
      if (this.pendingExchange === exchange) {
        this.pendingExchange = null;
      }
    });
    return exchange;
  }

  private async createDeviceSession(): Promise<void> {
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
      throw new Error(response.message ?? AuthSessionStore.fallbackMessage);
    }
  }

  private async hasIdentityProviderSession(): Promise<boolean> {
    const result = await firstValueFrom(this.oidcSecurityService.isAuthenticated$.pipe(take(1)));
    return result.isAuthenticated;
  }

  private clearPublishedSession(): void {
    this.requestVersion += 1;
    this.pendingResolution = null;
    this.currentUserSignal.set(null);
    this.permissionsSignal.set(new Set());
    this.isAuthenticatedSignal.set(false);
    this.resolutionStateSignal.set('anonymous');
  }

  private markResolutionError(): void {
    if (this.currentUserSignal() === null) {
      this.permissionsSignal.set(new Set());
      this.isAuthenticatedSignal.set(false);
    }
    this.resolutionStateSignal.set('error');
  }

  private startAuthorization(): void {
    if (this.authorizationStarted) {
      return;
    }
    this.authorizationStarted = true;
    this.oidcSecurityService.authorize();
  }

  private rememberReturnLocation(url: string): void {
    if (this.isSafeInternalUrl(url)) {
      sessionStorage.setItem(AuthSessionStore.returnLocationKey, url);
    }
  }

  private consumeReturnLocation(): string {
    const stored = sessionStorage.getItem(AuthSessionStore.returnLocationKey);
    sessionStorage.removeItem(AuthSessionStore.returnLocationKey);
    return stored && this.isSafeInternalUrl(stored) ? stored : DASHBOARD_ROUTE_PATH;
  }

  private clearReturnLocation(): void {
    sessionStorage.removeItem(AuthSessionStore.returnLocationKey);
  }

  private isSafeInternalUrl(url: string): boolean {
    return url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/\\');
  }

  private isUnauthorized(error: unknown): boolean {
    return error instanceof HttpErrorResponse && error.status === 401;
  }
}
