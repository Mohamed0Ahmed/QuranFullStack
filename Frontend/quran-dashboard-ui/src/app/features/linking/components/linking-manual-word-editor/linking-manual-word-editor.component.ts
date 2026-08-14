import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingManualWordEditorFacade } from '../../state/linking-manual-word-editor.facade';
import {
  LinkingVirtualAyahListComponent,
  LinkingVirtualWordToggle,
} from '../linking-virtual-ayah-list/linking-virtual-ayah-list.component';

@Component({
  selector: 'qd-linking-manual-word-editor',
  standalone: true,
  imports: [QdActionDirective, LinkingVirtualAyahListComponent],
  templateUrl: './linking-manual-word-editor.component.html',
  styleUrl: './linking-manual-word-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingManualWordEditorComponent {
  protected readonly facade = inject(LinkingManualWordEditorFacade);
  private readonly destroyRef = inject(DestroyRef);

  readonly sourceKey = input<string | null>(null);
  readonly dismissed = output<void>();

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.facade.state;
  protected readonly item = this.facade.item;
  protected readonly request = this.facade.request;
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

  protected toggleWord(toggle: LinkingVirtualWordToggle): void {
    this.facade.toggleWord(toggle.ayahId, toggle.quranWordId);
  }
}
