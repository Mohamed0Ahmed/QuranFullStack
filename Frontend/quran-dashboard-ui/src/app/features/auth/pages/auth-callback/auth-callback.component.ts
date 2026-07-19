import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take } from 'rxjs';

import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';

type AuthCallbackStatus = 'pending' | 'error';

// The app-initializer has already processed the Logto code/state, so `isAuthenticated$` is
// settled by the time this activates. A settled-but-unauthenticated visit is a genuine
// FAILURE (an `error` or a `code` query param is present — read before navigateByUrl replaces
// them) versus a benign ABANDONMENT (neither param), which still navigates since browsing is
// public. Only FAILURE stays here and renders the calm error state.
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

  retry(): void {
    this.oidcSecurityService.authorize();
  }
}
