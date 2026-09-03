import { Component, OnInit, inject, signal } from '@angular/core';

import { AuthSessionStore } from '../../../../core/auth/auth-session.store';
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
  private readonly authSession = inject(AuthSessionStore);

  readonly status = signal<AuthCallbackStatus>('pending');

  ngOnInit(): void {
    void this.completeSignIn();
  }

  retry(): void {
    this.authSession.retrySignIn();
  }

  private async completeSignIn(): Promise<void> {
    if (!await this.authSession.completeCallback()) {
      this.status.set('error');
    }
  }
}
