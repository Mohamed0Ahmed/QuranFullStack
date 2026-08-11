import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TafsirEntryDto } from '../../models/mushaf.models';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { SafeHtmlPipe } from '../../../../shared/ui/safe-html/safe-html.pipe';
import { formatCoverageAyahNumbers } from '../../utils/mushaf-verse-key-display';

@Component({
  selector: 'qd-tafsir-card',
  standalone: true,
  imports: [CommonModule, QdEmptyStateComponent, SafeHtmlPipe],
  templateUrl: './tafsir-card.component.html',
  styleUrls: ['./tafsir-card.component.scss'],
})
export class TafsirCardComponent {
  readonly entry = input<TafsirEntryDto | null>(null);

  protected readonly coverageAyahNumbers = computed(() => {
    const keys = this.entry()?.coveredAyahKeys;
    return keys ? formatCoverageAyahNumbers(keys) : '';
  });
}
