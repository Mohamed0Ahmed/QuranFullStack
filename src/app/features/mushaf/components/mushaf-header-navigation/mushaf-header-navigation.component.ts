import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafPageViewModel, MushafSurahCatalogItemDto } from '../../models/mushaf.models';

@Component({
  selector: 'qd-mushaf-header-navigation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mushaf-header-navigation.component.html',
  styleUrls: ['./mushaf-header-navigation.component.scss'],
})
export class MushafHeaderNavigationComponent {
  readonly page = input.required<MushafPageViewModel>();
  readonly surahCatalog = input.required<MushafSurahCatalogItemDto[]>();

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

  protected onSurahSelect(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (!Number.isFinite(value) || value < 1) {
      return;
    }
    this.surahJump.emit(value);
  }
}
