import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { of } from 'rxjs';

import {
  AbwabDoorLinksPanelComponent,
} from '../../../abwab/components/abwab-door-links-panel/abwab-door-links-panel.component';
import { AbwabRelationsModalComponent } from '../../../abwab/components/abwab-relations-modal/abwab-relations-modal.component';
import { AbwabToolbarComponent } from '../../../abwab/components/abwab-toolbar/abwab-toolbar.component';
import { AbwabTreeComponent } from '../../../abwab/components/abwab-tree/abwab-tree.component';
import { ABWAB_LABELS } from '../../../abwab/models/abwab.labels';
import { AbwabNode } from '../../../abwab/models/abwab.models';
import { AbwabDoorLinksFacade } from '../../../abwab/state/abwab-door-links.facade';
import { AbwabPermissionsController } from '../../../abwab/state/abwab-permissions.controller';
import { AbwabRelationsController } from '../../../abwab/state/abwab-relations.controller';
import { AbwabSnapshotFacade } from '../../../abwab/state/abwab-snapshot.facade';
import { searchAbwabNodes } from '../../../abwab/state/abwab-tree-search';
import { abwabPermissionDenied } from '../../../abwab/state/abwab-write.controller';
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

const NO_IDS: ReadonlySet<number> = new Set<number>();
const NO_ROOTS: readonly AbwabNode[] = [];
const REVEAL_HOLD_MS = 3000;

interface DoorPalettePopover {
  readonly door: MushafAppliedDoorViewModel;
  readonly anchor: HTMLElement;
  readonly position: { readonly x: number; readonly y: number };
}

@Component({
  selector: 'qd-mushaf-doors-panel',
  standalone: true,
  imports: [
    AbwabDoorLinksPanelComponent,
    AbwabRelationsModalComponent,
    AbwabToolbarComponent,
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
  providers: [AbwabPermissionsController],
})
export class MushafDoorsPanelComponent implements OnInit, OnDestroy {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  protected readonly tree = inject(AbwabSnapshotFacade);
  protected readonly doorLinks = inject(AbwabDoorLinksFacade);
  protected readonly highlights = inject(MushafDoorsHighlightStore);
  private readonly relations = inject(AbwabRelationsController);
  protected readonly activeTab = signal<MushafDoorsPanelTab>('doors');
  protected readonly palettePopover = signal<DoorPalettePopover | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly hideUnrelatedRoots = signal(false);
  protected readonly revealedId = signal<number | null>(null);
  private readonly revealSeedId = signal<number | null>(null);
  private readonly relationsDoorId = signal<number | null>(null);
  private revealTimer: ReturnType<typeof setTimeout> | null = null;

  protected readonly liveRoots = computed<readonly AbwabNode[]>(
    () => this.tree.snapshot()?.liveRoots ?? NO_ROOTS,
  );
  private readonly searchResult = computed(() => searchAbwabNodes(
    this.liveRoots(),
    this.searchQuery(),
    { hideUnrelatedRoots: this.hideUnrelatedRoots() },
  ));
  protected readonly displayRoots = computed(() => this.searchResult().displayRoots);
  protected readonly searchMatches = computed(() => Array.from(this.searchResult().matchedIds).flatMap((id) => {
    const node = this.tree.snapshot()?.byId.get(id);
    return node ? [node] : [];
  }));
  protected readonly matchedIds = computed(() => this.searchResult().matchedIds);
  protected readonly searchEmptyMessage = computed(() =>
    this.searchResult().isFiltering && this.hideUnrelatedRoots() && this.liveRoots().length > 0
      ? ABWAB_LABELS.noSearchMatchesMessage
      : 'لا توجد أبواب متاحة حاليًا.',
  );
  protected readonly searchExpandedIds = computed<ReadonlySet<number>>(() => {
    const ids = this.searchResult().autoExpandedIds;
    return ids.size === 0 ? NO_IDS : ids;
  });
  protected readonly revealExpandedIds = computed<ReadonlySet<number>>(() => {
    const doorId = this.revealSeedId();
    const byId = this.tree.snapshot()?.byId;
    if (doorId === null || !byId) {
      return NO_IDS;
    }
    const ids = new Set<number>();
    let parentId = byId.get(doorId)?.parentId ?? null;
    while (parentId !== null && !ids.has(parentId)) {
      ids.add(parentId);
      parentId = byId.get(parentId)?.parentId ?? null;
    }
    return ids.size === 0 ? NO_IDS : ids;
  });
  protected readonly openLinksDoor = computed(() => {
    const doorId = this.doorLinks.openDoorId();
    const door = doorId === null ? null : this.tree.snapshot()?.byId.get(doorId) ?? null;
    return door?.isArchived === false ? door : null;
  });
  protected readonly relationsDoor = computed(() => {
    const doorId = this.relationsDoorId();
    const door = doorId === null ? null : this.tree.snapshot()?.byId.get(doorId) ?? null;
    return door?.isArchived === false ? door : null;
  });

  protected readonly loadRelations = (doorId: number) => this.relations.loadFor(doorId);
  protected readonly refetchRelations = (doorId: number) => this.relations.refetchFor(doorId);
  protected readonly rejectRelationWrite = () => of(abwabPermissionDenied());

  constructor() {
    effect(() => {
      const doorId = this.doorLinks.openDoorId();
      const snapshot = this.tree.snapshot();
      if (doorId !== null && snapshot && this.openLinksDoor() === null) {
        untracked(() => this.doorLinks.close());
      }
    });

    effect(() => {
      const doorId = this.relationsDoorId();
      const snapshot = this.tree.snapshot();
      if (doorId !== null && snapshot && this.relationsDoor() === null) {
        untracked(() => this.closeRelations());
      }
    });
  }

  ngOnInit(): void {
    this.doorLinks.close();
    this.tree.ensureLoaded();
  }

  ngOnDestroy(): void {
    this.doorLinks.close();
    this.closeRelations();
    this.clearReveal();
  }

  protected selectTab(tab: MushafDoorsPanelTab): void {
    this.closePalette();
    if (tab !== 'doors') {
      this.doorLinks.close();
      this.closeRelations();
    }
    this.activeTab.set(tab);
  }

  protected toggleDoor(doorId: number): void {
    this.highlights.toggleDraftDoor(doorId);
  }

  protected confirmDoors(): void {
    this.highlights.confirmDraft();
    this.selectTab('selected');
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

  protected openRelations(doorId: number): void {
    const door = this.tree.snapshot()?.byId.get(doorId);
    if (!door || door.isArchived) {
      return;
    }
    this.doorLinks.close();
    this.relationsDoorId.set(door.id);
  }

  protected closeRelations(): void {
    this.relationsDoorId.set(null);
  }

  protected revealDoor(doorId: number): void {
    const door = this.tree.snapshot()?.byId.get(doorId);
    if (!door || door.isArchived) {
      return;
    }
    this.closeRelations();
    this.revealSeedId.set(door.id);
    this.revealedId.set(door.id);
    if (this.revealTimer !== null) {
      clearTimeout(this.revealTimer);
    }
    setTimeout(() => {
      const row = this.elementRef.nativeElement.querySelector<HTMLElement>(
        `[data-testid="abwab-tree-row-${door.id}"]`,
      );
      row?.scrollIntoView({ block: 'nearest' });
      row?.focus();
    });
    this.revealTimer = setTimeout(() => this.clearReveal(), REVEAL_HOLD_MS);
  }

  private clearReveal(): void {
    if (this.revealTimer !== null) {
      clearTimeout(this.revealTimer);
      this.revealTimer = null;
    }
    this.revealedId.set(null);
    this.revealSeedId.set(null);
  }
}
