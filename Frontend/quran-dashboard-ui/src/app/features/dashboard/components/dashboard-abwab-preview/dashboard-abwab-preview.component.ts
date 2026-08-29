import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import type { DashboardAbwabContent } from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-abwab-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-abwab-preview.component.html',
  styleUrl: './dashboard-abwab-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardAbwabPreviewComponent {
  readonly content = input.required<DashboardAbwabContent>();
  readonly active = input(false);

  protected readonly expanded = signal(false);
  private hasActivated = false;

  constructor() {
    effect(() => {
      if (this.active() && !this.hasActivated) {
        this.hasActivated = true;
        this.expanded.set(true);
      }
    });
  }

  protected toggleBranch(): void {
    this.expanded.update((value) => !value);
  }
}
