import { Component, input, linkedSignal, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahMutashabihatDto,
  AyahNavigationTarget,
  MutashabihatGroupDto,
  MutashabihatOccurrenceDto,
  MUTASHABIHAT_EMPTY_MESSAGE,
  MUTASHABIHAT_LOADING_MESSAGE,
  ResourceLoadState,
} from '../../models/mushaf.models';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';
import { buildCollapsedOccurrencePreview } from './mutashabihat-occurrence-preview';

const OCCURRENCE_PREVIEW_COUNT = 5;

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

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = MUTASHABIHAT_EMPTY_MESSAGE;
  protected readonly loadingMessage = MUTASHABIHAT_LOADING_MESSAGE;

  private readonly expandedGroupKeys = linkedSignal<AyahMutashabihatDto | null, ReadonlySet<string>>({
    source: () => this.mutashabihat(),
    computation: () => new Set(),
  });

  protected displayAyahText(textUthmani: string): string {
    return toStudyAyahDisplayText(textUthmani);
  }

  protected wordRangeLabel(wordFrom: number, wordTo: number): string {
    return wordFrom === wordTo ? `كلمة ${wordFrom}` : `كلمات ${wordFrom}–${wordTo}`;
  }

  protected visibleOccurrences(group: MutashabihatGroupDto): MutashabihatOccurrenceDto[] {
    if (this.isGroupExpanded(group.groupKey)) {
      return group.occurrences;
    }

    return buildCollapsedOccurrencePreview(group.occurrences, OCCURRENCE_PREVIEW_COUNT);
  }

  protected hiddenOccurrenceCount(group: MutashabihatGroupDto): number {
    if (this.isGroupExpanded(group.groupKey)) {
      return 0;
    }

    return Math.max(0, group.occurrences.length - this.visibleOccurrences(group).length);
  }

  protected occurrenceListId(groupKey: string): string {
    return `mutashabihat-occurrences-${groupKey}`;
  }

  protected showExpandToggle(group: MutashabihatGroupDto): boolean {
    return group.occurrences.length > OCCURRENCE_PREVIEW_COUNT;
  }

  protected isGroupExpanded(groupKey: string): boolean {
    return this.expandedGroupKeys().has(groupKey);
  }

  protected toggleGroupExpanded(groupKey: string): void {
    const next = new Set(this.expandedGroupKeys());

    if (next.has(groupKey)) {
      next.delete(groupKey);
    } else {
      next.add(groupKey);
    }

    this.expandedGroupKeys.set(next);
  }

  protected ayahNavigateLabel(occurrence: MutashabihatOccurrenceDto): string {
    return `فتح ${occurrence.surahNameArabic} — ${occurrence.ayahNumber} في المصحف`;
  }

  protected onAyahNavigate(occurrence: MutashabihatOccurrenceDto): void {
    this.ayahNavigate.emit({
      verseKey: occurrence.verseKey,
      pageNumber: occurrence.pageNumber,
    });
  }
}
