import { ChangeDetectionStrategy, Component, ElementRef, HostListener, computed, inject, input, output, signal } from '@angular/core';

import { WORD_TYPES_CASE_FILTER_LABEL, WORD_TYPES_COLLAPSE_LABEL, WORD_TYPES_CURRENT_FILTER_LABEL, WORD_TYPES_EXPAND_LABEL, WORD_TYPES_FILTER_LABEL, WORD_TYPES_SUBTYPE_GROUP_LABEL, WORD_TYPES_TENSE_FILTER_LABEL, WORD_TYPES_VOICE_FILTER_LABEL, WORD_TYPE_CASE_LABELS, WORD_TYPE_TENSE_LABELS, WORD_TYPE_VOICE_LABELS } from '../../models/word-types.labels';
import {
  WORD_TYPE_CASES,
  WORD_TYPE_TENSES,
  WORD_TYPE_VOICES,
  WordTypeCase,
  WordTypeChildNodeDto,
  WordTypeMainType,
  WordTypeSecondaryFilterDto,
  WordTypeTense,
  WordTypeTreeDto,
  WordTypeTreeNodeDto,
  WordTypeVoice,
} from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-filter',
  standalone: true,
  templateUrl: './word-type-filter.component.html',
  styleUrl: './word-type-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeFilterComponent {
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly tree = input<WordTypeTreeDto | null>(null);
  readonly selectedType = input<WordTypeMainType>('noun');
  readonly selectedChildCode = input<string | null>(null);
  readonly selectedCase = input<WordTypeCase>('all');
  readonly selectedTense = input<WordTypeTense>('all');
  readonly selectedVoice = input<WordTypeVoice>('all');
  readonly loading = input(false);
  readonly typeSelected = output<WordTypeMainType>();
  readonly childSelected = output<string | null>();
  readonly caseSelected = output<WordTypeCase>();
  readonly tenseSelected = output<WordTypeTense>();
  readonly voiceSelected = output<WordTypeVoice>();
  protected readonly openPanelType = signal<WordTypeMainType | null>(null);

  protected get filterLabel() {
    return WORD_TYPES_FILTER_LABEL;
  }

  protected get caseFilterLabel() {
    return WORD_TYPES_CASE_FILTER_LABEL;
  }

  protected get tenseFilterLabel() {
    return WORD_TYPES_TENSE_FILTER_LABEL;
  }

  protected get voiceFilterLabel() {
    return WORD_TYPES_VOICE_FILTER_LABEL;
  }

  protected get subtypeGroupLabel() {
    return WORD_TYPES_SUBTYPE_GROUP_LABEL;
  }

  protected get currentFilterLabel() {
    return WORD_TYPES_CURRENT_FILTER_LABEL;
  }

  protected get caseOptions() {
    return WORD_TYPE_CASES;
  }

  protected get tenseOptions() {
    return WORD_TYPE_TENSES;
  }

  protected get voiceOptions() {
    return WORD_TYPE_VOICES;
  }

  // Open panel drives secondary-filter visibility. particle and inl expose kind="none"
  // and render no controls; noun renders case; verb renders tense + voice.
  protected readonly openNode = computed<WordTypeTreeNodeDto | null>(() => {
    const currentTree = this.tree();
    if (!currentTree) {
      return null;
    }
    return currentTree.mainTypes.find((node) => node.code === this.openPanelType()) ?? null;
  });

  protected readonly secondaryFilter = computed<WordTypeSecondaryFilterDto | null>(
    () => this.openNode()?.secondaryFilter ?? null,
  );

  protected readonly showCaseControls = computed(() => this.secondaryFilter()?.kind === 'case');
  protected readonly showVerbControls = computed(() => this.secondaryFilter()?.kind === 'tense+voice');

  protected selectType(node: WordTypeTreeNodeDto): void {
    if (this.loading()) {
      return;
    }

    this.typeSelected.emit(node.code);
    this.openPanelType.set(node.children.length > 0 ? node.code : null);
  }

  protected selectChild(child: WordTypeChildNodeDto): void {
    if (this.loading()) {
      return;
    }

    this.childSelected.emit(child.childCode);
    this.closePanel(this.openPanelType());
  }

  protected changeCase(event: Event): void {
    if (this.loading()) {
      return;
    }
    this.caseSelected.emit((event.target as HTMLSelectElement).value as WordTypeCase);
  }

  protected changeTense(event: Event): void {
    if (this.loading()) {
      return;
    }
    this.tenseSelected.emit((event.target as HTMLSelectElement).value as WordTypeTense);
  }

  protected changeVoice(event: Event): void {
    if (this.loading()) {
      return;
    }
    this.voiceSelected.emit((event.target as HTMLSelectElement).value as WordTypeVoice);
  }

  protected toggleExpand(node: WordTypeTreeNodeDto, event: Event): void {
    event.stopPropagation();
    if (node.children.length === 0) {
      return;
    }

    if (this.openPanelType() === node.code) {
      this.closePanel(node.code);
      return;
    }

    this.openPanelType.set(node.code);
  }

  protected isPanelOpen(node: WordTypeTreeNodeDto): boolean {
    return node.children.length > 0 && this.openPanelType() === node.code;
  }

  protected isSelected(node: WordTypeTreeNodeDto): boolean {
    return node.code === this.selectedType();
  }

  protected isChildSelected(child: WordTypeChildNodeDto): boolean {
    return child.childCode === this.selectedChildCode();
  }

  protected expandAriaLabel(node: WordTypeTreeNodeDto): string {
    if (node.children.length === 0) {
      return '';
    }

    const action = this.isPanelOpen(node) ? WORD_TYPES_COLLAPSE_LABEL : WORD_TYPES_EXPAND_LABEL;
    return `${action} ${node.label.ar}`;
  }

  protected caseOptionLabel(option: WordTypeCase): string {
    return WORD_TYPE_CASE_LABELS[option];
  }

  protected tenseOptionLabel(option: WordTypeTense): string {
    return WORD_TYPE_TENSE_LABELS[option];
  }

  protected voiceOptionLabel(option: WordTypeVoice): string {
    return WORD_TYPE_VOICE_LABELS[option];
  }

  protected panelRegionLabel(node: WordTypeTreeNodeDto): string {
    return node.label.ar;
  }

  private closePanel(focusType: WordTypeMainType | null): void {
    this.openPanelType.set(null);
    this.restorePanelFocus(focusType);
  }

  private restorePanelFocus(focusType: WordTypeMainType | null): void {
    if (!focusType) {
      return;
    }

    const host = this.host.nativeElement as HTMLElement;
    const trigger = host.querySelector<HTMLButtonElement>(`button.word-type-filter__expand[data-word-type-code="${focusType}"]`)
      ?? host.querySelector<HTMLButtonElement>(`button.word-type-filter__button[data-word-type-code="${focusType}"]`);
    trigger?.focus();
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    const target = event.target;
    if (!(target instanceof Node)) {
      return;
    }

    if (this.host.nativeElement.contains(target)) {
      return;
    }

    this.closePanel(this.openPanelType());
  }

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Escape' || this.openPanelType() === null) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    this.closePanel(this.openPanelType());
  }

  focusSelectedType(): void {
    const host = this.host.nativeElement as HTMLElement;
    const selected = host.querySelector<HTMLButtonElement>('.word-type-filter__button[aria-current="true"]');
    const fallback = host.querySelector<HTMLButtonElement>('.word-type-filter__button');
    (selected ?? fallback)?.focus();
  }
}
