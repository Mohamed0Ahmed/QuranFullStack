import { A11yModule } from '@angular/cdk/a11y';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

import { ScrollLockService } from '../modal-scroll-lock/scroll-lock.service';

let nextShellId = 0;

@Component({
  selector: 'qd-detail-modal-shell',
  standalone: true,
  imports: [A11yModule],
  templateUrl: './detail-modal-shell.component.html',
  styleUrl: './detail-modal-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DetailModalShellComponent {
  private readonly scrollLock = inject(ScrollLockService);
  private readonly destroyRef = inject(DestroyRef);
  private holdsLock = false;

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
  protected readonly headingId = `qd-detail-modal-title-${this.shellId}`;
  protected readonly countId = `qd-detail-modal-count-${this.shellId}`;

  private readonly restoreButton = viewChild<ElementRef<HTMLButtonElement>>('restoreButton');
  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');
  private readonly backButton = viewChild<ElementRef<HTMLButtonElement>>('backButton');
  private readonly dialogHeading = viewChild<ElementRef<HTMLHeadingElement>>('dialogHeading');
  private previousVisibility: 'open' | 'closed' | null = null;
  private previousDepth: number | null = null;

  private readonly lastFocusedByDepth = new Map<number, HTMLElement>();

  constructor() {
    effect(() => {
      const open = this.visibility() === 'open';
      if (open && !this.holdsLock) {
        this.scrollLock.acquire();
        this.holdsLock = true;
      } else if (!open && this.holdsLock) {
        this.scrollLock.release();
        this.holdsLock = false;
      }
    });

    // The very first render never steals focus; afterwards every transition that
    // destroys the focused control must hand focus somewhere deterministic.
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

      // Close (Escape/backdrop/Close button) keeps a retained stack; focus moves
      // to the restore control so keyboard users can immediately reopen it.
      if (visibility === 'closed') {
        setTimeout(() => this.restoreButton()?.nativeElement.focus(), 0);
        return;
      }

      if (previousDepth === null) {
        return;
      }

      // A pop destroys the frame the user was standing in — and at depth one it
      // also destroys the Back button they pressed.
      if (depth < previousDepth) {
        this.restoreFocusAfterPop(depth);
        return;
      }

      // A push swaps the frame while the dialog stays open, destroying the link
      // that was activated to open it. The trap only holds focus that is already
      // inside it, so without this the same "focus left on the document" failure
      // the pop had happens on the way in.
      if (depth > previousDepth) {
        this.focusFrameEntryControl();
      }
    });

    this.destroyRef.onDestroy(() => {
      if (this.holdsLock) {
        this.scrollLock.release();
        this.holdsLock = false;
      }
    });
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeRequested.emit();
    }
  }

  protected onEscape(event: Event): void {
    event.stopPropagation();
    this.closeRequested.emit();
  }

  protected onDialogFocusIn(event: FocusEvent): void {
    const target = event.target;
    if (target instanceof HTMLElement) {
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
      return;
    }
    this.dialogHeading()?.nativeElement.focus();
  }
}
