import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceItem } from '../../models/linking-workspace.models';

@Component({
  selector: 'qd-linking-workspace-item',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './linking-workspace-item.component.html',
  styleUrl: './linking-workspace-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceItemComponent {
  readonly item = input.required<LinkingWorkspaceItem>();
  readonly removeRequested = output<LinkingWorkspaceItem>();
  readonly editSelectionRequested = output<LinkingWorkspaceItem>();
  readonly directLinkRequested = output<LinkingWorkspaceItem>();

  protected readonly labels = LINKING_LABELS;
  protected readonly sourceKindLabel = computed(() => this.labels.sourceKinds[this.item().source.kind]);
  protected readonly resultCountText = computed(() => {
    const count = this.item().resultCount;
    return count === null ? this.labels.unresolvedResultCount : `${count} نتيجة`;
  });
  protected readonly selectedCountText = computed(() => {
    const { resultCount, selection } = this.item();
    if (resultCount === null) {
      return this.labels.unresolvedResultCount;
    }

    const overrides = new Set(selection.verseKeys).size;
    const selectedCount =
      selection.mode === 'all-except' ? Math.max(0, resultCount - overrides) : overrides;
    return `${selectedCount} آية`;
  });
  protected readonly highlightText = computed(() =>
    this.item().highlightSourceWords ? this.labels.highlightEnabled : this.labels.highlightDisabled,
  );
}
