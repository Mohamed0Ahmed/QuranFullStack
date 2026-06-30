import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { WORD_TYPES_COLLAPSE_LABEL, WORD_TYPES_EXPAND_LABEL, WORD_TYPES_FILTER_LABEL } from '../../models/word-types.labels';
import { WordTypeChildNodeDto, WordTypeMainType, WordTypeTreeDto, WordTypeTreeNodeDto } from '../../models/word-types.models';

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
  readonly loading = input(false);
  readonly typeSelected = output<WordTypeMainType>();
  readonly childSelected = output<string | null>();

  protected readonly filterLabel = WORD_TYPES_FILTER_LABEL;

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
}
