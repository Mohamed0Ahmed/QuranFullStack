import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';

import { PhraseContextBranchOptionDto } from '../../../../../core/api/generated/models/phrase-context-branch-option-dto';
import { PhraseContextBranchesResponse } from '../../../../../core/api/generated/models/phrase-context-branches-response';
import { PhraseContextFocusTarget } from '../../models/phrase-context.models';

@Component({
  selector: 'qd-phrase-context-explorer',
  standalone: true,
  templateUrl: './phrase-context-explorer.component.html',
  styleUrl: './phrase-context-explorer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextExplorerComponent {
  readonly branches = input.required<PhraseContextBranchesResponse>();
  readonly previousOptions = input.required<readonly PhraseContextBranchOptionDto[]>();
  readonly followingOptions = input.required<readonly PhraseContextBranchOptionDto[]>();
  readonly busy = input(false);
  readonly focusTarget = input<PhraseContextFocusTarget | null>(null);

  readonly previousSelected = output<string>();
  readonly followingSelected = output<string>();
  readonly previousReversed = output<void>();
  readonly followingReversed = output<void>();
  readonly previousMoreRequested = output<void>();
  readonly followingMoreRequested = output<void>();

  protected selectPrevious(option: PhraseContextBranchOptionDto): void {
    this.previousSelected.emit(option.selectionRef);
  }

  protected selectFollowing(option: PhraseContextBranchOptionDto): void {
    this.followingSelected.emit(option.selectionRef);
  }

  protected boundaryLabel(option: PhraseContextBranchOptionDto): string {
    if (option.boundaryKind === 'start') {
      return 'بداية الآية';
    }
    if (option.boundaryKind === 'end') {
      return 'نهاية الآية';
    }
    return option.displayText;
  }
}
