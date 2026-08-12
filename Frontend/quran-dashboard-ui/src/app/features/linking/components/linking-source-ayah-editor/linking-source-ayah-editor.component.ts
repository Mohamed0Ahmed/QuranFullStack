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
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSourceEditorFacade } from '../../state/linking-source-editor.facade';
import { LinkingAyahSelectionComponent } from '../linking-ayah-selection/linking-ayah-selection.component';
import { LinkingAyahDescriptionsComponent } from './linking-ayah-descriptions.component';

@Component({
  selector: 'qd-linking-source-ayah-editor',
  standalone: true,
  imports: [
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    LinkingAyahSelectionComponent,
    LinkingAyahDescriptionsComponent,
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

  protected descriptionsFor(ayah: LinkingAyah): readonly string[] {
    return this.facade.descriptionsFor(ayah.ayahId);
  }

  protected setDescriptions(ayah: LinkingAyah, bodies: readonly string[]): void {
    this.facade.setDescriptions(ayah.ayahId, bodies);
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

  protected setManualLinkShape(event: Event): void {
    this.facade.setManualLinkShape((event.target as HTMLInputElement).value as 'grouped' | 'independent');
  }
}
