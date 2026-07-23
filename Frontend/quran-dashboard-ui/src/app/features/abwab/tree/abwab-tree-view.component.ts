import { ChangeDetectionStrategy, Component, ViewChild, input, output, signal } from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { AbwabVisibleTreeNode } from './abwab-tree-node';

const ROW_HEIGHT_PX = 40;

@Component({
  selector: 'qd-abwab-tree-view',
  standalone: true,
  imports: [ScrollingModule],
  templateUrl: './abwab-tree-view.component.html',
  styleUrl: './abwab-tree-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { dir: 'rtl' },
})
export class AbwabTreeViewComponent {
  @ViewChild(CdkVirtualScrollViewport) private viewport?: CdkVirtualScrollViewport;

  readonly nodes = input.required<readonly AbwabVisibleTreeNode[]>();
  readonly selectedCategoryId = input<string | null>(null);
  readonly canReorder = input(false);
  readonly canMove = input(false);
  readonly canDelete = input(false);

  readonly select = output<string>();
  readonly toggleExpand = output<string>();
  readonly moveUp = output<string>();
  readonly moveDown = output<string>();
  readonly moveRequested = output<string>();
  readonly deleteRequested = output<string>();

  protected readonly rowHeight = ROW_HEIGHT_PX;
  protected readonly focusedIndex = signal(0);

  protected trackByCategoryId(_index: number, node: AbwabVisibleTreeNode): string {
    return node.category.categoryId;
  }

  protected onRowActivate(node: AbwabVisibleTreeNode): void {
    this.select.emit(node.category.categoryId);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const nodes = this.nodes();
    if (nodes.length === 0) {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.moveFocus(Math.min(this.focusedIndex() + 1, nodes.length - 1));
        return;
      case 'ArrowUp':
        event.preventDefault();
        this.moveFocus(Math.max(this.focusedIndex() - 1, 0));
        return;
      case 'Home':
        event.preventDefault();
        this.moveFocus(0);
        return;
      case 'End':
        event.preventDefault();
        this.moveFocus(nodes.length - 1);
        return;
      case 'Enter':
      case ' ':
        event.preventDefault();
        this.select.emit(nodes[this.focusedIndex()].category.categoryId);
        return;
      // RTL: ArrowLeft expands (moves "inward"), ArrowRight collapses — the WAI-ARIA treeview
      // pattern's reversal of the LTR left/right expand/collapse assignment for RTL locales.
      case 'ArrowLeft': {
        const node = nodes[this.focusedIndex()];
        if (node.hasChildren && !node.isExpanded) {
          event.preventDefault();
          this.toggleExpand.emit(node.category.categoryId);
        }
        return;
      }
      case 'ArrowRight': {
        const node = nodes[this.focusedIndex()];
        if (node.hasChildren && node.isExpanded) {
          event.preventDefault();
          this.toggleExpand.emit(node.category.categoryId);
        }
        return;
      }
      default:
        return;
    }
  }

  private moveFocus(index: number): void {
    this.focusedIndex.set(index);
    this.viewport?.scrollToIndex(index, 'auto');
    queueMicrotask(() => {
      const host = this.viewport?.elementRef.nativeElement;
      const target = host?.querySelector<HTMLElement>(`[data-index="${index}"]`);
      target?.focus();
    });
  }
}
