import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'qd-abwab-tree-branches',
  standalone: true,
  templateUrl: './abwab-tree-branches.component.html',
  styleUrl: './abwab-tree-branches.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-hidden': 'true' },
})
export class AbwabTreeBranchesComponent {
  readonly guides = input<readonly boolean[]>([]);
  readonly expanded = input(false);
}
