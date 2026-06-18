import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { TafsirEntryDto } from '../../models/mushaf.models';
import { SafeHtmlPipe } from '../../../../shared/ui/safe-html/safe-html.pipe';

@Component({
  selector: 'qd-tafsir-card',
  standalone: true,
  imports: [CommonModule, SafeHtmlPipe],
  templateUrl: './tafsir-card.component.html',
  styleUrls: ['./tafsir-card.component.scss'],
})
export class TafsirCardComponent {
  readonly entry = input<TafsirEntryDto | null>(null);
}
