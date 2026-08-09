import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdActionDirective } from '../action/action.directive';

export type QdNoticeTone = 'success' | 'info';

@Component({
  selector: 'qd-notice',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './notice.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdNoticeComponent {
  readonly message = input<string>('');
  readonly tone = input<QdNoticeTone>('success');
  readonly dismissLabel = input<string | null>(null);
  readonly testId = input('qd-notice');
  readonly dismissTestId = input('qd-notice-dismiss');

  readonly dismiss = output<void>();
}
