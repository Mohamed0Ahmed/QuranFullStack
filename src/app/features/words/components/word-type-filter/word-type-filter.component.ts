import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { WORD_TYPES_FILTER_LABEL } from '../../models/word-types.labels';
import { WordTypeMainType, WordTypeTreeDto, WordTypeTreeNodeDto } from '../../models/word-types.models';

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
  readonly loading = input(false);
  readonly typeSelected = output<WordTypeMainType>();

  protected readonly filterLabel = WORD_TYPES_FILTER_LABEL;

  protected selectType(node: WordTypeTreeNodeDto): void {
    if (this.loading()) {
      return;
    }

    this.typeSelected.emit(node.code);
  }

  protected isSelected(node: WordTypeTreeNodeDto): boolean {
    return node.code === this.selectedType();
  }
}
