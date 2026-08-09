import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';

@Component({
  selector: 'qd-explorer-search-row',
  standalone: true,
  imports: [QdControlDirective],
  templateUrl: './explorer-search-row.component.html',
  styleUrl: './explorer-search-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'search',
    class: 'qd-explorer-search-row',
  },
})
export class ExplorerSearchRowComponent {
  readonly searchValue = input.required<string>();
  readonly searchLabel = input.required<string>();
  readonly searchPlaceholder = input.required<string>();
  readonly searchTestid = input.required<string>();
  readonly disabled = input(false);

  readonly searchChange = output<string>();
}
