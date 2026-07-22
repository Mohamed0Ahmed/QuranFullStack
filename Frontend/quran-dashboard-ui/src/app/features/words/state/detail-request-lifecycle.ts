import { Subscription } from 'rxjs';

// Both slots (summary + detail) are abandoned together on transition, or a late detail response
// overwrites the new panel; and because a cache replays synchronously, callbacks must re-check isCurrent(token).
export class DetailRequestLifecycle {
  private generation = 0;
  private summarySub?: Subscription;
  private detailSub?: Subscription;

  beginTransition(): number {
    this.cancelAll();
    return this.generation;
  }

  isCurrent(token: number): boolean {
    return token === this.generation;
  }

  trackSummary(sub: Subscription | undefined): void {
    this.summarySub?.unsubscribe();
    this.summarySub = sub;
  }

  trackDetail(sub: Subscription | undefined): void {
    this.detailSub?.unsubscribe();
    this.detailSub = sub;
  }

  cancelAll(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
    this.generation += 1;
  }
}
