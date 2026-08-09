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

import {
  LEMMAS_EMPTY_SELECTION_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
  LEMMAS_PANEL_LABEL,
  LEMMAS_PANEL_TAB_ARIA,
  LEMMAS_PANEL_TAB_LABELS,
} from '../../models/lemmas.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { LEMMA_VIEW_KEYS, LemmaView } from '../../models/lemmas.models';

@Component({
  selector: 'qd-lemma-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet, QdActionDirective, QdDetailsWorkspaceComponent, QdTabDirective, QdTabsComponent],
  templateUrl: './lemma-details-panel.component.html',
  styleUrl: './lemma-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaDetailsPanelComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly view = input.required<LemmaView>();
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly notFoundMessage = input('');

  readonly viewChange = output<LemmaView>();
  readonly close = output<void>();

  protected get panelLabel() {
    return LEMMAS_PANEL_LABEL;
  }

  protected get closeLabel() {
    return CLOSE_LABEL;
  }

  protected get emptySelectionLabel() {
    return LEMMAS_EMPTY_SELECTION_LABEL;
  }

  protected get notFoundLabel() {
    return LEMMAS_NOT_FOUND_LABEL;
  }

  protected readonly tabs = LEMMA_VIEW_KEYS.map((key) => ({
    key,
    label: LEMMAS_PANEL_TAB_LABELS[key],
    aria: LEMMAS_PANEL_TAB_ARIA[key],
  }));

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected isActive(key: LemmaView): boolean {
    return this.view() === key;
  }

  protected selectView(key: LemmaView): void {
    if (this.emptySelection() || this.notFound() || key === this.view()) {
      return;
    }
    this.viewChange.emit(key);
  }

  protected tabDisabled(key: LemmaView): boolean {
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
