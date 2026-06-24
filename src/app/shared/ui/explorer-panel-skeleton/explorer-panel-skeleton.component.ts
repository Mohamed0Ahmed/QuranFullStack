import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'qd-explorer-panel-skeleton',
  standalone: true,
  templateUrl: './explorer-panel-skeleton.component.html',
  styleUrl: './explorer-panel-skeleton.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerPanelSkeletonComponent {
  readonly loadingLabel = input('جارٍ التحميل…');
}
