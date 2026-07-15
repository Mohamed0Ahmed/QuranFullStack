import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { WORD_TYPES_PRESENCE_FILTER_LABELS } from '../../models/word-types.labels';
import { WordTypePresenceDimension } from '../../models/word-types.models';

export interface WordTypePresenceFlagChange {
  readonly dimension: WordTypePresenceDimension;
  readonly value: boolean | null;
}

/**
 * Tri-state presence filter for the Word Types list scope (Feature 026, US6). Presentational only:
 * renders a three-option chip group (any / has / missing) per dimension (root/stem/lemma, labels per
 * lock D) and emits the chosen flag; the page owns URL serialization and page reset.
 */
@Component({
  selector: 'qd-word-types-presence-filter',
  standalone: true,
  templateUrl: './word-types-presence-filter.component.html',
  styleUrl: './word-types-presence-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesPresenceFilterComponent {
  readonly hasRoot = input.required<boolean | null>();
  readonly hasStem = input.required<boolean | null>();
  readonly hasLemma = input.required<boolean | null>();
  readonly disabled = input(false);

  readonly flagChange = output<WordTypePresenceFlagChange>();

  // TDZ-safe getters (see words README): label consts read via readonly fields resolve to undefined
  // in the bundled test build.
  protected get labels() { return WORD_TYPES_PRESENCE_FILTER_LABELS; }
  protected get dimensions(): readonly WordTypePresenceDimension[] { return ['root', 'stem', 'lemma']; }

  protected valueFor(dimension: WordTypePresenceDimension): boolean | null {
    switch (dimension) {
      case 'root': return this.hasRoot();
      case 'stem': return this.hasStem();
      case 'lemma': return this.hasLemma();
    }
  }

  protected onSelect(dimension: WordTypePresenceDimension, value: boolean | null): void {
    if (this.disabled()) {
      return;
    }
    this.flagChange.emit({ dimension, value });
  }
}
