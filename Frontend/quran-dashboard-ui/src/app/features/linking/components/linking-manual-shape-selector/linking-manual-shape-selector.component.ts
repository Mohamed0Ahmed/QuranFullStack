import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingManualLinkShape } from '../../models/linking-manual-mushaf.models';

@Component({
  selector: 'qd-linking-manual-shape-selector',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './linking-manual-shape-selector.component.html',
  styleUrl: './linking-manual-shape-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingManualShapeSelectorComponent {
  readonly selectedCount = input.required<number>();
  readonly linkShape = input.required<LinkingManualLinkShape>();
  readonly linkShapeChanged = output<LinkingManualLinkShape>();

  protected readonly labels = LINKING_LABELS;
}
