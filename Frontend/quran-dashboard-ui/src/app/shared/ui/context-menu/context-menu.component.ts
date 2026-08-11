import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdFloatingLayerDirective } from '../floating-layer/floating-layer.directive';
import { CONTEXT_MENU_LABELS } from './context-menu.labels';

@Component({
  selector: 'qd-context-menu',
  standalone: true,
  imports: [QdFloatingLayerDirective],
  templateUrl: './context-menu.component.html',
  styleUrl: './context-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdContextMenuComponent {
  readonly position = input.required<{ x: number; y: number }>();
  readonly menuTestId = input.required<string>();
  readonly backdropTestId = input.required<string>();
  readonly menuAriaLabel = input(CONTEXT_MENU_LABELS.menuAriaLabel);

  readonly dismissed = output<void>();
}
