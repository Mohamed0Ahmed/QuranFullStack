import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  LEMMAS_AYAH_TYPE_ALL_LABEL,
  LEMMAS_AYAH_TYPE_FILTERS_LABEL,
  LEMMAS_LOADING_LABEL,
} from '../../models/lemmas.labels';
import { TypeSummaryDto } from '../../models/lemmas.models';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { WORD_COUNT_DISABLED_REASON } from '../word-count-chip/word-count-chip.component';

let nextDisabledReasonId = 0;

@Component({
  selector: 'qd-lemma-ayah-type-filters',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './lemma-ayah-type-filters.component.html',
  styleUrl: './lemma-ayah-type-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaAyahTypeFiltersComponent {
  readonly items = input.required<readonly TypeSummaryDto[]>();
  readonly selectedTypeCode = input<string | null>(null);
  readonly loading = input(false);

  readonly typeCodeChange = output<string | null>();

  protected readonly sectionLabel = LEMMAS_AYAH_TYPE_FILTERS_LABEL;
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;

  protected readonly isAllSelected = computed(() => this.selectedTypeCode() === null);

  protected readonly loadingChipPlaceholders = [0, 1, 2, 3] as const;
  protected get disabledReason(): string { return WORD_COUNT_DISABLED_REASON; }
  protected readonly disabledReasonId = `lemma-ayah-type-disabled-reason-${nextDisabledReasonId++}`;
  protected readonly hasDisabledItems = computed(() => this.items().some((item) => item.occurrencesCount === 0));

  protected selectTypeCode(typeCode: string | null): void {
    if (typeCode !== null && this.items().find((item) => item.code === typeCode)?.occurrencesCount === 0) {
      return;
    }
    const alreadyActive = typeCode === null ? this.isAllSelected() : this.isSelected(typeCode);
    if (alreadyActive) {
      return;
    }

    this.typeCodeChange.emit(typeCode);
  }

  protected allFilterLabel(): string {
    return LEMMAS_AYAH_TYPE_ALL_LABEL;
  }

  protected isSelected(code: string): boolean {
    if (this.selectedTypeCode() === code) {
      return true;
    }

    return this.items().length === 1 && this.selectedTypeCode() === null && this.items()[0]?.code === code;
  }
}
