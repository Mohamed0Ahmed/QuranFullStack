import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafLineDto, PageMarkerDto } from '../../models/mushaf.models';
import { MushafMarkerComponent } from '../mushaf-marker/mushaf-marker.component';
import { MushafWordComponent } from '../mushaf-word/mushaf-word.component';

@Component({
  selector: 'qd-mushaf-line',
  standalone: true,
  imports: [CommonModule, MushafWordComponent, MushafMarkerComponent],
  templateUrl: './mushaf-line.component.html',
  styleUrls: ['./mushaf-line.component.scss'],
})
export class MushafLineComponent {
  readonly line = input.required<MushafLineDto>();
  readonly markers = input<PageMarkerDto[]>([]);
  readonly selectedVerseKey = input<string | null>(null);

  readonly ayahSelect = output<string>();

  readonly lineMarkers = computed(() =>
    this.markers().filter((marker) => marker.lineNumber === this.line().lineNumber),
  );
}
