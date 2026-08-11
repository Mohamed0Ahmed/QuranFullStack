import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { qdLoadingSizeReservation } from '../../../../shared/layout/loading-size-reservation';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';

import {
  STEMS_EMPTY_SELECTION_LABEL,
  STEMS_NOT_FOUND_LABEL,
  STEMS_PANEL_LABEL,
  STEMS_PANEL_TAB_ARIA,
  STEMS_PANEL_TAB_LABELS,
} from '../../models/stems.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { STEM_VIEW_KEYS, StemView } from '../../models/stems.models';

@Component({
  selector: 'qd-stem-details-panel',
  standalone: true,
  imports: [NgTemplateOutlet, QdActionDirective, QdDetailsWorkspaceComponent, QdModalShellComponent, QdTabDirective, QdTabsComponent],
  templateUrl: './stem-details-panel.component.html',
  styleUrl: './stem-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemDetailsPanelComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly view = input.required<StemView>();
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly notFoundMessage = input('');

  readonly viewChange = output<StemView>();
  readonly close = output<void>();

  private readonly contentElement = viewChild<ElementRef<HTMLElement>>('detailsContent');

  protected readonly reservedBlockSize = qdLoadingSizeReservation({
    host: this.contentElement,
    isLoading: this.loading,
    isSettled: computed(() => !this.loading() && !this.notFound() && !this.emptySelection()),
  }).reservedBlockSize;

  protected get panelLabel() {
    return STEMS_PANEL_LABEL;
  }

  protected get closeLabel() {
    return CLOSE_LABEL;
  }

  protected get emptySelectionLabel() {
    return STEMS_EMPTY_SELECTION_LABEL;
  }

  protected get notFoundLabel() {
    return STEMS_NOT_FOUND_LABEL;
  }

  protected readonly tabs = STEM_VIEW_KEYS.map((key) => ({
    key,
    label: STEMS_PANEL_TAB_LABELS[key],
    aria: STEMS_PANEL_TAB_ARIA[key],
  }));

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected isActive(key: StemView): boolean {
    return this.view() === key;
  }

  protected selectView(key: StemView): void {
    if (this.emptySelection() || this.notFound() || key === this.view()) {
      return;
    }
    this.viewChange.emit(key);
  }

  protected tabDisabled(key: StemView): boolean {
    return this.emptySelection() || (this.notFound() && key !== this.view());
  }

  protected onEscape(): void {
    if (!this.inline() || this.hasSelection()) {
      this.close.emit();
    }
  }
}
