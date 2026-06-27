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
  protected readonly allLabel = LEMMAS_AYAH_TYPE_ALL_LABEL;
  protected readonly loadingLabel = LEMMAS_LOADING_LABEL;

  protected readonly isAllSelected = computed(() => this.selectedTypeCode() === null);

  protected selectTypeCode(typeCode: string | null): void {
    this.typeCodeChange.emit(typeCode);
  }

  protected isSelected(code: string): boolean {
    return this.selectedTypeCode() === code;
  }
}
