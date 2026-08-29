import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { QdActionDirective } from '../../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../../shared/ui/error-state/error-state.component';
import { LINKING_LABELS } from '../../../../linking/models/linking.labels';
import { PhraseSimilarityAyahSelectionStore } from '../../state/phrase-similarity-ayah-selection.store';
import { PhraseSimilarityLinkingCoordinator } from '../../state/phrase-similarity-linking.coordinator';

@Component({
  selector: 'qd-phrase-similarity-linking-actions',
  standalone: true,
  imports: [QdActionDirective, QdErrorStateComponent],
  templateUrl: './phrase-similarity-linking-actions.component.html',
  styleUrl: './phrase-similarity-linking-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseSimilarityLinkingActionsComponent {
  private readonly selection = inject(PhraseSimilarityAyahSelectionStore);
  protected readonly linking = inject(PhraseSimilarityLinkingCoordinator);

  readonly disabled = input(false);

  protected readonly labels = LINKING_LABELS;
  protected readonly selectedCount = this.selection.selectedCount;
  protected readonly actionDisabled = computed(
    () => this.disabled() || this.selectedCount() === 0 || this.linking.resolving(),
  );
  protected readonly addLabel = computed(
    () => `${this.labels.addToWorkspace}: ${this.selectedCount()} آية محددة`,
  );
  protected readonly directLabel = computed(
    () => `${this.labels.directLink}: ${this.selectedCount()} آية محددة`,
  );

  protected addToWorkspace(): void {
    this.linking.addToWorkspace();
  }

  protected startDirectLink(): void {
    this.linking.startDirectLink();
  }
}
