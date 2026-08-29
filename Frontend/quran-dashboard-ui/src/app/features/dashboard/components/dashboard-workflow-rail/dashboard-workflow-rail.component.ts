import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import type {
  DashboardResearchSectionKey,
  DashboardWorkflowStep,
} from '../../models/dashboard-home.models';

@Component({
  selector: 'qd-dashboard-workflow-rail',
  standalone: true,
  templateUrl: './dashboard-workflow-rail.component.html',
  styleUrl: './dashboard-workflow-rail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardWorkflowRailComponent {
  readonly steps = input.required<readonly DashboardWorkflowStep[]>();
  readonly activeStep = input.required<DashboardResearchSectionKey>();
  readonly sectionSelect = output<DashboardResearchSectionKey>();

  protected selectSection(event: MouseEvent, key: DashboardResearchSectionKey): void {
    event.preventDefault();
    this.sectionSelect.emit(key);
  }
}
