import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { QdSkeletonRowsComponent } from '../skeleton/skeleton-rows.component';

export type QdPanelSkeletonShape = 'lines' | 'rows' | 'panel';

// `qd-explorer-panel-skeleton` is kept as a thin alias so existing call-sites keep
// working; default shape 'lines' reproduces the original six-line skeleton byte-for-byte.
@Component({
  selector: 'qd-panel-skeleton, qd-explorer-panel-skeleton',
  standalone: true,
  imports: [QdSkeletonRowsComponent],
  templateUrl: './explorer-panel-skeleton.component.html',
  styleUrl: './explorer-panel-skeleton.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.qd-panel-skeleton--panel-shape]': "shape() === 'panel'",
  },
})
export class ExplorerPanelSkeletonComponent {
  readonly loadingLabel = input('جارٍ التحميل…');
  readonly shape = input<QdPanelSkeletonShape>('lines');

  readonly rowsCount = input(4);
  readonly rowTemplate = input('1fr');
}
