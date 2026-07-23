import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { BulkMoveRenderPayload } from './abwab-audit-render.models';

// §6.3 bulk-move render: descendants are nested under their moved root (never reported as
// independent moves), and sibling-order side effects are grouped by affected parent/order scope —
// this IS where ordering data appears for a move; there is no standalone "ordering" component.
@Component({
  selector: 'qd-abwab-bulk-move-render',
  standalone: true,
  templateUrl: './bulk-move-render.component.html',
  styleUrl: './bulk-move-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BulkMoveRenderComponent {
  readonly payload = input.required<BulkMoveRenderPayload>();
}
