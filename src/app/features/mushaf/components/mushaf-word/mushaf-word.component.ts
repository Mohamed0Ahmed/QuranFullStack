import { Component, input } from '@angular/core';

import { MushafWordDto } from '../../models/mushaf.models';

@Component({
  selector: 'qd-mushaf-word',
  standalone: true,
  templateUrl: './mushaf-word.component.html',
  styleUrls: ['./mushaf-word.component.scss'],
})
export class MushafWordComponent {
  readonly word = input.required<MushafWordDto>();
}
