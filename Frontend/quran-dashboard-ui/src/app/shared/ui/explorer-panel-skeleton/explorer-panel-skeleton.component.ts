import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { QdSkeletonRowsComponent } from '../skeleton/skeleton-rows.component';

export type QdPanelSkeletonShape = 'lines' | 'rows' | 'panel';

/**
 * Generalized loading skeleton for explorer/detail panels
 * (UI_STYLE_SYSTEM.md §17 `qd-panel-skeleton`).
 *
 * The `qd-explorer-panel-skeleton` selector is kept as a thin alias so
 * existing call-sites (e.g. `word-drilldown-modal`) keep working unchanged;
 * migrating them onto the `qd-panel-skeleton` name is a later phase. The
 * default `shape` ('lines') reproduces today's six-line panel skeleton
 * exactly, byte-for-byte, regardless of which selector matched.
 */
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

  /** Only used when `shape="rows"`. */
  readonly rowsCount = input(4);
  /** Only used when `shape="rows"`; a `grid-template-columns` value. */
  readonly rowTemplate = input('1fr');
}
