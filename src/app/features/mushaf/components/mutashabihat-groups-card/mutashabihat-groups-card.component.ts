import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahMutashabihatDto,
  MUTASHABIHAT_EMPTY_MESSAGE,
  MUTASHABIHAT_LOADING_MESSAGE,
  ResourceLoadState,
} from '../../models/mushaf.models';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';

@Component({
  selector: 'qd-mutashabihat-groups-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mutashabihat-groups-card.component.html',
  styleUrls: ['./mutashabihat-groups-card.component.scss'],
})
export class MutashabihatGroupsCardComponent {
  readonly mutashabihat = input<AyahMutashabihatDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();

  protected readonly emptyMessage = MUTASHABIHAT_EMPTY_MESSAGE;
  protected readonly loadingMessage = MUTASHABIHAT_LOADING_MESSAGE;

  protected displayAyahText(textUthmani: string): string {
    return toStudyAyahDisplayText(textUthmani);
  }

  protected wordRangeLabel(wordFrom: number, wordTo: number): string {
    return wordFrom === wordTo ? `كلمة ${wordFrom}` : `كلمات ${wordFrom}–${wordTo}`;
  }
}
