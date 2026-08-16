import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { TypeSummaryDto } from '../../../../core/api/generated/models';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';

@Component({
  selector: 'qd-ayah-type-filters',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './ayah-type-filters.component.html',
  styleUrl: './ayah-type-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AyahTypeFiltersComponent {
  readonly items = input.required<readonly TypeSummaryDto[]>();
  readonly selectedTypeCode = input<string | null>(null);
  readonly loading = input(false);

  readonly typeCodeChange = output<string | null>();

  protected readonly sectionLabel = 'تصفية الأنواع في الآيات';
  protected readonly loadingLabel = 'جارٍ التحميل…';
  protected readonly allFilterLabel = 'عرض الكل';
  protected readonly isAllSelected = computed(() => this.selectedTypeCode() === null);
  protected readonly loadingChipPlaceholders = [0, 1, 2, 3] as const;

  protected selectTypeCode(typeCode: string | null): void {
    const alreadyActive = typeCode === null ? this.isAllSelected() : this.isSelected(typeCode);
    if (!alreadyActive) {
      this.typeCodeChange.emit(typeCode);
    }
  }

  protected isSelected(code: string): boolean {
    if (this.selectedTypeCode() === code) {
      return true;
    }

    return this.items().length === 1 && this.selectedTypeCode() === null && this.items()[0]?.code === code;
  }
}
