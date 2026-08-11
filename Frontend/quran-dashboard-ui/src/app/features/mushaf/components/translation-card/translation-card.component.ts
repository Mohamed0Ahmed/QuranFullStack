import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TranslationEntryDto } from '../../models/mushaf.models';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { SafeHtmlPipe } from '../../../../shared/ui/safe-html/safe-html.pipe';

@Component({
  selector: 'qd-translation-card',
  standalone: true,
  imports: [CommonModule, QdEmptyStateComponent, SafeHtmlPipe],
  templateUrl: './translation-card.component.html',
  styleUrls: ['./translation-card.component.scss'],
})
export class TranslationCardComponent {
  readonly entry = input<TranslationEntryDto | null>(null);
}
