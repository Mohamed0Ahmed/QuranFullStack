import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { RouterLink, type UrlTree } from '@angular/router';

import type { DashboardLinkingContent } from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-linking-flow',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-linking-flow.component.html',
  styleUrl: './dashboard-linking-flow.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardLinkingFlowComponent {
  readonly content = input.required<DashboardLinkingContent>();
  readonly mushafTarget = input.required<UrlTree>();

  protected readonly activeStage = signal(0);

  protected selectStage(index: number): void {
    this.activeStage.set(index);
  }
}
