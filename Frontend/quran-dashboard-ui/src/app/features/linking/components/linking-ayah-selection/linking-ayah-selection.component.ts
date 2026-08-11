import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdControlDirective } from '../../../../shared/ui/form-field/control.directive';
import { QdFormFieldComponent } from '../../../../shared/ui/form-field/form-field.component';
import { arabicSearchIncludes } from '../../../../shared/quran/arabic-search-normalize';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSelection } from '../../models/linking-workspace.models';
import { isVerseSelected } from '../../utils/linking-selection';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';

@Component({
  selector: 'qd-linking-ayah-selection',
  standalone: true,
  imports: [QdActionDirective, QdControlDirective, QdFormFieldComponent, LinkingAyahCardComponent],
  templateUrl: './linking-ayah-selection.component.html',
  styleUrl: './linking-ayah-selection.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahSelectionComponent {
  readonly ayahs = input.required<readonly LinkingAyah[]>();
  readonly selection = input.required<LinkingSelection>();
  readonly highlightSourceWords = input(true);
  readonly selectedCount = input.required<number>();

  readonly selectionToggled = output<string>();
  readonly selectAllRequested = output<void>();
  readonly clearAllRequested = output<void>();

  protected readonly labels = LINKING_LABELS;
  protected readonly searchQuery = signal('');
  protected readonly visibleAyahs = computed(() => {
    const query = this.searchQuery();
    return this.ayahs().filter((ayah) => arabicSearchIncludes(searchText(ayah), query));
  });

  protected onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected isSelected(verseKey: string): boolean {
    return isVerseSelected(this.selection(), verseKey);
  }
}

function searchText(ayah: LinkingAyah): string {
  return [ayah.verseKey, ayah.surahNameArabic ?? '', ...ayah.words.filter((word) => !word.isAyahMarker).map((word) => word.textUthmani)].join(' ');
}
