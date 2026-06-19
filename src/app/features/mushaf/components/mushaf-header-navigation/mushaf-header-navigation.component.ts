import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SurahJumpPickerComponent } from '../surah-jump-picker/surah-jump-picker.component';
import { MushafPageViewModel, MushafSurahJuzGroupDto, SurahOnPageDto } from '../../models/mushaf.models';

@Component({
  selector: 'qd-mushaf-header-navigation',
  standalone: true,
  imports: [CommonModule, SurahJumpPickerComponent],
  templateUrl: './mushaf-header-navigation.component.html',
  styleUrls: ['./mushaf-header-navigation.component.scss'],
})
export class MushafHeaderNavigationComponent {
  readonly page = input.required<MushafPageViewModel>();
  readonly surahCatalogByJuz = input.required<readonly MushafSurahJuzGroupDto[]>();

  readonly pageChange = output<number>();
  readonly surahJump = output<number>();

  protected onPrevious(): void {
    const previous = this.page().previousPageNumber;
    if (previous !== null) {
      this.pageChange.emit(previous);
    }
  }

  protected onNext(): void {
    const next = this.page().nextPageNumber;
    if (next !== null) {
      this.pageChange.emit(next);
    }
  }

  protected onSurahJump(surahNumber: number): void {
    this.surahJump.emit(surahNumber);
  }

  protected formatPageSurahNames(surahs: SurahOnPageDto[]): string {
    return surahs.map((surah) => surah.nameArabic).join('، ');
  }
}
