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
  }

  protected backToLanguages(): void {
    this.panelView.set('languages');
    this.activeLanguage.set(null);
    this.searchQuery.set('');
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
      return;
    }

    const languageGroup = this.resolveLanguageGroupForSelectedKey();
    if (languageGroup) {
      this.activeLanguage.set(languageGroup);
      this.panelView.set('sources');
      return;
    }

    if (this.activeLanguage()) {
      this.panelView.set('sources');
      return;
    }

    this.panelView.set('languages');
  }

  private closePanel(): void {
    this.panelOpen.set(false);
    this.searchQuery.set('');
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
