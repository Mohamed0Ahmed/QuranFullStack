import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { QdActionDirective } from '../../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../../shared/ui/error-state/error-state.component';
import { LINKING_LABELS } from '../../../../linking/models/linking.labels';
import { PhraseContextUrlState } from '../../models/phrase-context.models';
import { PhraseLinkingAyahSelectionStore } from '../../state/phrase-linking-ayah-selection.store';
import { PhraseContextLinkingCoordinator } from '../../state/phrase-context-linking.coordinator';

@Component({
  selector: 'qd-phrase-context-linking-actions',
  standalone: true,
  imports: [QdActionDirective, QdErrorStateComponent],
  templateUrl: './phrase-context-linking-actions.component.html',
  styleUrl: './phrase-context-linking-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextLinkingActionsComponent {
  private readonly selection = inject(PhraseLinkingAyahSelectionStore);
  protected readonly linking = inject(PhraseContextLinkingCoordinator);

  readonly route = input.required<PhraseContextUrlState>();
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
    this.linking.addToWorkspace(this.route());
  }

  protected startDirectLink(): void {
    this.linking.startDirectLink(this.route());
  }
}
