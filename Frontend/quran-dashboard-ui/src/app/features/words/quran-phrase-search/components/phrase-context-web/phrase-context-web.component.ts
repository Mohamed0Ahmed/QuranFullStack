import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { PhraseContextBranchOptionDto } from '../../../../../core/api/generated/models/phrase-context-branch-option-dto';
import { PhraseContextSidePageDto } from '../../../../../core/api/generated/models/phrase-context-side-page-dto';
import { PhraseSelectedPathDto } from '../../../../../core/api/generated/models/phrase-selected-path-dto';
import { phraseOccurrenceLabel, phraseWordLabel } from '../phrase-context-copy';
import { PhraseContextAlternativeGroupComponent } from '../phrase-context-alternative-group/phrase-context-alternative-group.component';

export type PhraseContextWebSide = 'previous' | 'following';

@Component({
  selector: 'qd-phrase-context-web',
  standalone: true,
  imports: [PhraseContextAlternativeGroupComponent],
  templateUrl: './phrase-context-web.component.html',
  styleUrl: './phrase-context-web.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextWebComponent {
  readonly side = input.required<PhraseContextWebSide>();
  readonly page = input.required<PhraseContextSidePageDto>();
  readonly selection = input.required<PhraseSelectedPathDto>();
  readonly options = input.required<readonly PhraseContextBranchOptionDto[]>();
  readonly busy = input(false);
  readonly navigationDisabled = input(false);
  readonly alternativeGroupActive = input(false);

  readonly optionSelected = output<string>();
  readonly pathSelected = output<string | null>();
  readonly alternativeToggled = output<string | null>();
  readonly alternativesCleared = output<void>();
  readonly moreRequested = output<void>();

  protected readonly occurrenceLabel = phraseOccurrenceLabel;
  protected readonly wordLabel = phraseWordLabel;
  protected readonly pathSteps = computed(() => this.selection().steps ?? []);
  protected readonly selectedAlternatives = computed(() =>
    this.options().filter((option) => option.isAlternativeSelected),
  );
  protected readonly pathNavigationDisabled = computed(() =>
    this.navigationDisabled() || this.alternativeGroupActive(),
  );
  protected readonly fallbackCurrentLabel = computed(() => {
    if (this.selection().endsAtBoundary) {
      return this.side() === 'previous' ? 'بداية الآية' : 'نهاية الآية';
    }
    return this.selection().tokens.at(-1)?.textUthmani ?? 'المستوى الحالي';
  });
  protected readonly tableOptions = computed<readonly PhraseContextBranchOptionDto[]>(() => {
    if (this.options().length > 0) {
      return this.options();
    }
    const selection = this.selection();
    const current = this.pathSteps().at(-1);
    if (!selection.endsAtBoundary || !selection.selectionRef) {
      return [];
    }
    const boundaryKind = current?.boundaryKind ?? (
      this.side() === 'previous' ? 'ayah-start' : 'ayah-end'
    );
    return [{
      boundaryKind,
      displayText: current?.displayText ?? this.fallbackCurrentLabel(),
      exactTokenId: null,
      alternativeToggleRef: null,
      isAlternativeSelected: false,
      passesThroughCount: this.page().passesThroughCount,
      selectionRef: selection.selectionRef,
      sideEndsHereCount: this.page().passesThroughCount,
    }];
  });
  protected readonly displayedOptionCount = computed(() =>
    Math.max(this.page().totalOptions, this.tableOptions().length),
  );

  protected alternativeActionLabel(option: PhraseContextBranchOptionDto): string {
    const action = option.isAlternativeSelected ? 'إزالة' : 'إضافة';
    const group = this.side() === 'previous'
      ? 'مجموعة الكلمات السابقة'
      : 'مجموعة الكلمات اللاحقة';
    return `${action} ${option.displayText} ${option.isAlternativeSelected ? 'من' : 'إلى'} ${group}`;
  }
}
