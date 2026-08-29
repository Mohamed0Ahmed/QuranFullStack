import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import type { DashboardWordContent } from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-word-structure',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-word-structure.component.html',
  styleUrl: './dashboard-word-structure.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardWordStructureComponent {
  readonly content = input.required<DashboardWordContent>();
}
