import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';

@Component({
  selector: 'qd-linking-ayah-selection-toolbar',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './linking-ayah-selection-toolbar.component.html',
  styleUrl: './linking-ayah-selection-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahSelectionToolbarComponent {
  readonly selectAllLabel = input.required<string>();
  readonly clearAllLabel = input.required<string>();
  readonly selectAllRequested = output<void>();
  readonly clearAllRequested = output<void>();
}
