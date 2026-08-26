import { Injectable } from '@angular/core';
import { Subscription } from 'rxjs';

@Injectable()
export class PhraseActionRequestGate {
  private epoch = 0;
  private subscription?: Subscription;

  begin(): number {
    this.invalidate();
    return this.epoch;
  }

  track(epoch: number, subscription: Subscription): void {
    if (!this.isCurrent(epoch)) {
      subscription.unsubscribe();
      return;
    }
    this.subscription = subscription;
  }

  isCurrent(epoch: number): boolean {
    return epoch === this.epoch;
  }

  invalidate(): void {
    this.epoch += 1;
    this.subscription?.unsubscribe();
    this.subscription = undefined;
  }
}
