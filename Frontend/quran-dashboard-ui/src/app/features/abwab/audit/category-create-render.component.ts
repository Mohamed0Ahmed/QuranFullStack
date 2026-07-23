import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { CategoryCreateRenderPayload, formatFieldValue, formatOrderValue } from './abwab-audit-render.models';

@Component({
  selector: 'qd-abwab-category-create-render',
  standalone: true,
  templateUrl: './category-create-render.component.html',
  styleUrl: './category-create-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryCreateRenderComponent {
  readonly payload = input.required<CategoryCreateRenderPayload>();
  protected readonly formatValue = formatFieldValue;
  protected readonly formatOrder = formatOrderValue;
}
