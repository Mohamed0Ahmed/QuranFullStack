import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
} from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { type LinkingManualLinkShape } from '../../models/linking-manual-mushaf.models';
import { LinkingSourceEditorFacade } from '../../state/linking-source-editor.facade';
import { LinkingAyahSelectionComponent } from '../linking-ayah-selection/linking-ayah-selection.component';

@Component({
  selector: 'qd-linking-source-ayah-editor',
  standalone: true,
  imports: [
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    LinkingAyahSelectionComponent,
  ],
  templateUrl: './linking-source-ayah-editor.component.html',
  styleUrl: './linking-source-ayah-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingSourceAyahEditorComponent {
  private readonly facade = inject(LinkingSourceEditorFacade);
  private readonly destroyRef = inject(DestroyRef);

  readonly sourceKey = input<string | null>(null);
  readonly dismissed = output<void>();
  readonly manualWordsRequested = output<void>();

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.facade.state;
  protected readonly selection = this.facade.selection;
  protected readonly selectedCount = this.facade.selectedCount;
  protected readonly filteredAyahs = this.facade.filteredAyahs;
  protected readonly currentItem = this.facade.currentItem;

  constructor() {
    effect(() => this.facade.open(this.sourceKey()));
    this.destroyRef.onDestroy(() => this.facade.close());
  }

  protected dismiss(): void {
    this.facade.close();
    this.dismissed.emit();
  }

  protected retry(): void {
    this.facade.retry();
  }

  protected setQuery(query: string): void {
    this.facade.setQuery(query);
  }

  protected toggleAyah(verseKey: string): void {
    this.facade.toggleAyah(verseKey);
  }

  protected selectAll(): void {
    this.facade.selectAll();
  }

  protected clearAll(): void {
    this.facade.clearAll();
  }

  protected setManualLinkShape(linkShape: LinkingManualLinkShape): void {
    this.facade.setManualLinkShape(linkShape);
  }
}
