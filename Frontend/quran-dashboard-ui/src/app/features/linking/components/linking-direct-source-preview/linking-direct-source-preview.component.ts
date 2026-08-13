import { ScrollingModule, VIRTUAL_SCROLL_STRATEGY } from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { LinkingAyah } from '../../models/linking-ayah.models';
import { LINKING_LABELS } from '../../models/linking.labels';
import { MeasuredRowVirtualScrollStrategy } from '../../utils/measured-row-virtual-scroll.strategy';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';

const ESTIMATED_AYAH_ROW_SIZE = 156;
const AYAH_ROW_BUFFER = 720;

@Component({
  selector: 'qd-linking-direct-source-preview',
  standalone: true,
  imports: [ScrollingModule, LinkingAyahCardComponent],
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (): MeasuredRowVirtualScrollStrategy =>
        new MeasuredRowVirtualScrollStrategy(ESTIMATED_AYAH_ROW_SIZE, AYAH_ROW_BUFFER),
    },
  ],
  templateUrl: './linking-direct-source-preview.component.html',
  styleUrl: './linking-direct-source-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingDirectSourcePreviewComponent {
  readonly ayahs = input.required<readonly LinkingAyah[]>();
  readonly highlightSourceWords = input.required<boolean>();

  protected readonly labels = LINKING_LABELS;
  protected readonly trackAyah = (_index: number, ayah: LinkingAyah): string => ayah.verseKey;
}
