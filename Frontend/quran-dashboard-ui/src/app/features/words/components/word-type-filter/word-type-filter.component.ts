import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, linkedSignal, output } from '@angular/core';

import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { WORD_TYPES_CASE_FILTER_LABEL, WORD_TYPES_CURRENT_FILTER_LABEL, WORD_TYPES_FILTER_LABEL, WORD_TYPES_LOADING_LABEL, WORD_TYPES_NO_SUBTYPES_LABEL, WORD_TYPES_SUBTYPE_GROUP_LABEL, WORD_TYPES_TENSE_FILTER_LABEL, WORD_TYPES_VOICE_FILTER_LABEL, WORD_TYPE_CASE_LABELS, WORD_TYPE_TENSE_LABELS, WORD_TYPE_VOICE_LABELS } from '../../models/word-types.labels';
import {
  WORD_TYPE_CASES,
  WORD_TYPE_MAIN_TYPES,
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

export interface WordTypeScopeSelectedEvent {
  readonly type: WordTypeMainType;
  readonly childCode: string | null;
}

@Component({
  selector: 'qd-word-type-filter',
  standalone: true,
  imports: [ExplorerPanelSkeletonComponent],
  templateUrl: './word-type-filter.component.html',
  styleUrl: './word-type-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeFilterComponent {
  private readonly host = inject(ElementRef<HTMLElement>);

  // TDZ-safe getter, not a readonly field: as a field this const is undefined in the bundled test build.
  protected get skeletonTriggers() {
    return WORD_TYPE_MAIN_TYPES;
  }

  readonly tree = input<WordTypeTreeDto | null>(null);
  readonly selectedType = input<WordTypeMainType>('noun');
  readonly selectedChildCode = input<string | null>(null);
  readonly selectedCase = input<WordTypeCase>('all');
  readonly selectedTense = input<WordTypeTense>('all');
  readonly selectedVoice = input<WordTypeVoice>('all');
  readonly loading = input(false);
  readonly scopeSelected = output<WordTypeScopeSelectedEvent>();
  readonly caseSelected = output<WordTypeCase>();
  readonly tenseSelected = output<WordTypeTense>();
  readonly voiceSelected = output<WordTypeVoice>();

  protected get filterLabel() {
    return WORD_TYPES_FILTER_LABEL;
  }

  protected get loadingLabel() {
    return WORD_TYPES_LOADING_LABEL;
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

  protected readonly browsedType = linkedSignal(() => this.selectedType());

  protected readonly browsedNode = computed<WordTypeTreeNodeDto | null>(() => {
    const currentTree = this.tree();
    if (!currentTree) {
      return null;
    }
    return currentTree.mainTypes.find((node) => node.code === this.browsedType()) ?? null;
  });

  protected readonly secondaryFilter = computed<WordTypeSecondaryFilterDto | null>(
    () => this.browsedType() === this.selectedType()
      ? this.browsedNode()?.secondaryFilter ?? null
      : null,
  );

  protected readonly showCaseControls = computed(() => this.secondaryFilter()?.kind === 'case');
  protected readonly showVerbControls = computed(() => this.secondaryFilter()?.kind === 'tense+voice');

  protected readonly showNoSubtypes = computed(
    () => (this.browsedNode()?.children.length ?? 0) === 0 && this.secondaryFilter()?.kind === 'none',
  );

  protected selectType(node: WordTypeTreeNodeDto): void {
    if (this.loading()) {
      return;
    }

    if (node.children.length === 0) {
      this.scopeSelected.emit({ type: node.code, childCode: null });
      return;
    }

    this.browsedType.set(node.code);
  }

  protected selectChild(child: WordTypeChildNodeDto): void {
    if (this.loading()) {
      return;
    }

    const type = this.browsedNode()?.code;
    if (type) {
      this.scopeSelected.emit({ type, childCode: child.childCode });
    }
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

  protected isSelected(node: WordTypeTreeNodeDto): boolean {
    return node.code === this.selectedType();
  }

  protected isBrowsed(node: WordTypeTreeNodeDto): boolean {
    return node.code === this.browsedType();
  }

  protected isChildSelected(child: WordTypeChildNodeDto): boolean {
    return this.browsedType() === this.selectedType()
      && child.childCode === this.selectedChildCode();
  }

  protected get noSubtypesLabel() {
    return WORD_TYPES_NO_SUBTYPES_LABEL;
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

  focusSelectedType(): void {
    const host = this.host.nativeElement as HTMLElement;
    const selected = host.querySelector<HTMLButtonElement>('.word-type-filter__button[aria-current="true"]');
    const fallback = host.querySelector<HTMLButtonElement>('.word-type-filter__button');
    (selected ?? fallback)?.focus();
  }
}
