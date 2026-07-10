import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PageMarkerDto } from '../../models/mushaf.models';

@Component({
  selector: 'qd-mushaf-marker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mushaf-marker.component.html',
  styleUrls: ['./mushaf-marker.component.scss'],
})
export class MushafMarkerComponent {
  readonly marker = input.required<PageMarkerDto>();

  protected readonly label = computed(() => {
    const m = this.marker();
    switch (m.markerType) {
      case 'juz':
        return `جزء ${m.markerNumber}`;
      case 'hizb':
        return `حزب ${m.markerNumber}`;
      case 'rub':
        return `ربع ${m.markerNumber}`;
      case 'sajda':
        return `سجدة ${m.markerNumber}`;
      default:
        return '';
    }
  });
}
