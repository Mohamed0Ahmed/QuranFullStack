import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/**
 * Active-door display + the single-door operations (plan-slice-b.md T415). **No**
 * relations/protection entries (plan.md §5.1, contract `:250-251`). Move and reorder
 * are deliberately not offered here yet — move needs the two-stage picker (T505) and
 * reorder wiring lands in T506; the tree's own inline number editor (T413) already
 * covers reordering in the meantime. Bulk mode joins this panel in T503.
 */
@Component({
  selector: 'qd-abwab-side-panel',
  standalone: true,
  templateUrl: './abwab-side-panel.component.html',
  styleUrl: './abwab-side-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabSidePanelComponent {
  readonly selectedDoor = input<AbwabDoorDto | null>(null);

  readonly addChildRequested = output<void>();
  readonly editRequested = output<void>();
  readonly archiveRequested = output<void>();
  readonly clearRequested = output<void>();

  protected get activeDoorHeading(): string { return ABWAB_LABELS.activeDoorHeading; }
  protected get noSelectionHint(): string { return ABWAB_LABELS.noSelectionHint; }
  protected get clearLabel(): string { return ABWAB_LABELS.clearSelection; }
  protected get operationsHeading(): string { return ABWAB_LABELS.operationsHeading; }
  protected get addChildLabel(): string { return ABWAB_LABELS.addChildOp; }
  protected get editLabel(): string { return ABWAB_LABELS.editOp; }
  protected get archiveLabel(): string { return ABWAB_LABELS.archiveOp; }
}
