import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ABWAB_LABELS } from '../../models/abwab.labels';

@Component({
  selector: 'qd-abwab-door-link-operations',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './abwab-door-link-operations.component.html',
  styleUrl: './abwab-door-link-operations.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinkOperationsComponent {
  readonly doorOpen = input(false);
  readonly selectedCount = input(0);

  readonly editRequested = output<void>();
  readonly deleteRequested = output<void>();
  readonly copyRequested = output<void>();

  protected readonly canEdit = computed(() => this.doorOpen() && this.selectedCount() === 1);
  protected readonly canUseSelection = computed(() => this.doorOpen() && this.selectedCount() > 0);

  protected get heading(): string { return ABWAB_LABELS.doorLinksOperationsHeading; }
  protected get editLabel(): string { return ABWAB_LABELS.doorLinksEdit; }
  protected get deleteLabel(): string { return ABWAB_LABELS.doorLinksDelete; }
  protected get copyLabel(): string { return ABWAB_LABELS.doorLinksCopy; }
  protected get noDoorHint(): string { return ABWAB_LABELS.doorLinksNoDoorHint; }
  protected readonly selectedLabel = computed(() => ABWAB_LABELS.doorLinksSelectedCount(this.selectedCount()));
}
