import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';

import { AbwabTreeComponent } from '../../../abwab/components/abwab-tree/abwab-tree.component';
import { AbwabSnapshotFacade } from '../../../abwab/state/abwab-snapshot.facade';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import {
  MushafAppliedDoorViewModel,
  MushafDoorColorSlot,
} from '../../models/mushaf-door-highlights.models';
import { MushafDoorsHighlightStore } from '../../state/mushaf-doors-highlight.store';
import { MushafDoorPaletteComponent } from './mushaf-door-palette.component';

type MushafDoorsPanelTab = 'doors' | 'selected';

interface DoorPalettePopover {
  readonly door: MushafAppliedDoorViewModel;
  readonly anchor: HTMLElement;
  readonly position: { readonly x: number; readonly y: number };
}

@Component({
  selector: 'qd-mushaf-doors-panel',
  standalone: true,
  imports: [
    AbwabTreeComponent,
    ExplorerPanelSkeletonComponent,
    QdActionDirective,
    QdContextMenuComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdTabDirective,
    QdTabsComponent,
    MushafDoorPaletteComponent,
  ],
  templateUrl: './mushaf-doors-panel.component.html',
  styleUrls: ['./mushaf-doors-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MushafDoorsPanelComponent implements OnInit {
  protected readonly tree = inject(AbwabSnapshotFacade);
  protected readonly highlights = inject(MushafDoorsHighlightStore);
  protected readonly activeTab = signal<MushafDoorsPanelTab>('doors');
  protected readonly palettePopover = signal<DoorPalettePopover | null>(null);

  ngOnInit(): void {
    this.tree.ensureLoaded();
  }

  protected selectTab(tab: MushafDoorsPanelTab): void {
    this.closePalette();
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
    this.closePalette();
  }

  protected openPalette(event: MouseEvent, door: MushafAppliedDoorViewModel): void {
    const anchor = event.currentTarget;
    if (!(anchor instanceof HTMLElement)) {
      return;
    }

    if (this.palettePopover()?.door.id === door.id) {
      this.closePalette();
      return;
    }

    const rect = anchor.getBoundingClientRect();
    this.palettePopover.set({
      door,
      anchor,
      position: { x: rect.right, y: rect.bottom },
    });
  }

  protected closePalette(): void {
    this.palettePopover.set(null);
  }

  protected removeDoor(event: MouseEvent, doorId: number): void {
    event.stopPropagation();
    if (this.palettePopover()?.door.id === doorId) {
      this.closePalette();
    }
    this.highlights.removeAppliedDoor(doorId);
  }

  protected retryTree(): void {
    this.tree.load();
  }
}
