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

/**
 * Presentation-only accessible dialog shell for the global detail overlay
 * (Feature 029, Change B). It owns dialog semantics (role/aria-modal/RTL,
 * labelled heading, focus trap, Escape/backdrop dismissal), the Back/Close
 * header actions, the closed-state restore control, and reference-counted body
 * scroll locking. It owns no entity, API, URL, or history state — the host
 * decides what the actions mean.
 */
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
  /** Polite status text (e.g. the eight-frame cap refusal); announced when set. */
  readonly statusMessage = input('');
  /** Entity-kind chip beside the title; the chip is omitted while empty. */
  readonly kindLabel = input('');
  /** Header count meta; its box stays reserved while this is empty. */
  readonly countText = input('');

  readonly backRequested = output<void>();
  readonly closeRequested = output<void>();
  readonly restoreRequested = output<void>();

  private readonly shellId = nextShellId++;
  protected readonly headingId = `qd-detail-modal-title-${this.shellId}`;
  protected readonly countId = `qd-detail-modal-count-${this.shellId}`;

  private readonly restoreButton = viewChild<ElementRef<HTMLButtonElement>>('restoreButton');
  private previousVisibility: 'open' | 'closed' | null = null;

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

    // Close (Escape/backdrop/Close button) keeps a retained stack; focus moves to
    // the restore control so keyboard users can immediately reopen it. The very
    // first render never steals focus.
    effect(() => {
      const visibility = this.visibility();
      const wasOpen = this.previousVisibility === 'open';
      this.previousVisibility = visibility;
      if (visibility === 'closed' && wasOpen) {
        setTimeout(() => this.restoreButton()?.nativeElement.focus(), 0);
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
}
