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
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingManualLinkShape } from '../../models/linking-manual-mushaf.models';
import { LinkingSourceEditorFacade } from '../../state/linking-source-editor.facade';
import { LinkingManualShapeSelectorComponent } from '../linking-manual-shape-selector/linking-manual-shape-selector.component';
import { LinkingSourceTypeFiltersComponent } from '../linking-source-type-filters/linking-source-type-filters.component';
import {
  LinkingVirtualAyahListComponent,
  LinkingVirtualWordToggle,
} from '../linking-virtual-ayah-list/linking-virtual-ayah-list.component';

@Component({
  selector: 'qd-linking-source-ayah-editor',
  standalone: true,
  imports: [
    QdActionDirective,
    LinkingManualShapeSelectorComponent,
    LinkingSourceTypeFiltersComponent,
    LinkingVirtualAyahListComponent,
  ],
  templateUrl: './linking-source-ayah-editor.component.html',
  styleUrl: './linking-source-ayah-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingSourceAyahEditorComponent {
  protected readonly facade = inject(LinkingSourceEditorFacade);
  private readonly destroyRef = inject(DestroyRef);

  readonly sourceKey = input<string | null>(null);
  readonly dismissPending = input(false);
  readonly dismissed = output<void>();

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.facade.state;
  protected readonly request = this.facade.request;
  protected readonly selectedCount = this.facade.selectedCount;
  protected readonly currentItem = this.facade.currentItem;
  protected readonly automaticConfiguration = computed(() => {
    const configuration = this.currentItem()?.configuration ?? null;
    return configuration?.kind === 'automatic' ? configuration : null;
  });

  constructor() {
    effect(() => this.facade.open(this.sourceKey()));
    this.destroyRef.onDestroy(() => this.facade.close());
  }

  protected dismiss(): void {
    if (!this.dismissPending()) {
      this.dismissed.emit();
    }
  }

  protected retry(): void {
    this.facade.retry();
  }

  protected toggleWord(toggle: LinkingVirtualWordToggle): void {
    this.facade.toggleManualWord(toggle.ayahId, toggle.quranWordId);
  }

  protected setManualLinkShape(linkShape: LinkingManualLinkShape): void {
    this.facade.setManualLinkShape(linkShape);
  }
}
