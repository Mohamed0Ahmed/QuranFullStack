import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  STEMS_AYAH_TYPE_ALL_LABEL,
  STEMS_AYAH_TYPE_FILTERS_LABEL,
  STEMS_LOADING_LABEL,
} from '../../models/stems.labels';
import { TypeSummaryDto } from '../../models/stems.models';

@Component({
  selector: 'qd-stem-ayah-type-filters',
  standalone: true,
  templateUrl: './stem-ayah-type-filters.component.html',
  styleUrl: './stem-ayah-type-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemAyahTypeFiltersComponent {
  readonly items = input.required<readonly TypeSummaryDto[]>();
  readonly selectedTypeCode = input<string | null>(null);
  readonly loading = input(false);

  readonly typeCodeChange = output<string | null>();

  protected readonly sectionLabel = STEMS_AYAH_TYPE_FILTERS_LABEL;
  protected readonly loadingLabel = STEMS_LOADING_LABEL;

  protected readonly isAllSelected = computed(() => this.selectedTypeCode() === null);

  protected readonly loadingChipPlaceholders = [0, 1, 2, 3] as const;

  protected selectTypeCode(typeCode: string | null): void {
    this.typeCodeChange.emit(typeCode);
  }

  protected allFilterLabel(): string {
    return STEMS_AYAH_TYPE_ALL_LABEL;
  }

  protected isSelected(code: string): boolean {
    if (this.selectedTypeCode() === code) {
      return true;
    }

    return this.items().length === 1 && this.selectedTypeCode() === null && this.items()[0]?.code === code;
  }
}
