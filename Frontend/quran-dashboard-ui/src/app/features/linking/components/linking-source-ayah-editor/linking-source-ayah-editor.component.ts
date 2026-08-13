import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
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
import {
  LinkingAyahSelectionComponent,
  type LinkingWordToggle,
} from '../linking-ayah-selection/linking-ayah-selection.component';
import { LinkingManualShapeSelectorComponent } from '../linking-manual-shape-selector/linking-manual-shape-selector.component';

@Component({
  selector: 'qd-linking-source-ayah-editor',
  standalone: true,
  imports: [
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    LinkingAyahSelectionComponent,
    LinkingManualShapeSelectorComponent,
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

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.facade.state;
  protected readonly selection = this.facade.selection;
  protected readonly selectedCount = this.facade.selectedCount;
  protected readonly configuredAyahs = this.facade.configuredAyahs;
  protected readonly currentItem = this.facade.currentItem;
  protected readonly automaticConfiguration = computed(() => {
    const configuration = this.currentItem()?.configuration ?? null;
    return configuration?.kind === 'automatic' ? configuration : null;
  });
  protected readonly isManual = computed(() => this.currentItem()?.configuration.kind === 'manual');
  protected readonly isManualGrouped = computed(() => {
    const configuration = this.currentItem()?.configuration;
    return configuration?.kind === 'manual'
      && configuration.linkShape === 'grouped'
      && this.selectedCount() > 1;
  });

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

  protected toggleAyah(verseKey: string): void {
    this.facade.toggleAyah(verseKey);
  }

  protected toggleWord(toggle: LinkingWordToggle): void {
    this.facade.toggleManualWord(toggle.verseKey, toggle.quranWordId);
  }

  protected selectAll(): void {
    this.facade.selectAll();
  }

  protected clearAll(): void {
    this.facade.clearAll();
  }

  protected setAutomaticWordMatches(enabled: boolean): void {
    this.facade.setAutomaticWordMatches(enabled);
  }

  protected setManualLinkShape(linkShape: LinkingManualLinkShape): void {
    this.facade.setManualLinkShape(linkShape);
  }
}
