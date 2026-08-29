import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { RouterLink, type UrlTree } from '@angular/router';

import type {
  DashboardMushafContent,
  DashboardStudyTab,
} from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-mushaf-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-mushaf-preview.component.html',
  styleUrl: './dashboard-mushaf-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardMushafPreviewComponent {
  readonly content = input.required<DashboardMushafContent>();
  readonly mushafTarget = input.required<UrlTree>();

  protected readonly activeTab = signal<DashboardStudyTab['key']>('analysis');

  protected selectTab(key: DashboardStudyTab['key']): void {
    this.activeTab.set(key);
  }

  protected selectedTab(): DashboardStudyTab {
    return this.content().tabs.find((tab) => tab.key === this.activeTab()) ?? this.content().tabs[0];
  }
}
