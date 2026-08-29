import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import type {
  DashboardPhraseContent,
  DashboardPhraseTab,
} from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-phrase-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-phrase-preview.component.html',
  styleUrl: './dashboard-phrase-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPhrasePreviewComponent {
  readonly content = input.required<DashboardPhraseContent>();

  protected readonly activeTab = signal<DashboardPhraseTab['key']>('repetitions');

  protected selectTab(key: DashboardPhraseTab['key']): void {
    this.activeTab.set(key);
  }

  protected selectedTab(): DashboardPhraseTab {
    return this.content().tabs.find((tab) => tab.key === this.activeTab()) ?? this.content().tabs[0];
  }
}
