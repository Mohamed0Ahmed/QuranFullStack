import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { WORD_TYPE_DETAIL_PRESENTATIONS } from '../../models/word-types.labels';
import {
  WORD_TYPE_DETAIL_VIEWS,
  WORD_TYPE_DETAIL_VIEW_KEYS,
  WordTypeDetailView,
} from '../../models/word-types.models';
import { WordTypeDetailSelectionKind } from '../../models/word-types-detail.models';

@Component({
  selector: 'qd-word-type-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet, QdActionDirective, QdDetailsWorkspaceComponent, QdTabDirective, QdTabsComponent],
  templateUrl: './word-type-details-panel.component.html',
  styleUrl: './word-type-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailsPanelComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  // Only the top layer may trap focus: while the detail overlay is open this drawer sits in the inert
  // app shell, so its own trap stands down and the dialog's is the only one enabled.
  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly view = input.required<WordTypeDetailView>();
  readonly kind = input<WordTypeDetailSelectionKind>('word');
  readonly inline = input(true);
  // Content-only mode: renders just the tablist + tabpanel body, no dialog chrome. Used inside the
  // global detail overlay shell, which owns the dialog chrome and the notFound rendering.
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);

  readonly viewChange = output<WordTypeDetailView>();
  readonly close = output<void>();

  protected get panelLabel() { return this.presentation.panelLabel; }
  protected get closeLabel() { return CLOSE_LABEL; }
  protected get emptySelectionLabel() { return this.presentation.emptySelectionLabel; }
  protected get notFoundLabel() { return this.presentation.notFoundLabel; }

  private get presentation() { return WORD_TYPE_DETAIL_PRESENTATIONS[this.kind()]; }

  // Word selections expose ayahs/surahs; grouped selections add the leading related-words tab.
  protected readonly tabKeys = computed<readonly WordTypeDetailView[]>(() =>
    this.kind() === 'word' ? WORD_TYPE_DETAIL_VIEW_KEYS : WORD_TYPE_DETAIL_VIEWS,
  );

  protected readonly tabs = computed(() =>
    this.tabKeys().map((key) => ({
      key,
      ...this.presentation.tabs[key],
    })),
  );

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected isActive(key: WordTypeDetailView): boolean {
    return this.view() === key;
  }

  protected selectView(key: WordTypeDetailView): void {
    if (this.emptySelection() || this.notFound() || key === this.view()) {
      return;
    }

    this.viewChange.emit(key);
  }

  protected tabDisabled(key: WordTypeDetailView): boolean {
    return this.emptySelection() || (this.notFound() && key !== this.view());
  }

  protected onEscape(): void {
    if (!this.inline() || this.hasSelection()) {
      this.close.emit();
    }
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close.emit();
    }
  }
}
