import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take } from 'rxjs';

import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';

/** Component-local status: `pending` while the callback settles, `error` on a failed login. */
type AuthCallbackStatus = 'pending' | 'error';

/**
 * Public OIDC landing page (Feature 033). The app-initializer
 * (`withAppInitializerAuthCheck()`) has already processed the Logto code/state by the
 * time this activates, so `isAuthenticated$` is settled: we read it once and, when
 * authenticated, fire the current-user load (non-blocking) before landing on the
 * dashboard.
 *
 * A settled-but-unauthenticated visit is either a genuine FAILURE or a benign
 * ABANDONMENT, distinguished by the callback URL's own query params (read via
 * `ActivatedRoute.snapshot`, still populated at this point — before `navigateByUrl`
 * replaces them):
 * - `error` present → Logto/OIDC returned an error response (denied consent, provider
 *   error, …) → FAILURE.
 * - `code` present but not authenticated → the code/state exchange ran and did not
 *   authenticate → FAILURE.
 * - neither present → the visitor never completed (or never started) a login →
 *   ABANDONMENT. Browsing is public (Feature 033, Phase 2), so we still navigate to
 *   `/dashboard` as an anonymous browser; nothing forces a re-login.
 *
 * On FAILURE we stay on this page and render a calm error state with a retry action
 * instead of navigating. Deep-link return-URL preservation is out of scope; a
 * successful login always lands on the dashboard.
 */
@Component({
  selector: 'qd-auth-callback',
  standalone: true,
  imports: [QdStateComponent],
  templateUrl: './auth-callback.component.html',
  styleUrls: ['./auth-callback.component.scss'],
})
export class AuthCallbackComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly status = signal<AuthCallbackStatus>('pending');

  ngOnInit(): void {
    this.oidcSecurityService.isAuthenticated$
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ isAuthenticated }) => {
        if (isAuthenticated) {
          this.currentUserStore.load();
          this.router.navigateByUrl(DASHBOARD_ROUTE_PATH);
          return;
        }

        const queryParamMap = this.route.snapshot.queryParamMap;
        const isFailure = queryParamMap.has('error') || queryParamMap.has('code');
        if (isFailure) {
          this.status.set('error');
          return;
        }

        this.router.navigateByUrl(DASHBOARD_ROUTE_PATH);
      });
  }

  /** Restarts the login flow from the error state's single recovery action. */
  retry(): void {
    this.oidcSecurityService.authorize();
  }
}
