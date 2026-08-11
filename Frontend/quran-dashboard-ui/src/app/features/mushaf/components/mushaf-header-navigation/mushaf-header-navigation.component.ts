import { Component, ElementRef, input, output, viewChild } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { SurahJumpPickerComponent } from '../surah-jump-picker/surah-jump-picker.component';
import { MushafPageViewModel, MushafSurahJuzGroupDto } from '../../models/mushaf.models';

@Component({
  selector: 'qd-mushaf-header-navigation',
  standalone: true,
  imports: [QdActionDirective, SurahJumpPickerComponent],
  templateUrl: './mushaf-header-navigation.component.html',
  styleUrls: ['./mushaf-header-navigation.component.scss'],
})
export class MushafHeaderNavigationComponent {
  readonly page = input.required<MushafPageViewModel>();
  readonly surahCatalogByJuz = input.required<readonly MushafSurahJuzGroupDto[]>();
  readonly canSelectAyahs = input(false);
  readonly ayahSelectionMode = input(false);

  readonly pageChange = output<number>();
  readonly surahJump = output<number>();
  readonly ayahSelectionModeChange = output<void>();
  private readonly ayahSelectionButton = viewChild<ElementRef<HTMLButtonElement>>('ayahSelectionButton');

  focusAyahSelectionAction(): void {
    this.ayahSelectionButton()?.nativeElement.focus();
  }

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

  protected toggleAyahSelectionMode(): void {
    this.ayahSelectionModeChange.emit();
  }
}
