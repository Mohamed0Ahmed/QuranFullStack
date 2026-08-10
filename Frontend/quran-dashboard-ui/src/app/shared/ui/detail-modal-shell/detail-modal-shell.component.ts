import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';

import { QdActionDirective } from '../action/action.directive';
import { QdModalShellComponent } from '../modal-shell/modal-shell.component';

let nextShellId = 0;

@Component({
  selector: 'qd-detail-modal-shell',
  standalone: true,
  imports: [QdActionDirective, QdModalShellComponent],
  templateUrl: './detail-modal-shell.component.html',
  styleUrl: './detail-modal-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(focusin)': 'onDialogFocusIn($event)',
  },
})
export class DetailModalShellComponent {
  readonly visibility = input.required<'open' | 'closed'>();
  readonly titleText = input.required<string>();
  readonly depth = input.required<number>();
  readonly backLabel = input.required<string>();
  readonly closeLabel = input.required<string>();
  readonly restoreLabel = input.required<string>();
  readonly restoreAriaLabel = input.required<string>();
  readonly statusMessage = input('');
  readonly kindLabel = input('');
  readonly countText = input('');

  readonly backRequested = output<void>();
  readonly closeRequested = output<void>();
  readonly restoreRequested = output<void>();

  private readonly shellId = nextShellId++;
  protected readonly countId = `qd-detail-modal-count-${this.shellId}`;

  private readonly restoreButton = viewChild<ElementRef<HTMLButtonElement>>('restoreButton');
  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');
  private readonly backButton = viewChild<ElementRef<HTMLButtonElement>>('backButton');
  private previousVisibility: 'open' | 'closed' | null = null;
  private previousDepth: number | null = null;

  private readonly lastFocusedByDepth = new Map<number, HTMLElement>();

  constructor() {
    effect(() => {
      const visibility = this.visibility();
      const depth = this.depth();
      const wasOpen = this.previousVisibility === 'open';
      const previousDepth = this.previousDepth;
      this.previousVisibility = visibility;
      this.previousDepth = depth;

      if (!wasOpen) {
        return;
      }

      if (visibility === 'closed') {
        setTimeout(() => this.restoreButton()?.nativeElement.focus(), 0);
        return;
      }

      if (previousDepth === null) {
        return;
      }

      if (depth < previousDepth) {
        this.restoreFocusAfterPop(depth);
        return;
      }

      if (depth > previousDepth) {
        this.focusFrameEntryControl();
      }
    });
  }

  protected onDialogFocusIn(event: FocusEvent): void {
    const target = event.target;
    if (this.visibility() === 'open' && target instanceof HTMLElement) {
      this.lastFocusedByDepth.set(this.depth(), target);
    }
  }

  private restoreFocusAfterPop(depth: number): void {
    const opener = this.lastFocusedByDepth.get(depth);
    for (const recordedDepth of Array.from(this.lastFocusedByDepth.keys())) {
      if (recordedDepth > depth) {
        this.lastFocusedByDepth.delete(recordedDepth);
      }
    }

    setTimeout(() => {
      if (this.visibility() !== 'open') {
        return;
      }
      if (opener?.isConnected) {
        opener.focus();
        return;
      }
      this.focusFallback();
    }, 0);
  }

  private focusFrameEntryControl(): void {
    setTimeout(() => {
      if (this.visibility() !== 'open') {
        return;
      }
      const active = document.activeElement;
      if (active instanceof HTMLElement && active.isConnected && active.closest('[role="dialog"]') !== null) {
        return;
      }
      const back = this.backButton()?.nativeElement;
      if (back?.isConnected) {
        back.focus();
        return;
      }
      this.focusFallback();
    }, 0);
  }

  private focusFallback(): void {
    const close = this.closeButton()?.nativeElement;
    if (close?.isConnected) {
      close.focus();
    }
  }
}
