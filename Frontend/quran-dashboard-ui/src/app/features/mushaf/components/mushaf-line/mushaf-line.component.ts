import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafLineDto, PageMarkerDto } from '../../models/mushaf.models';
import { MushafMarkerComponent } from '../mushaf-marker/mushaf-marker.component';
import { MushafWordComponent } from '../mushaf-word/mushaf-word.component';
import { MUSHAF_BASMALLAH_DISPLAY_TEXT } from './mushaf-basmallah-display-text';
import { mushafCommonLigature } from './mushaf-common-ligature';
import { isVisibleMushafPageMarker } from './mushaf-page-marker-visibility';
import { mushafSurahNameLigature } from './mushaf-surah-name-ligature';

@Component({
  selector: 'qd-mushaf-line',
  standalone: true,
  imports: [CommonModule, MushafWordComponent, MushafMarkerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mushaf-line.component.html',
  styleUrls: ['./mushaf-line.component.scss'],
})
export class MushafLineComponent {
  readonly line = input.required<MushafLineDto>();
  readonly markers = input<PageMarkerDto[]>([]);
  readonly highlightedVerseKey = input<string | null>(null);
  readonly selectedWordLocation = input<string | null>(null);
  readonly surahNameArabic = input<string | null>(null);

  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();

  basmallahText(): string {
    return MUSHAF_BASMALLAH_DISPLAY_TEXT;
  }

  surahHeaderLigature(): string {
    return mushafCommonLigature('surah_header');
  }

  surahNameLigature(surahNumber: number): string {
    return mushafSurahNameLigature(surahNumber) ?? '';
  }

  readonly lineMarkers = computed(() =>
    this.markers()
      .filter((marker) => marker.lineNumber === this.line().lineNumber)
      .filter(isVisibleMushafPageMarker),
  );
}
