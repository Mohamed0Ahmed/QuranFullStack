import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafPageViewModel, MushafSurahJuzGroupDto, ResourceLoadState } from '../../models/mushaf.models';
import { MushafHeaderNavigationComponent } from '../mushaf-header-navigation/mushaf-header-navigation.component';
import { MushafPageViewComponent } from '../mushaf-page-view/mushaf-page-view.component';

@Component({
  selector: 'qd-mushaf-page-area',
  standalone: true,
  imports: [CommonModule, MushafHeaderNavigationComponent, MushafPageViewComponent],
  templateUrl: './mushaf-page-area.component.html',
  styleUrls: ['./mushaf-page-area.component.scss'],
})
export class MushafPageAreaComponent {
  readonly page = input<MushafPageViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly surahCatalogByJuz = input.required<readonly MushafSurahJuzGroupDto[]>();
  readonly selectedVerseKey = input<string | null>(null);
  readonly selectedWordLocation = input<string | null>(null);

  readonly pageChange = output<number>();
  readonly surahJump = output<number>();
  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();
}
