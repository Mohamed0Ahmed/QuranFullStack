import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { WORD_TYPE_DETAIL_PRESENTATIONS } from '../../models/word-types.labels';
import {
  WORD_TYPE_DETAIL_VIEWS,
  WORD_TYPE_DETAIL_VIEW_KEYS,
  WordTypeDetailView,
} from '../../models/word-types.models';
import { WordTypeDetailSelectionKind } from '../../models/word-types-detail.models';

@Component({
  selector: 'qd-word-type-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './word-type-details-panel.component.html',
  styleUrl: './word-type-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailsPanelComponent {
  readonly view = input.required<WordTypeDetailView>();
  readonly kind = input<WordTypeDetailSelectionKind>('word');
  readonly inline = input(true);
  /**
   * Content-only mode (Feature 029, Change B4): render just the kind-aware view
   * tablist + tabpanel body in a plain wrapper — no card section, no
   * dialog/backdrop, no header/close. Used inside the global detail overlay
   * shell, which owns the dialog chrome (the composer also owns the notFound
   * rendering there). When false, the inline/modal branches behave as before.
   */
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);

  readonly viewChange = output<WordTypeDetailView>();
  readonly close = output<void>();

  protected get panelLabel() { return this.presentation.panelLabel; }
  protected get closeLabel() { return CLOSE_LABEL; }
  protected get emptySelectionLabel() { return this.presentation.emptySelectionLabel; }
  protected get notFoundLabel() { return this.presentation.notFoundLabel; }
  protected readonly surfaceDomId = 'word-type-details-panel-surface';

  private get presentation() { return WORD_TYPE_DETAIL_PRESENTATIONS[this.kind()]; }

  // Word selections expose ayahs/surahs; grouped selections add the leading related-words tab.
  protected readonly tabKeys = computed<readonly WordTypeDetailView[]>(() =>
    this.kind() === 'word' ? WORD_TYPE_DETAIL_VIEW_KEYS : WORD_TYPE_DETAIL_VIEWS,
  );

  protected readonly tabs = computed(() =>
    this.tabKeys().map((key) => ({
      key,
      ...this.presentation.tabs[key],
    })),
  );

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');
  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected tabDomId(key: WordTypeDetailView): string {
    return `word-type-details-tabbtn-${key}`;
  }

  protected isActive(key: WordTypeDetailView): boolean {
    return this.view() === key;
  }

  protected selectView(key: WordTypeDetailView): void {
    if (this.emptySelection() || key === this.view()) {
      return;
    }

    this.viewChange.emit(key);
  }

  protected onEscape(): void {
    if (!this.inline() || this.hasSelection()) {
      this.close.emit();
    }
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close.emit();
    }
  }

  protected onTabKeydown(event: KeyboardEvent, currentKey: WordTypeDetailView): void {
    const order = this.tabKeys();
    const index = order.indexOf(currentKey);
    let nextIndex: number | null = null;

    switch (event.key) {
      case 'ArrowLeft':
        nextIndex = (index + 1) % order.length;
        break;
      case 'ArrowRight':
        nextIndex = (index - 1 + order.length) % order.length;
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = order.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    const nextKey = order[nextIndex!];
    this.selectView(nextKey);
    this.focusTab(nextKey);
  }

  private focusTab(key: WordTypeDetailView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-word-type-tab="${key}"]`);
    tab?.focus();
  }
}
