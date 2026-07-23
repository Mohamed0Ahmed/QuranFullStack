import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { SubtreeDeleteRenderPayload } from './abwab-audit-render.models';

@Component({
  selector: 'qd-abwab-subtree-delete-render',
  standalone: true,
  templateUrl: './subtree-delete-render.component.html',
  styleUrl: './subtree-delete-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubtreeDeleteRenderComponent {
  readonly payload = input.required<SubtreeDeleteRenderPayload>();
}
