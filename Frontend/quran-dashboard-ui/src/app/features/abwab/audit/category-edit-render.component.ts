import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { FieldDiffRowComponent } from './field-diff-row.component';
import { CategoryEditRenderPayload } from './abwab-audit-render.models';

// §6.3 category-edit render: order fields are shown WITHIN this payload's field set (via
// FieldDiffRowComponent), never in a standalone "ordering" component — see audit-render.spec.ts.
@Component({
  selector: 'qd-abwab-category-edit-render',
  standalone: true,
  imports: [FieldDiffRowComponent],
  templateUrl: './category-edit-render.component.html',
  styleUrl: './category-edit-render.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryEditRenderComponent {
  readonly payload = input.required<CategoryEditRenderPayload>();
}
