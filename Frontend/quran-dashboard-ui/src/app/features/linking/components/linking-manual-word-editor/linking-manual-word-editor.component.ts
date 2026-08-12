import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingManualWordEditorFacade } from '../../state/linking-manual-word-editor.facade';

@Component({
  selector: 'qd-linking-manual-word-editor',
  standalone: true,
  imports: [
    QdActionDirective,
    AyahCardComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
  ],
  templateUrl: './linking-manual-word-editor.component.html',
  styleUrl: './linking-manual-word-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingManualWordEditorComponent {
  private readonly facade = inject(LinkingManualWordEditorFacade);
  private readonly destroyRef = inject(DestroyRef);

  readonly sourceKey = input<string | null>(null);
  readonly dismissed = output<void>();

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.facade.state;
  protected readonly includedVerseKeys = this.facade.includedVerseKeys;
  protected readonly ayahs = this.facade.ayahs;
  protected readonly status = this.facade.status;
  protected readonly selectedWordCount = this.facade.selectedWordCount;

  constructor() {
    effect(() => this.facade.open(this.sourceKey()));
    this.destroyRef.onDestroy(() => this.facade.close());
  }

  protected dismiss(): void {
    this.facade.close();
    this.dismissed.emit();
  }

  protected save(): void {
    if (this.facade.save()) {
      this.dismissed.emit();
    }
  }

  protected toggleWord(verseKey: string, quranWordId: number): void {
    this.facade.toggleWord(verseKey, quranWordId);
  }

  protected clearAyah(verseKey: string): void {
    this.facade.clearAyah(verseKey);
  }

  protected retry(): void {
    this.facade.retry();
  }
}
