import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import type { DashboardNavigationLink } from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-resume-strip',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-resume-strip.component.html',
  styleUrl: './dashboard-resume-strip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardResumeStripComponent {
  readonly items = input.required<readonly DashboardNavigationLink[]>();
}
