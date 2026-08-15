import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { take } from 'rxjs';

import { AuthReturnLocationStore } from '../../../../core/auth/auth-return-location.store';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';

type AuthCallbackStatus = 'pending' | 'error';

@Component({
  selector: 'qd-auth-callback',
  standalone: true,
  imports: [QdErrorStateComponent, ExplorerPanelSkeletonComponent],
  templateUrl: './auth-callback.component.html',
  styleUrls: ['./auth-callback.component.scss'],
})
export class AuthCallbackComponent implements OnInit {
  private readonly oidcSecurityService = inject(OidcSecurityService);
  private readonly currentUserStore = inject(CurrentUserStore);
  private readonly authReturnLocationStore = inject(AuthReturnLocationStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly status = signal<AuthCallbackStatus>('pending');

  ngOnInit(): void {
    this.oidcSecurityService.isAuthenticated$
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ isAuthenticated }) => {
        if (isAuthenticated) {
          void this.completeSignIn();
          return;
        }

        const queryParamMap = this.route.snapshot.queryParamMap;
        const isFailure = queryParamMap.has('error') || queryParamMap.has('code');
        if (isFailure) {
          this.status.set('error');
          return;
        }

        this.authReturnLocationStore.clear();
        this.router.navigateByUrl(DASHBOARD_ROUTE_PATH);
      });
  }

  retry(): void {
    this.oidcSecurityService.authorize();
  }

  private async completeSignIn(): Promise<void> {
    await this.currentUserStore.completeInteractiveSignIn();
    if (!this.currentUserStore.isAuthenticated()) {
      this.status.set('error');
      return;
    }
    await this.router.navigateByUrl(this.authReturnLocationStore.consume(DASHBOARD_ROUTE_PATH));
  }
}
