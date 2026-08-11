import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type ExplorerToolbarVariant = 'explorer' | 'taxonomy';

@Component({
  selector: 'qd-explorer-toolbar',
  standalone: true,
  templateUrl: './explorer-toolbar.component.html',
  styleUrl: './explorer-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'explorer-toolbar qd-toolbar',
    role: 'group',
    '[class.qd-toolbar--explorer]': "variant() === 'explorer'",
    '[class.qd-toolbar--taxonomy]': "variant() === 'taxonomy'",
    '[attr.aria-label]': 'ariaLabel()',
  },
})
export class ExplorerToolbarComponent {
  readonly ariaLabel = input.required<string>();
  readonly variant = input<ExplorerToolbarVariant>('explorer');
}
