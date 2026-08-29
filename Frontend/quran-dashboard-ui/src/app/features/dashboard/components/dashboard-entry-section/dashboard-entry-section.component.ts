import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import type { DashboardNavigationLink } from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-entry-section',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-entry-section.component.html',
  styleUrl: './dashboard-entry-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardEntrySectionComponent {
  readonly heading = input.required<string>();
  readonly items = input.required<readonly DashboardNavigationLink[]>();
}
