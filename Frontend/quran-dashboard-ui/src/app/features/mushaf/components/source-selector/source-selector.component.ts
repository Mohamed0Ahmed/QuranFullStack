import {
  Component,
  computed,
  ElementRef,
  HostListener,
  inject,
  input,
  output,
  signal,
} from '@angular/core';

import { SourceOption } from '../../models/mushaf.models';
import {
  filterLanguageGroups,
  filterSourceOptions,
  findSourceOption,
  groupSourceOptionsByLanguage,
  LanguageSourceGroup,
} from '../../utils/study-source-catalog.groups';
import {
  SOURCE_SELECTOR_PANEL_MAX_HEIGHT_VAR,
  SOURCE_SELECTOR_PANEL_MIN_HEIGHT_PX,
  SOURCE_SELECTOR_PANEL_VIEWPORT_PADDING_PX,
} from './source-selector-panel.constants';

type PickerMode = 'languageFirst' | 'flat';
type PanelView = 'languages' | 'sources';

@Component({
  selector: 'qd-source-selector',
  standalone: true,
  templateUrl: './source-selector.component.html',
  styleUrls: ['./source-selector.component.scss'],
})
export class SourceSelectorComponent {
  private readonly elementRef = inject(ElementRef);

  readonly label = input.required<string>();
  readonly selectedKey = input<string | null>(null);
  readonly usedLabel = input<string | null>(null);
  readonly options = input<SourceOption[]>([]);
  readonly pickerMode = input<PickerMode>('languageFirst');

  readonly sourceChange = output<string>();

  protected readonly panelOpen = signal(false);
  protected readonly panelView = signal<PanelView>('languages');
  protected readonly activeLanguage = signal<LanguageSourceGroup | null>(null);
  protected readonly searchQuery = signal('');

  protected readonly languageGroups = computed(() =>
    groupSourceOptionsByLanguage(this.options()),
  );

  protected readonly filteredLanguageGroups = computed(() =>
    filterLanguageGroups(this.languageGroups(), this.searchQuery()),
  );

  protected readonly filteredSourceOptions = computed(() => {
    const options =
      this.pickerMode() === 'flat'
        ? this.options()
        : (this.activeLanguage()?.options ?? []);

    return filterSourceOptions(options, this.searchQuery());
  });

  protected readonly resolvedSelectedLabel = computed(() => {
    const selected = findSourceOption(this.options(), this.selectedKey());
    if (selected) {
      return selected.label;
    }

    return this.usedLabel();
  });

  protected readonly triggerPlaceholder = computed(() =>
    this.pickerMode() === 'flat' ? 'اختر المصدر' : 'اختر اللغة ثم المصدر',
  );

  protected readonly showPicker = computed(() => this.options().length > 1);

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.panelOpen()) {
      this.closePanel();
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

  protected togglePanel(): void {
    if (this.panelOpen()) {
      this.closePanel();
      return;
    }

    this.openPanel();
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected selectLanguage(group: LanguageSourceGroup): void {
    this.activeLanguage.set(group);
    this.panelView.set('sources');
    this.searchQuery.set('');
    this.schedulePanelLayout();
  }

  protected backToLanguages(): void {
    this.panelView.set('languages');
    this.activeLanguage.set(null);
    this.searchQuery.set('');
    this.schedulePanelLayout();
  }

  protected selectSource(key: string): void {
    this.sourceChange.emit(key);
    this.closePanel();
  }

  private openPanel(): void {
    this.panelOpen.set(true);
    this.searchQuery.set('');

    if (this.pickerMode() === 'flat') {
      this.panelView.set('sources');
      this.schedulePanelLayout();
      return;
    }

    const languageGroup = this.resolveLanguageGroupForSelectedKey();
    if (languageGroup) {
      this.activeLanguage.set(languageGroup);
      this.panelView.set('sources');
      this.schedulePanelLayout();
      return;
    }

    if (this.activeLanguage()) {
      this.panelView.set('sources');
      this.schedulePanelLayout();
      return;
    }

    this.panelView.set('languages');
    this.schedulePanelLayout();
  }

  private closePanel(): void {
    this.panelOpen.set(false);
    this.searchQuery.set('');
    this.clearPanelMaxHeight();
  }

  private schedulePanelLayout(): void {
    requestAnimationFrame(() => {
      this.fitPanelToViewport();
      this.focusPanelSearch();
    });
  }

  private fitPanelToViewport(): void {
    const panel = this.queryPanelElement();
    if (!panel) {
      return;
    }

    const top = panel.getBoundingClientRect().top;
    const available = window.innerHeight - top - SOURCE_SELECTOR_PANEL_VIEWPORT_PADDING_PX;
    const maxHeight = Math.max(SOURCE_SELECTOR_PANEL_MIN_HEIGHT_PX, available);
    panel.style.setProperty(SOURCE_SELECTOR_PANEL_MAX_HEIGHT_VAR, `${maxHeight}px`);
  }

  private clearPanelMaxHeight(): void {
    this.queryPanelElement()?.style.removeProperty(SOURCE_SELECTOR_PANEL_MAX_HEIGHT_VAR);
  }

  private focusPanelSearch(): void {
    const root = this.elementRef.nativeElement as HTMLElement;
    const search =
      (root.querySelector('[data-testid="source-selector-language-search"]') as HTMLInputElement | null) ??
      (root.querySelector('[data-testid="source-selector-source-search"]') as HTMLInputElement | null);

    search?.focus();
  }

  private queryPanelElement(): HTMLElement | null {
    return this.elementRef.nativeElement.querySelector(
      '[data-testid="source-selector-panel"]',
    ) as HTMLElement | null;
  }

  private resolveLanguageGroupForSelectedKey(): LanguageSourceGroup | null {
    const selected = findSourceOption(this.options(), this.selectedKey());
    if (!selected) {
      return null;
    }

    const languageKey = selected.languageCode ?? selected.languageNameAr ?? selected.key;
    return (
      this.languageGroups().find(
        (group) =>
          group.languageCode === languageKey || group.languageNameAr === selected.languageNameAr,
      ) ?? null
    );
  }
}
