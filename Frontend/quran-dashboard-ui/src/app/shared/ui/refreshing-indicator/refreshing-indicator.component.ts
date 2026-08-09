import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'qd-refreshing-indicator',
  standalone: true,
  templateUrl: './refreshing-indicator.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdRefreshingIndicatorComponent {
  readonly active = input(false);
  readonly testId = input('qd-refreshing-indicator');
}
