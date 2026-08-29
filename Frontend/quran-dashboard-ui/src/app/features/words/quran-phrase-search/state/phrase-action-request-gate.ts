import { Injectable } from '@angular/core';
import { Subscription } from 'rxjs';

export type PhraseActionRequestTarget =
  | 'route'
  | 'query'
  | 'workspace'
  | 'branches'
  | 'results';

interface PhraseTrackedRequest {
  readonly epoch: number;
  readonly onInvalidate?: () => void;
  subscription?: Subscription;
}

@Injectable()
export class PhraseActionRequestGate {
  private readonly epochs = new Map<PhraseActionRequestTarget, number>();
  private readonly requests = new Map<PhraseActionRequestTarget, PhraseTrackedRequest>();

  begin(target: PhraseActionRequestTarget, onInvalidate?: () => void): number {
    this.invalidate(target);
    const epoch = this.currentEpoch(target);
    this.requests.set(target, { epoch, onInvalidate });
    return epoch;
  }

  track(
    target: PhraseActionRequestTarget,
    epoch: number,
    subscription: Subscription,
  ): void {
    const request = this.requests.get(target);
    if (!this.isCurrent(target, epoch) || request?.epoch !== epoch) {
      subscription.unsubscribe();
      return;
    }
    if (subscription.closed) {
      this.requests.delete(target);
      return;
    }
    request.subscription = subscription;
  }

  isCurrent(target: PhraseActionRequestTarget, epoch: number): boolean {
    return epoch === this.currentEpoch(target);
  }

  invalidate(target?: PhraseActionRequestTarget): void {
    if (target) {
      this.invalidateTarget(target);
      return;
    }
    const targets = new Set<PhraseActionRequestTarget>([
      ...this.epochs.keys(),
      ...this.requests.keys(),
    ]);
    for (const requestTarget of targets) {
      this.invalidateTarget(requestTarget);
    }
  }

  private invalidateTarget(target: PhraseActionRequestTarget): void {
    this.epochs.set(target, this.currentEpoch(target) + 1);
    const request = this.requests.get(target);
    this.requests.delete(target);
    if (!request || request.subscription?.closed) {
      return;
    }
    request.subscription?.unsubscribe();
    request.onInvalidate?.();
  }

  private currentEpoch(target: PhraseActionRequestTarget): number {
    return this.epochs.get(target) ?? 0;
  }
}
