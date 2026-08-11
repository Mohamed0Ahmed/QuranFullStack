import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdResultItemDirective } from '../../../../shared/ui/result-list/result-list.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceSourceRowView } from '../../models/linking-workspace-view.models';

@Component({
  selector: 'qd-linking-workspace-source-row',
  standalone: true,
  imports: [QdActionDirective, QdResultItemDirective],
  templateUrl: './linking-workspace-source-row.component.html',
  styleUrl: './linking-workspace-source-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceSourceRowComponent {
  readonly row = input.required<LinkingWorkspaceSourceRowView>();
  readonly position = input.required<number>();
  readonly setSize = input.required<number>();
  readonly membershipChanged = output<boolean>();
  readonly editRequested = output<void>();
  readonly automaticWordsChanged = output<boolean>();
  readonly manualWordsRequested = output<void>();
  readonly removeRequested = output<void>();
  protected readonly labels = LINKING_LABELS;
  protected readonly sourceKind = computed(() => this.labels.sourceKinds[this.row().item.source.kind]);
  protected readonly isAutomatic = computed(() => this.row().item.configuration.kind === 'automatic');
  protected readonly automaticConfiguration = computed(() => {
    const configuration = this.row().item.configuration;
    return configuration.kind === 'automatic' ? configuration : null;
  });
}
