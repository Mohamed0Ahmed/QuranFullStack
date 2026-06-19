import { Component, computed, input, output } from '@angular/core';

import { SourceOption } from '../../models/mushaf.models';

export interface SourceOptionGroup {
  languageNameAr: string | null;
  options: SourceOption[];
}

@Component({
  selector: 'qd-source-selector',
  standalone: true,
  templateUrl: './source-selector.component.html',
  styleUrls: ['./source-selector.component.scss'],
})
export class SourceSelectorComponent {
  readonly label = input.required<string>();
  readonly selectedKey = input<string | null>(null);
  readonly usedLabel = input<string | null>(null);
  readonly options = input<SourceOption[]>([]);

  readonly sourceChange = output<string>();

  protected readonly optionGroups = computed<SourceOptionGroup[]>(() => {
    const groups: SourceOptionGroup[] = [];
    const groupIndex = new Map<string, number>();

    for (const option of this.options()) {
      const languageKey = option.languageNameAr ?? '';
      let index = groupIndex.get(languageKey);
      if (index === undefined) {
        index = groups.length;
        groupIndex.set(languageKey, index);
        groups.push({
          languageNameAr: option.languageNameAr ?? null,
          options: [],
        });
      }
      groups[index].options.push(option);
    }

    return groups;
  });

  protected readonly useGroupedSelect = computed(
    () => this.options().length > 1 && this.optionGroups().some((group) => group.languageNameAr),
  );

  protected onSelectChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value) {
      this.sourceChange.emit(value);
    }
  }
}
