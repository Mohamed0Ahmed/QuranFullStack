import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdResultItemDirective } from '../../../../shared/ui/result-list/result-list.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceSourceRowView } from '../../models/linking-workspace-view.models';
import { linkingSourcePresentation } from '../../utils/linking-source-presentation';

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
  readonly removeRequested = output<void>();
  protected readonly labels = LINKING_LABELS;
  protected readonly sourceKind = computed(() => linkingSourcePresentation(this.row().item.source));
  protected readonly membershipLabel = computed(() =>
    `${this.row().checked ? 'إلغاء تحديد' : 'تحديد'} مصدر ${this.row().item.source.label} للعملية`,
  );
  protected readonly showAyahsLabel = computed(() =>
    `${this.labels.showAyahs}: ${this.row().countLabel}`,
  );
  protected readonly automaticWordsLabel = computed(() =>
    `${this.labels.highlightStep}: ${this.row().item.source.label}`,
  );
  protected readonly automaticConfiguration = computed(() => {
    const configuration = this.row().item.configuration;
    return configuration.kind === 'automatic' ? configuration : null;
  });

  protected toggleMembershipFromRow(event: MouseEvent): void {
    if (event.target instanceof Element && event.target.closest('a, button, input, label, select, textarea')) {
      return;
    }

    this.membershipChanged.emit(!this.row().checked);
  }
}
