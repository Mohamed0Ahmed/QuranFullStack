import { Component, input, output } from '@angular/core';

import { SourceOption } from '../../models/mushaf.models';

@Component({
  selector: 'qd-source-selector',
  standalone: true,
  templateUrl: './source-selector.component.html',
  styleUrls: ['./source-selector.component.scss'],
})
export class SourceSelectorComponent {
  readonly label = input.required<string>();
  readonly selectedKey = input<string | null>(null);
  readonly usedLabel = input<string | null>(null);
  readonly options = input<SourceOption[]>([]);

  readonly sourceChange = output<string>();
}
