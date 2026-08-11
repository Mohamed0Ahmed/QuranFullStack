import { A11yModule, CdkTrapFocus } from '@angular/cdk/a11y';
import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { QdActionDirective } from '../action/action.directive';
import { ScrollLockHandle, ScrollLockService } from '../modal-scroll-lock/scroll-lock.service';

export type QdModalShellVariant = 'confirm' | 'form' | 'wide' | 'overlay';
export type QdModalShellRole = 'dialog' | 'alertdialog';
export type QdModalShellDismissReason = 'close' | 'escape' | 'backdrop';

let nextShellId = 0;

const openShells = signal<readonly QdModalShellComponent[]>([]);

@Component({
  selector: 'qd-modal-shell',
  standalone: true,
  imports: [A11yModule, QdActionDirective],
  templateUrl: './modal-shell.component.html',
  styleUrl: './modal-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdModalShellComponent {
  private readonly scrollLock = inject(ScrollLockService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);

  readonly open = input(false);
  readonly variant = input<QdModalShellVariant>('form');
  readonly wideInlineSize = input<string | null>(null);
  readonly wideBlockSize = input<string | null>(null);
  readonly titleText = input.required<string>();
  readonly dialogRole = input<QdModalShellRole>('dialog');
  readonly describedById = input<string | null>(null);
  readonly showTitle = input(true);
  readonly showClose = input(true);
  readonly closeLabel = input('إغلاق');
  readonly hasFooter = input(true);
  readonly flushBody = input(false);
  readonly dismissOnBackdrop = input(true);
  readonly dismissOnEscape = input(true);
  readonly trapFocus = input(true);
  readonly returnFocus = input(true);
  readonly testIdPrefix = input('qd-modal-shell');
  readonly dialogTestId = input<string | null>(null);

  readonly dismissed = output<QdModalShellDismissReason>();

  private readonly instance = nextShellId++;
  readonly instanceId = `qd-modal-shell-${this.instance}`;
  protected readonly titleId = `${this.instanceId}-title`;

  private readonly trap = viewChild(CdkTrapFocus);

  protected readonly bareHeader = computed(() => !this.showTitle() && !this.showClose());

  protected readonly trapEnabled = computed(() => {
    const stack = openShells();
    return this.trapFocus() && stack[stack.length - 1] === this;
  });

  private handle: ScrollLockHandle | null = null;
  private focusReturnTarget: HTMLElement | null = null;
  private pointerDownTarget: EventTarget | null = null;

  constructor() {
    effect(() => {
      if (this.open()) {
        this.onOpened();
        return;
      }
      this.onClosed();
    });

    effect(() => {
      const trap = this.trap();
      if (!this.open() || trap === undefined) {
        return;
      }
      void trap.focusTrap.focusInitialElementWhenReady();
    });

    this.destroyRef.onDestroy(() => {
      const wasOpen = this.handle !== null;
      this.releaseLock();
      this.deregister();
      if (wasOpen) {
        this.restoreFocus();
      }
      this.focusReturnTarget = null;
    });
  }

  protected onBackdropPointerDown(event: Event): void {
    this.pointerDownTarget = event.target;
  }

  protected onBackdropClick(event: MouseEvent): void {
    const pressed = this.pointerDownTarget;
    this.pointerDownTarget = null;
    if (!this.dismissOnBackdrop() || event.target !== event.currentTarget) {
      return;
    }
    if (pressed !== null && pressed !== event.currentTarget) {
      return;
    }
    this.dismissed.emit('backdrop');
  }

  protected onEscape(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    if (!this.dismissOnEscape()) {
      return;
    }
    this.dismissed.emit('escape');
  }

  protected onCloseClick(): void {
    this.dismissed.emit('close');
  }

  private onOpened(): void {
    if (this.handle === null) {
      this.captureFocusReturnTarget();
      this.handle = this.scrollLock.hold();
      openShells.update((stack) => [...stack, this]);
    }
  }

  private onClosed(): void {
    if (this.handle === null) {
      return;
    }
    this.releaseLock();
    this.deregister();
    this.restoreFocus();
  }

  private deregister(): void {
    openShells.update((stack) => stack.filter((shell) => shell !== this));
  }

  private releaseLock(): void {
    this.handle?.release();
    this.handle = null;
  }

  private captureFocusReturnTarget(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const active = document.activeElement;
    this.focusReturnTarget = active instanceof HTMLElement && active !== document.body ? active : null;
  }

  private restoreFocus(): void {
    const target = this.focusReturnTarget;
    this.focusReturnTarget = null;
    if (!isPlatformBrowser(this.platformId) || target === null || !this.returnFocus()) {
      return;
    }
    if (target.isConnected) {
      target.focus();
    }
  }
}
