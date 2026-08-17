import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LinkingSourceTypeOption } from '../../models/linking-source.models';
import { normalizeSelectedSourceTypeCodes } from '../../utils/linking-source-types';

@Component({
  selector: 'qd-linking-source-type-filters',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './linking-source-type-filters.component.html',
  styleUrl: './linking-source-type-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingSourceTypeFiltersComponent {
  readonly items = input.required<readonly LinkingSourceTypeOption[]>();
  readonly selectedTypeCodes = input<readonly string[]>([]);
  readonly viewTypeCode = input<string | null>(null);
  readonly disabled = input(false);
  readonly typeCodesChange = output<readonly string[]>();
  readonly viewTypeCodeChange = output<string | null>();

  protected readonly sectionLabel = 'عرض الآيات وتحديد أنواع الكلمات التي تدخل في الربط';
  protected readonly allLabel = 'الكل';
  protected readonly inclusionLabel = 'ضمن الربط';
  protected readonly isAllIncluded = computed(() => this.selectedTypeCodes().length === 0);

  protected isVisible(code: string): boolean {
    return this.viewTypeCode() === code;
  }

  protected isIncluded(code: string): boolean {
    return this.isAllIncluded() || this.selectedTypeCodes().includes(code);
  }

  protected isOnlyIncluded(code: string): boolean {
    const selected = this.selectedTypeCodes();
    return selected.length === 1 && selected[0] === code;
  }

  protected showAll(): void {
    if (this.viewTypeCode() !== null) {
      this.viewTypeCodeChange.emit(null);
    }
  }

  protected showType(code: string): void {
    if (!this.isVisible(code)) {
      this.viewTypeCodeChange.emit(code);
    }
  }

  protected includeAll(): void {
    if (!this.disabled() && !this.isAllIncluded()) {
      this.typeCodesChange.emit([]);
    }
  }

  protected toggleIncluded(code: string): void {
    if (this.disabled()) {
      return;
    }
    const selected = this.isAllIncluded()
      ? new Set(this.items().map((item) => item.code))
      : new Set(this.selectedTypeCodes());
    selected.has(code) ? selected.delete(code) : selected.add(code);
    if (selected.size === 0) {
      return;
    }
    this.typeCodesChange.emit(normalizeSelectedSourceTypeCodes([...selected], this.items()));
  }
}
