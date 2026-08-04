import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { splitGridTemplateColumns } from './grid-template-columns';

@Component({
  selector: 'qd-skeleton-rows',
  standalone: true,
  templateUrl: './skeleton-rows.component.html',
  styleUrl: './skeleton-rows.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdSkeletonRowsComponent {
  readonly count = input.required<number>();
  readonly rowTemplate = input.required<string>();
  readonly loadingLabel = input('جارٍ التحميل…');

  protected readonly rowIndexes = computed(() =>
    Array.from({ length: Math.max(0, this.count()) }, (_, index) => index),
  );

  protected readonly columnTracks = computed(() => splitGridTemplateColumns(this.rowTemplate()));
}
