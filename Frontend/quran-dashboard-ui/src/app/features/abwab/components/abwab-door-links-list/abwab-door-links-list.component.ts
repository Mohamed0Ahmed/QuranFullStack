import {
  ScrollingModule,
  VIRTUAL_SCROLL_STRATEGY,
} from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { AbwabDoorLinkRecordView } from '../../models/abwab-door-links.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinksFacade } from '../../state/abwab-door-links.facade';
import { MeasuredRowVirtualScrollStrategy } from '../../../../shared/ui/virtual-scroll/measured-row-virtual-scroll.strategy';
import { AbwabDoorLinkRecordComponent } from '../abwab-door-link-record/abwab-door-link-record.component';

const ESTIMATED_RECORD_SIZE = 112;
const RECORD_BUFFER = 960;

@Component({
  selector: 'qd-abwab-door-links-list',
  standalone: true,
  imports: [ScrollingModule, AbwabDoorLinkRecordComponent],
  providers: [
    {
      provide: VIRTUAL_SCROLL_STRATEGY,
      useFactory: (): MeasuredRowVirtualScrollStrategy =>
        new MeasuredRowVirtualScrollStrategy(ESTIMATED_RECORD_SIZE, RECORD_BUFFER),
    },
  ],
  templateUrl: './abwab-door-links-list.component.html',
  styleUrl: './abwab-door-links-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorLinksListComponent {
  readonly canSelect = input(false);

  protected readonly facade = inject(AbwabDoorLinksFacade);
  protected readonly records = this.facade.recordViews;
  protected readonly state = this.facade.state;
  protected readonly listLabel = ABWAB_LABELS.doorLinksHeading;
  protected readonly trackRecord = (_index: number, record: AbwabDoorLinkRecordView): number =>
    record.summary.unitId;

  protected isSelected(unitId: number): boolean {
    const selection = this.state().selection;
    const listed = selection.unitIds.includes(unitId);
    return selection.mode === 'only' ? listed : !listed;
  }
}
