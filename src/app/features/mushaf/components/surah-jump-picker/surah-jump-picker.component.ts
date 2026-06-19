import {
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { MushafSurahJuzGroupDto } from '../../models/mushaf.models';
import {
  dedupeSurahCatalogItems,
  filterUniqueSurahs,
} from '../../utils/surah-jump-catalog.helpers';

const LISTBOX_ID = 'surah-jump-listbox';
const PANEL_VIEWPORT_PADDING_PX = 8;
const PANEL_DEFAULT_MAX_PX = 350;
const PANEL_MAX_HEIGHT_VAR = '--surah-jump-picker-panel-max-height';

interface SurahListboxOption {
  id: string;
  surahNumber: number;
}

@Component({
  selector: 'qd-surah-jump-picker',
  standalone: true,
  templateUrl: './surah-jump-picker.component.html',
  styleUrls: ['./surah-jump-picker.component.scss'],
})
export class SurahJumpPickerComponent {
  private readonly elementRef = inject(ElementRef);

  readonly triggerRef = viewChild<ElementRef<HTMLButtonElement>>('trigger');
  readonly panelRef = viewChild<ElementRef<HTMLElement>>('panel');
  readonly searchInputRef = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly surahCatalogByJuz = input.required<readonly MushafSurahJuzGroupDto[]>();

  readonly surahJump = output<number>();

  protected readonly listboxId = LISTBOX_ID;
  protected readonly panelOpen = signal(false);
  protected readonly searchQuery = signal('');
  protected readonly activeOptionIndex = signal(-1);

  protected readonly uniqueSurahs = computed(() =>
    dedupeSurahCatalogItems(this.surahCatalogByJuz()),
  );

  protected readonly isSearchMode = computed(() => this.searchQuery().trim().length > 0);

  protected readonly filteredUniqueSurahs = computed(() =>
    filterUniqueSurahs(this.uniqueSurahs(), this.searchQuery()),
  );

  protected readonly isDisabled = computed(() => this.surahCatalogByJuz().length === 0);

  protected readonly listboxOptions = computed((): SurahListboxOption[] => {
    if (this.isSearchMode()) {
      return this.filteredUniqueSurahs().map((surah) => ({
        id: this.optionId(null, surah.surahNumber),
        surahNumber: surah.surahNumber,
      }));
    }

    const options: SurahListboxOption[] = [];
    for (const group of this.surahCatalogByJuz()) {
      for (const surah of group.surahs) {
        options.push({
          id: this.optionId(group.juzNumber, surah.surahNumber),
          surahNumber: surah.surahNumber,
        });
      }
    }

    return options;
  });

  protected readonly activeOptionId = computed(() => {
    const index = this.activeOptionIndex();
    const options = this.listboxOptions();
    if (index < 0 || index >= options.length) {
      return null;
    }

    return options[index].id;
  });

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' && this.panelOpen()) {
      event.preventDefault();
      this.closePanel();
      return;
    }

    if (!this.panelOpen()) {
      return;
    }

    const options = this.listboxOptions();

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.moveActiveOption(1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.moveActiveOption(-1);
        break;
      case 'Home':
        if (options.length === 0) {
          return;
        }
        event.preventDefault();
        this.activeOptionIndex.set(0);
        this.scrollActiveOptionIntoView();
        break;
      case 'End':
        if (options.length === 0) {
          return;
        }
        event.preventDefault();
        this.activeOptionIndex.set(options.length - 1);
        this.scrollActiveOptionIntoView();
        break;
      case 'Enter':
        if (options.length === 0) {
          return;
        }
        event.preventDefault();
        this.selectActiveOption();
        break;
    }
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.panelOpen()) {
      return;
    }

    const root = this.elementRef.nativeElement as HTMLElement;
    const target = event.target as HTMLElement | null;
    if (target && !root.contains(target)) {
      this.closePanel();
    }
  }

  @HostListener('window:scroll')
  @HostListener('window:resize')
  protected onViewportChange(): void {
    if (this.panelOpen()) {
      requestAnimationFrame(() => this.applyPanelMaxHeight());
    }
  }

  protected togglePanel(): void {
    if (this.isDisabled()) {
      return;
    }

    if (this.panelOpen()) {
      this.closePanel();
      return;
    }

    this.openPanel();
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
    this.resetActiveOption();
  }

  protected selectSurah(surahNumber: number): void {
    this.surahJump.emit(surahNumber);
    this.closePanel();
  }

  protected formatSurahLabel(surahNumber: number, nameArabic: string): string {
    return `${surahNumber}. ${nameArabic}`;
  }

  protected optionId(juzNumber: number | null, surahNumber: number): string {
    return juzNumber === null
      ? `surah-jump-option-${surahNumber}`
      : `surah-jump-option-j${juzNumber}-s${surahNumber}`;
  }

  protected isOptionActive(juzNumber: number | null, surahNumber: number): boolean {
    return this.activeOptionId() === this.optionId(juzNumber, surahNumber);
  }

  private openPanel(): void {
    this.panelOpen.set(true);
    this.searchQuery.set('');
    this.resetActiveOption();
    requestAnimationFrame(() => {
      this.applyPanelMaxHeight();
      this.searchInputRef()?.nativeElement.focus();
    });
  }

  private closePanel(): void {
    this.panelOpen.set(false);
    this.searchQuery.set('');
    this.activeOptionIndex.set(-1);
    this.panelRef()?.nativeElement.style.removeProperty(PANEL_MAX_HEIGHT_VAR);
    this.triggerRef()?.nativeElement.focus();
  }

  private applyPanelMaxHeight(): void {
    const trigger = this.triggerRef()?.nativeElement;
    const panel = this.panelRef()?.nativeElement;
    if (!trigger || !panel) {
      return;
    }

    const triggerRect = trigger.getBoundingClientRect();
    const availableBelowTrigger =
      window.innerHeight - triggerRect.bottom - PANEL_VIEWPORT_PADDING_PX;

    const maxHeight = Math.min(
      PANEL_DEFAULT_MAX_PX,
      Math.max(120, availableBelowTrigger),
    );

    panel.style.setProperty(PANEL_MAX_HEIGHT_VAR, `${maxHeight}px`);
  }

  private resetActiveOption(): void {
    this.activeOptionIndex.set(this.listboxOptions().length > 0 ? 0 : -1);
  }

  private moveActiveOption(delta: number): void {
    const options = this.listboxOptions();
    if (options.length === 0) {
      return;
    }

    const current = this.activeOptionIndex();
    const next =
      current < 0
        ? delta > 0
          ? 0
          : options.length - 1
        : Math.max(0, Math.min(options.length - 1, current + delta));

    this.activeOptionIndex.set(next);
    this.scrollActiveOptionIntoView();
  }

  private selectActiveOption(): void {
    const index = this.activeOptionIndex();
    const option = this.listboxOptions()[index];
    if (!option) {
      return;
    }

    this.selectSurah(option.surahNumber);
  }

  private scrollActiveOptionIntoView(): void {
    const activeId = this.activeOptionId();
    if (!activeId) {
      return;
    }

    const element = document.getElementById(activeId);
    element?.scrollIntoView?.({ block: 'nearest' });
  }
}
