import { Injectable, inject } from '@angular/core';

import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingWorkspaceAddResult } from '../models/linking-workspace-add.models';
import { linkingSourceKey } from '../utils/linking-source-key';
import { LinkingAddFeedbackStore } from './linking-add-feedback.store';

type CanMutate = () => boolean;
type HasSource = (sourceKey: string) => boolean;
type EnqueueSource = (source: LinkingSourceDescriptor) => Promise<void>;

@Injectable({ providedIn: 'root' })
export class LinkingWorkspaceSourceAdder {
  private readonly feedback = inject(LinkingAddFeedbackStore);
  private readonly pendingSourceKeys = new Set<string>();
  private canMutate: CanMutate | null = null;
  private hasSource: HasSource | null = null;
  private enqueueSource: EnqueueSource | null = null;

  connect(canMutate: CanMutate, hasSource: HasSource, enqueueSource: EnqueueSource): void {
    this.canMutate = canMutate;
    this.hasSource = hasSource;
    this.enqueueSource = enqueueSource;
  }

  add(source: LinkingSourceDescriptor): LinkingWorkspaceAddResult | null {
    if (!this.canMutate?.() || this.hasSource === null || this.enqueueSource === null) {
      return null;
    }

    const sourceKey = linkingSourceKey(source);
    const alreadyPresent = this.hasSource(sourceKey) || this.pendingSourceKeys.has(sourceKey);
    const status = alreadyPresent ? 'already-present' : 'added';

    if (!alreadyPresent) {
      this.pendingSourceKeys.add(sourceKey);
      void this.enqueueSource(source).finally(() => this.pendingSourceKeys.delete(sourceKey));
    }

    this.feedback.show(status, source.label);
    return { sourceKey, status };
  }
}
