import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { FieldDiff, formatFieldValue } from './abwab-audit-render.models';

@Component({
  selector: 'qd-abwab-field-diff-row',
  standalone: true,
  templateUrl: './field-diff-row.component.html',
  styleUrl: './field-diff-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldDiffRowComponent {
  readonly diff = input.required<FieldDiff>();
  protected readonly formatValue = formatFieldValue;
}
