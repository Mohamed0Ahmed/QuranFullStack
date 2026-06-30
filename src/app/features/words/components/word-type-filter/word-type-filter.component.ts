import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { WORD_TYPES_CASE_FILTER_LABEL, WORD_TYPES_COLLAPSE_LABEL, WORD_TYPES_EXPAND_LABEL, WORD_TYPES_FILTER_LABEL, WORD_TYPES_TENSE_FILTER_LABEL, WORD_TYPES_VOICE_FILTER_LABEL, WORD_TYPE_CASE_LABELS, WORD_TYPE_TENSE_LABELS, WORD_TYPE_VOICE_LABELS } from '../../models/word-types.labels';
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

  protected get caseOptions() {
    return WORD_TYPE_CASES;
  }

  protected get tenseOptions() {
    return WORD_TYPE_TENSES;
  }

  protected get voiceOptions() {
    return WORD_TYPE_VOICES;
  }

  // The active parent node drives secondary-filter visibility. particle and inl expose kind="none"
  // and render no controls; noun renders case; verb renders tense + voice.
  protected readonly activeNode = computed<WordTypeTreeNodeDto | null>(() => {
    const currentTree = this.tree();
    if (!currentTree) {
      return null;
    }
    return currentTree.mainTypes.find((node) => node.code === this.selectedType()) ?? null;
  });

  protected readonly secondaryFilter = computed<WordTypeSecondaryFilterDto | null>(
    () => this.activeNode()?.secondaryFilter ?? null,
  );

  protected readonly showCaseControls = computed(() => this.secondaryFilter()?.kind === 'case');
  protected readonly showVerbControls = computed(() => this.secondaryFilter()?.kind === 'tense+voice');

  // Track which parents are expanded so the user can browse child nodes. A parent is considered
  // expanded when it is the active type OR has been toggled open by the expand affordance.
  private readonly expandedTypes = new Set<WordTypeMainType>();

  protected selectType(node: WordTypeTreeNodeDto): void {
    if (this.loading()) {
      return;
    }

    this.typeSelected.emit(node.code);
  }

  protected selectChild(child: WordTypeChildNodeDto): void {
    if (this.loading()) {
      return;
    }

    this.childSelected.emit(child.childCode);
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

    if (this.expandedTypes.has(node.code)) {
      this.expandedTypes.delete(node.code);
    } else {
      this.expandedTypes.add(node.code);
    }
  }

  protected isExpanded(node: WordTypeTreeNodeDto): boolean {
    // The active parent stays expanded so its selected child stays visible; other parents
    // expand only when explicitly toggled.
    return node.children.length === 0
      ? false
      : this.expandedTypes.has(node.code) || this.isSelected(node);
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

    const action = this.isExpanded(node) ? WORD_TYPES_COLLAPSE_LABEL : WORD_TYPES_EXPAND_LABEL;
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
}
