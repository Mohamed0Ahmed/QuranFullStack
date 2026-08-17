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
  readonly disabled = input(false);
  readonly typeCodesChange = output<readonly string[]>();

  protected readonly sectionLabel = 'تحديد أنواع الكلمات التي تدخل في الربط';
  protected readonly allLabel = 'الكل';
  protected readonly isAllSelected = computed(() => this.selectedTypeCodes().length === 0);

  protected isSelected(code: string): boolean {
    return this.selectedTypeCodes().includes(code);
  }

  protected selectAll(): void {
    if (!this.disabled() && !this.isAllSelected()) {
      this.typeCodesChange.emit([]);
    }
  }

  protected toggle(code: string): void {
    if (this.disabled()) {
      return;
    }
    const selected = this.isAllSelected() ? new Set<string>() : new Set(this.selectedTypeCodes());
    selected.has(code) ? selected.delete(code) : selected.add(code);
    this.typeCodesChange.emit(normalizeSelectedSourceTypeCodes([...selected], this.items()));
  }
}
