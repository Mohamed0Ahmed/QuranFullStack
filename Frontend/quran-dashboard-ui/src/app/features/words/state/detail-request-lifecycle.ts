import { Subscription } from 'rxjs';

// Two-slot request lifecycle shared by every words detail controller.
//
// A controller keeps two requests in flight — the entity summary and the active
// detail view — both belonging to ONE identity. Cancelling only the summary on an
// identity transition leaves the previous entity's detail request live, so its
// late response overwrites the newly selected panel (ayahs/words/counts under the
// wrong identity); beginTransition() abandons both slots together. Cancellation
// alone is not enough: a shared cache replays its buffered value synchronously on
// subscribe, and a loader answering an already-held read invokes the handler with
// no subscription to cancel — so every callback must re-check isCurrent(token)
// before touching panel state.
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

  // Replaces an earlier view read of the SAME generation: a summary hands over to
  // its view load without a transition, and a loader resolving from held state
  // returns no subscription at all.
  trackDetail(sub: Subscription | undefined): void {
    this.detailSub?.unsubscribe();
    this.detailSub = sub;
  }

  // Retires the current generation (generation++), so any in-flight response
  // becomes a no-op instead of writing into a panel nobody is driving.
  cancelAll(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
    this.generation += 1;
  }
}
