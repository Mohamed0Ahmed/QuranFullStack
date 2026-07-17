import { Subscription } from 'rxjs';

/**
 * The two-slot request lifecycle shared by every words detail controller
 * (Feature 029, Change B4).
 *
 * A detail controller keeps two requests in flight — the entity summary and the
 * active detail/drilldown view — and both belong to ONE complete identity.
 * Cancelling only the summary on an identity transition leaves the previous
 * entity's detail request live, so its late response populates or overwrites the
 * newly selected entity's panel: Quran ayahs, words, counts, or morphology
 * rendered under the wrong identity. {@link beginTransition} therefore abandons
 * both slots together.
 *
 * Cancellation alone is not sufficient. A shared cache replays its buffered
 * value synchronously on subscribe, and the view loaders answer an
 * already-held read (cached missing surahs) by invoking the handler directly
 * with no subscription to cancel. Every callback must therefore re-check
 * {@link isCurrent} with its generation token before it touches panel state.
 */
export class DetailRequestLifecycle {
  private generation = 0;
  private summarySub?: Subscription;
  private detailSub?: Subscription;

  /**
   * Abandons both request slots and opens the next generation, returning that
   * generation's token. The token stays current only until the next transition
   * or cancellation, so a callback holding it can always tell whether it still
   * owns the panel.
   */
  beginTransition(): number {
    this.cancelAll();
    return this.generation;
  }

  /** True while `token` still denotes the identity the controller is showing. */
  isCurrent(token: number): boolean {
    return token === this.generation;
  }

  /** Holds the summary request of the current generation. */
  trackSummary(sub: Subscription | undefined): void {
    this.summarySub?.unsubscribe();
    this.summarySub = sub;
  }

  /**
   * Holds the detail/drilldown request of the current generation, replacing an
   * earlier view read of the same generation — a summary hands over to its view
   * load without a transition, and a loader that resolves from held state
   * returns no subscription at all.
   */
  trackDetail(sub: Subscription | undefined): void {
    this.detailSub?.unsubscribe();
    this.detailSub = sub;
  }

  /**
   * Abandons both slots and retires the current generation, so any response
   * that was already in flight becomes a no-op instead of writing into a panel
   * nobody is driving any more.
   */
  cancelAll(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
    this.generation += 1;
  }
}
