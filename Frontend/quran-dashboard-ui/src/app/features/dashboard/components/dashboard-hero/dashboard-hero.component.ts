import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, UrlTree } from '@angular/router';

import type { DashboardHeroContent } from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-hero',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-hero.component.html',
  styleUrl: './dashboard-hero.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHeroComponent {
  readonly content = input.required<DashboardHeroContent>();
  readonly mushafTarget = input.required<UrlTree>();
  readonly workflowSelect = output<void>();

  protected selectWorkflow(event: MouseEvent): void {
    event.preventDefault();
    this.workflowSelect.emit();
  }
}
