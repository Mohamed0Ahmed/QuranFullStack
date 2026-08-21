import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

import {
  QdFloatingLayerDirective,
  type QdFloatingLayerVariant,
} from '../floating-layer/floating-layer.directive';
import { CONTEXT_MENU_LABELS } from './context-menu.labels';

type QdContextSurfaceVariant = Extract<
  QdFloatingLayerVariant,
  'action-menu' | 'disclosure-popover' | 'tooltip'
>;

@Component({
  selector: 'qd-context-menu',
  standalone: true,
  imports: [QdFloatingLayerDirective],
  templateUrl: './context-menu.component.html',
  styleUrl: './context-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdContextMenuComponent implements AfterViewInit {
  readonly position = input.required<{ x: number; y: number }>();
  readonly anchorElement = input<HTMLElement | null>(null);
  readonly controlElement = input<HTMLElement | null>(null);
  readonly blockingBackdrop = input(true);
  readonly menuTestId = input.required<string>();
  readonly backdropTestId = input.required<string>();
  readonly menuAriaLabel = input<string>(CONTEXT_MENU_LABELS.menuAriaLabel);
  readonly variant = input<QdContextSurfaceVariant>('action-menu');

  readonly dismissed = output<void>();

  private readonly surface = viewChild.required<ElementRef<HTMLElement>>('surface');
  private readonly floatingLayer = viewChild.required(QdFloatingLayerDirective);

  ngAfterViewInit(): void {
    this.floatingLayer().reposition();
    if (this.variant() === 'disclosure-popover') {
      this.surface().nativeElement.focus();
    }
  }
}
