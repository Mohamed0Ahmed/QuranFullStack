import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';

import { AbwabTreeComponent } from '../../../abwab/components/abwab-tree/abwab-tree.component';
import { AbwabSnapshotFacade } from '../../../abwab/state/abwab-snapshot.facade';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { MushafDoorColorSlot } from '../../models/mushaf-door-highlights.models';
import { MushafDoorsHighlightStore } from '../../state/mushaf-doors-highlight.store';

type MushafDoorsPanelTab = 'doors' | 'selected';

@Component({
  selector: 'qd-mushaf-doors-panel',
  standalone: true,
  imports: [
    AbwabTreeComponent,
    ExplorerPanelSkeletonComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdTabDirective,
    QdTabsComponent,
  ],
  templateUrl: './mushaf-doors-panel.component.html',
  styleUrls: ['./mushaf-doors-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MushafDoorsPanelComponent implements OnInit {
  protected readonly tree = inject(AbwabSnapshotFacade);
  protected readonly highlights = inject(MushafDoorsHighlightStore);
  protected readonly activeTab = signal<MushafDoorsPanelTab>('doors');

  ngOnInit(): void {
    this.tree.ensureLoaded();
  }

  protected selectTab(tab: MushafDoorsPanelTab): void {
    this.activeTab.set(tab);
  }

  protected toggleDoor(doorId: number): void {
    this.highlights.toggleDraftDoor(doorId);
  }

  protected confirmDoors(): void {
    this.highlights.confirmDraft();
    this.activeTab.set('selected');
  }

  protected setDoorColor(doorId: number, colorSlot: MushafDoorColorSlot): void {
    this.highlights.setDoorColor(doorId, colorSlot);
  }

  protected retryTree(): void {
    this.tree.load();
  }

  protected colorLabel(colorSlot: MushafDoorColorSlot): string {
    return `اللون ${colorSlot}`;
  }
}
