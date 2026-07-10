export const EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS = 500;

export class ExplorerKeyboardNavScheduler<T> {
  private timer: ReturnType<typeof setTimeout> | null = null;
  private pendingTarget: T | null = null;

  constructor(
    private readonly callback: (target: T) => void,
    private readonly debounceMs = EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS,
  ) {}

  schedule(target: T): void {
    this.pendingTarget = target;
    this.clearTimer();
    this.timer = setTimeout(() => {
      this.timer = null;
      const next = this.pendingTarget;
      this.pendingTarget = null;
      if (next !== null) {
        this.callback(next);
      }
    }, this.debounceMs);
  }

  cancel(): void {
    this.pendingTarget = null;
    this.clearTimer();
  }

  hasPending(): boolean {
    return this.timer !== null;
  }

  private clearTimer(): void {
    if (this.timer !== null) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }
}
