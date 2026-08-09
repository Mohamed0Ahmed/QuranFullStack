import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdEmptyStateComponent } from '../empty-state/empty-state.component';
import { QdErrorStateComponent } from '../error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../explorer-panel-skeleton/explorer-panel-skeleton.component';

export type QdStateVariant = 'empty' | 'loading' | 'error';

@Component({
  selector: 'qd-state',
  standalone: true,
  imports: [ExplorerPanelSkeletonComponent, QdEmptyStateComponent, QdErrorStateComponent],
  templateUrl: './state.component.html',
  styleUrl: './state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdStateComponent {
  readonly variant = input.required<QdStateVariant>();
  readonly message = input.required<string>();

  readonly actionLabel = input<string | null>(null);

  readonly reserve = input(false);

  readonly action = output<void>();
}
