import { ChangeDetectionStrategy, Component, HostListener, input, output } from '@angular/core';

// Presentation-only row/node context menu shell (Slice A, plan §6 phase 6). Consumers
// project their own `role="menuitem"` items via `<ng-content>` — this component owns only
// the backdrop, positioning, and dismissal, never a door/template-node concern.
@Component({
  selector: 'qd-context-menu',
  standalone: true,
  templateUrl: './context-menu.component.html',
  styleUrl: './context-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdContextMenuComponent {
  readonly position = input.required<{ x: number; y: number }>();
  readonly menuTestId = input.required<string>();
  readonly backdropTestId = input.required<string>();

  readonly dismissed = output<void>();

  // Document-level, not `(keydown.escape)` on the menu element: none of the four open
  // paths (right-click, ⋯, the two workshop equivalents, or the tree's keyboard
  // ContextMenu/Shift+F10 path) puts focus inside the menu, so an element-bound handler
  // would never fire. Copies `top-navbar.component.ts`'s `document:keydown.escape` pattern.
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.dismissed.emit();
  }
}
