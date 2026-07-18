import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take } from 'rxjs';

import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';

/**
 * Public OIDC landing page (Feature 033). The app-initializer
 * (`withAppInitializerAuthCheck()`) has already processed the Logto code/state by the
 * time this activates, so `isAuthenticated$` is settled: we read it once and, when
 * authenticated, fire the current-user load (non-blocking) before landing on the
 * dashboard.
 *
 * An abandoned login is intentionally simple: if the visitor is not authenticated here,
 * we still navigate to `/dashboard`. Browsing is public (Feature 033, Phase 2), so the
 * visitor simply lands on the dashboard as an anonymous browser — nothing forces a
 * re-login. Deep-link return-URL preservation is out of scope; login always lands on the
 * dashboard.
 */
@Component({
  selector: 'qd-auth-callback',
  standalone: true,
  templateUrl: './auth-callback.component.html',
  styleUrls: ['./auth-callback.component.scss'],
})
export class AuthCallbackComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.oidcSecurityService.isAuthenticated$
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ isAuthenticated }) => {
        if (isAuthenticated) {
          this.currentUserStore.load();
        }
        this.router.navigateByUrl(DASHBOARD_ROUTE_PATH);
      });
  }
}
