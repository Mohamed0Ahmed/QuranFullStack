import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ManualProtectionRenderPayload } from './abwab-audit-render.models';

@Component({
  selector: 'qd-abwab-manual-protection-render',
  standalone: true,
  templateUrl: './manual-protection-render.component.html',
  styleUrl: './manual-protection-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManualProtectionRenderComponent {
  readonly payload = input.required<ManualProtectionRenderPayload>();
}
