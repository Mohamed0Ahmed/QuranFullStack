import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafPageViewModel } from '../../models/mushaf.models';
import { clampMushafPageNumber } from '../../state/mushaf-url-sync';
import { MushafLineComponent } from '../mushaf-line/mushaf-line.component';
import {
  MushafPageSwipeDirection,
  MushafPageSwipeDirective,
} from './mushaf-page-swipe.directive';
import { mushafJuzNumberLigature } from '../mushaf-line/mushaf-juz-number-ligature';
import {
  mushafSurahIconLigature,
  mushafSurahNameLigature,
} from '../mushaf-line/mushaf-surah-name-ligature';

@Component({
  selector: 'qd-mushaf-page-view',
  standalone: true,
  imports: [CommonModule, MushafLineComponent, MushafPageSwipeDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mushaf-page-view.component.html',
  styleUrls: ['./mushaf-page-view.component.scss'],
})
export class MushafPageViewComponent {
  readonly page = input.required<MushafPageViewModel>();
  readonly highlightedVerseKey = input<string | null>(null);
  readonly selectedWordLocation = input<string | null>(null);
  readonly ayahSelectionMode = input(false);
  readonly selectedVerseKeys = input<readonly string[]>([]);

  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();
  readonly pageChange = output<number>();

  private readonly pageJumpInputRef = viewChild<ElementRef<HTMLInputElement>>('pageJumpInput');

  protected readonly isPageEditing = signal(false);
  protected readonly pageDraft = signal('');

  protected readonly displaySurahs = computed(() => this.page().surahs);

  protected readonly displayJuz = computed(() => {
    const juzNumbers = this.page().navigation.juzNumbers;
    return juzNumbers.length > 0 ? juzNumbers[juzNumbers.length - 1] : null;
  });

  protected readonly displayLines = computed(() => {
    const page = this.page();
    const surahNames = new Map(page.surahs.map((surah) => [surah.surahNumber, surah.nameArabic]));
    return page.lines.map((line) => ({
      line,
      surahNameArabic:
        line.surahNumber === null ? null : (surahNames.get(line.surahNumber) ?? null),
    }));
  });

  protected surahLigature(surahNumber: number): string {
    return mushafSurahNameLigature(surahNumber) ?? '';
  }

  protected surahIconLigature(): string {
    return mushafSurahIconLigature();
  }

  protected displaySurahNamesArabic(): string {
    return this.displaySurahs()
      .map((surah) => surah.nameArabic)
      .join('، ');
  }

  protected juzNumberLigature(juzNumber: number): string {
    return mushafJuzNumberLigature(juzNumber) ?? '';
  }

  protected onPageSwipe(direction: MushafPageSwipeDirection): void {
    const page = this.page();
    const targetPage = direction === 'next' ? page.nextPageNumber : page.previousPageNumber;
    if (targetPage !== null) {
      this.pageChange.emit(targetPage);
    }
  }

  protected startPageEdit(): void {
    this.pageDraft.set(String(this.page().pageNumber));
    this.isPageEditing.set(true);

    queueMicrotask(() => {
      const input = this.pageJumpInputRef()?.nativeElement;
      if (!input) {
        return;
      }

      input.focus();
      input.select();
    });
  }

  protected onPageDraftInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.pageDraft.set(value);
  }

  protected onPageInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.commitPageEdit();
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      this.cancelPageEdit();
    }
  }

  protected onPageInputBlur(): void {
    this.cancelPageEdit();
  }

  protected commitPageEdit(): void {
    const trimmed = this.pageDraft().trim();
    if (!trimmed) {
      this.cancelPageEdit();
      return;
    }

    const targetPage = clampMushafPageNumber(trimmed);
    this.isPageEditing.set(false);

    if (targetPage !== this.page().pageNumber) {
      this.pageChange.emit(targetPage);
    }
  }

  protected cancelPageEdit(): void {
    this.isPageEditing.set(false);
    this.pageDraft.set('');
  }
}
