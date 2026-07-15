import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { splitGridTemplateColumns } from './grid-template-columns';

/**
 * Renders `count` skeleton rows, each a CSS grid using the caller-supplied
 * `rowTemplate`, so loading rows line up with the real, loaded row grid
 * exactly (UI_STYLE_SYSTEM.md §17 loading/skeleton system).
 *
 * Non-interactive: `aria-busy="true"` on the container plus a single
 * sr-only `role="status"` label; the per-row cells are `aria-hidden`.
 */
@Component({
  selector: 'qd-skeleton-rows',
  standalone: true,
  templateUrl: './skeleton-rows.component.html',
  styleUrl: './skeleton-rows.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdSkeletonRowsComponent {
  readonly count = input.required<number>();
  /** A `grid-template-columns` value, e.g. `"2.5rem 1fr auto"`. */
  readonly rowTemplate = input.required<string>();
  readonly loadingLabel = input('جارٍ التحميل…');

  protected readonly rowIndexes = computed(() =>
    Array.from({ length: Math.max(0, this.count()) }, (_, index) => index),
  );

  protected readonly columnTracks = computed(() => splitGridTemplateColumns(this.rowTemplate()));
}
