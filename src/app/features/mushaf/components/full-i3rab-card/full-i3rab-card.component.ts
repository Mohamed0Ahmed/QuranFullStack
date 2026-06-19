import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { FullI3rabEntryDto } from '../../models/mushaf.models';
import { SafeHtmlPipe } from '../../../../shared/ui/safe-html/safe-html.pipe';
import { formatCoverageAyahNumbers } from '../../utils/mushaf-verse-key-display';

@Component({
  selector: 'qd-full-i3rab-card',
  standalone: true,
  imports: [CommonModule, SafeHtmlPipe],
  templateUrl: './full-i3rab-card.component.html',
  styleUrls: ['./full-i3rab-card.component.scss'],
})
export class FullI3rabCardComponent {
  readonly entry = input<FullI3rabEntryDto | null>(null);

  protected readonly coverageAyahNumbers = computed(() => {
    const keys = this.entry()?.coveredAyahKeys;
    return keys ? formatCoverageAyahNumbers(keys) : '';
  });
}
