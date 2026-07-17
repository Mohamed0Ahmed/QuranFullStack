import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  LEMMAS_AYAH_TYPE_ALL_LABEL,
  LEMMAS_AYAH_TYPE_FILTERS_LABEL,
  LEMMAS_LOADING_LABEL,
} from '../../models/lemmas.labels';
import { TypeSummaryDto } from '../../models/lemmas.models';

@Component({
  selector: 'qd-lemma-ayah-type-filters',
  standalone: true,
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

  protected selectTypeCode(typeCode: string | null): void {
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
