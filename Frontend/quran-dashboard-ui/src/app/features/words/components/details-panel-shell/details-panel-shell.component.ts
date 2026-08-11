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

export interface QdDetailsPanelTab<TKey extends string = string> {
  readonly key: TKey;
  readonly label: string;
  readonly aria: string;
}

@Component({
  selector: 'qd-details-panel-shell',
  standalone: true,
  imports: [NgTemplateOutlet, QdActionDirective, QdDetailsWorkspaceComponent, QdModalShellComponent, QdTabDirective, QdTabsComponent],
  templateUrl: './details-panel-shell.component.html',
  styleUrl: './details-panel-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdDetailsPanelShellComponent<TKey extends string> {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly testIdPrefix = input.required<string>();
  readonly panelLabel = input.required<string>();
  readonly closeLabel = input.required<string>();
  readonly emptySelectionLabel = input.required<string>();
  readonly notFoundLabel = input.required<string>();
  readonly tabs = input.required<readonly QdDetailsPanelTab<TKey>[]>();
  readonly view = input.required<TKey>();
  readonly tabsTrackFloor = input('');
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly notFoundMessage = input('');

  readonly viewChange = output<TKey>();
  readonly closed = output<void>();

  private readonly contentElement = viewChild<ElementRef<HTMLElement>>('detailsContent');

  protected readonly reservedBlockSize = qdLoadingSizeReservation({
    host: this.contentElement,
    isLoading: this.loading,
    isSettled: computed(() => !this.loading() && !this.notFound() && !this.emptySelection()),
  }).reservedBlockSize;

  protected readonly blockClass = computed(() => `${this.testIdPrefix()}-panel`);
  protected readonly framelessClass = computed(
    () => `${this.blockClass()} qd-details-panel qd-details-panel--frameless`,
  );
  protected readonly inlineClass = computed(
    () => `explorer-detail-panel ${this.blockClass()} qd-details-panel`,
  );
  protected readonly modalClass = computed(
    () => `explorer-detail-modal ${this.testIdPrefix()}-modal`,
  );

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected readonly labelledTabKey = computed<TKey>(() => {
    const keys = this.tabs().map((tab) => tab.key);
    return keys.includes(this.view()) ? this.view() : keys[0];
  });

  protected isActive(key: TKey): boolean {
    return this.view() === key;
  }

  protected selectView(key: TKey): void {
    if (this.emptySelection() || this.notFound() || key === this.view()) {
      return;
    }

    this.viewChange.emit(key);
  }

  protected tabDisabled(key: TKey): boolean {
    return this.emptySelection() || (this.notFound() && key !== this.view());
  }

  protected onEscape(): void {
    if (!this.inline() || this.hasSelection()) {
      this.closed.emit();
    }
  }
}
