import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, ElementRef, input, output, signal, viewChild } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import {
  QdFloatingLayerDirective,
  QdFloatingLayerDismissReason,
} from '../../../../shared/ui/floating-layer/floating-layer.directive';
import {
  ExplorerTableColumnMove,
  ExplorerTableColumnState,
} from '../../state/explorer-table-columns.controller';

let nextPanelId = 0;

@Component({
  selector: 'qd-explorer-table-column-settings',
  standalone: true,
  imports: [CdkDrag, CdkDragHandle, CdkDropList, QdActionDirective, QdFloatingLayerDirective],
  templateUrl: './explorer-table-column-settings.component.html',
  styleUrl: './explorer-table-column-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerTableColumnSettingsComponent {
  readonly columns = input.required<readonly ExplorerTableColumnState[]>();
  readonly testIdPrefix = input.required<string>();

  readonly visibilityChanged = output<{ key: string; visible: boolean }>();
  readonly moved = output<{ key: string; direction: ExplorerTableColumnMove }>();
  readonly reordered = output<{ fromIndex: number; toIndex: number }>();
  readonly resetRequested = output<void>();

  private readonly trigger = viewChild<ElementRef<HTMLButtonElement>>('trigger');
  private readonly instanceId = nextPanelId++;
  protected readonly panelId = `explorer-table-column-settings-${this.instanceId}`;
  protected readonly panelOpen = signal(false);

  protected get triggerElement(): HTMLButtonElement | null {
    return this.trigger()?.nativeElement ?? null;
  }

  protected togglePanel(): void {
    this.panelOpen.update((open) => !open);
  }

  protected onDismissed(reason: QdFloatingLayerDismissReason): void {
    this.panelOpen.set(false);
    if (reason === 'escape') {
      this.triggerElement?.focus();
    }
  }

  protected onVisibilityChanged(column: ExplorerTableColumnState, event: Event): void {
    this.visibilityChanged.emit({
      key: column.key,
      visible: (event.target as HTMLInputElement).checked,
    });
  }

  protected move(column: ExplorerTableColumnState, direction: ExplorerTableColumnMove): void {
    this.moved.emit({ key: column.key, direction });
  }

  protected canMoveUp(index: number): boolean {
    const columns = this.columns();
    return index > 0 && !columns[index].reorderLocked && !columns[index - 1].reorderLocked;
  }

  protected canMoveDown(index: number): boolean {
    const columns = this.columns();
    return index < columns.length - 1 && !columns[index].reorderLocked && !columns[index + 1].reorderLocked;
  }

  protected drop(event: CdkDragDrop<readonly ExplorerTableColumnState[]>): void {
    this.reordered.emit({ fromIndex: event.previousIndex, toIndex: event.currentIndex });
  }
}
