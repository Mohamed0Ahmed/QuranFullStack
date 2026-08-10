import { Component, ElementRef, computed, input, output, signal, viewChild } from '@angular/core';

import { QdFloatingLayerDirective } from '../../../../shared/ui/floating-layer/floating-layer.directive';
import { MushafSurahJuzGroupDto } from '../../models/mushaf.models';
import {
  dedupeSurahCatalogItems,
  filterUniqueSurahs,
} from '../../utils/surah-jump-catalog.helpers';

let nextSurahJumpInstance = 0;

@Component({
  selector: 'qd-surah-jump-picker',
  standalone: true,
  imports: [QdFloatingLayerDirective],
  templateUrl: './surah-jump-picker.component.html',
  styleUrls: ['./surah-jump-picker.component.scss'],
})
export class SurahJumpPickerComponent {
  readonly searchInputRef = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly surahCatalogByJuz = input.required<readonly MushafSurahJuzGroupDto[]>();

  readonly surahJump = output<number>();

  private readonly instanceId = `surah-jump-${nextSurahJumpInstance++}`;
  protected readonly listboxId = `${this.instanceId}-listbox`;
  protected readonly panelOpen = signal(false);
  protected readonly searchQuery = signal('');

  protected readonly uniqueSurahs = computed(() =>
    dedupeSurahCatalogItems(this.surahCatalogByJuz()),
  );

  protected readonly isSearchMode = computed(() => this.searchQuery().trim().length > 0);

  protected readonly filteredUniqueSurahs = computed(() =>
    filterUniqueSurahs(this.uniqueSurahs(), this.searchQuery()),
  );

  protected readonly isDisabled = computed(() => this.surahCatalogByJuz().length === 0);

  protected togglePanel(): void {
    if (this.isDisabled()) {
      return;
    }

    if (this.panelOpen()) {
      this.closePanel();
      return;
    }

    this.panelOpen.set(true);
    this.searchQuery.set('');
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected selectCursorOption(event: Event): void {
    const cursorId = this.searchInputRef()?.nativeElement.getAttribute('aria-activedescendant');
    if (!cursorId) {
      return;
    }

    const layer = event.currentTarget as HTMLElement;
    const option = Array.from(layer.querySelectorAll<HTMLElement>('[role="option"]')).find(
      (candidate) => candidate.id === cursorId,
    );
    if (!option) {
      return;
    }

    event.preventDefault();
    option.click();
  }

  protected selectSurah(surahNumber: number): void {
    this.surahJump.emit(surahNumber);
    this.closePanel();
  }

  protected closePanel(): void {
    this.panelOpen.set(false);
    this.searchQuery.set('');
  }

  protected formatSurahLabel(surahNumber: number, nameArabic: string): string {
    return `${surahNumber}. ${nameArabic}`;
  }

  protected optionId(juzNumber: number | null, surahNumber: number): string {
    return juzNumber === null
      ? `${this.instanceId}-option-${surahNumber}`
      : `${this.instanceId}-option-j${juzNumber}-s${surahNumber}`;
  }
}
