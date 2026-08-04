import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'qd-abwab-announcer',
  standalone: true,
  templateUrl: './abwab-announcer.component.html',
  styleUrl: './abwab-announcer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabAnnouncerComponent {
  readonly message = input<string | null>(null);
}
