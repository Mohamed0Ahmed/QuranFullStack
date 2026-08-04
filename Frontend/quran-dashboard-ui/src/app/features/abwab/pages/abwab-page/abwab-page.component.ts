import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabSelectionStore } from '../../state/abwab-selection.store';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { AbwabPageOverlaysController } from '../../state/abwab-page-overlays.controller';
import { AbwabModalUrlController, DOOR_MODAL_KINDS } from '../../state/abwab-modal-url.controller';
import {
  countAbwabDoorsInOpenScope,
  countLiveAbwabDoors,
  filterAbwabRootsBySection,
  pruneAbwabNodesToVisible,
  searchAbwabNodes,
} from '../../state/abwab-tree.builder';
import { parseAbwabQueryParams, buildAbwabQueryParams } from '../../state/abwab-url-sync';
import {
  ABWAB_ORDER_SCOPE_TO_WIRE,
  AbwabModalKind,
  AbwabNode,
  AbwabOrderScope,
  AbwabView,
} from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabToolbarComponent } from '../../components/abwab-toolbar/abwab-toolbar.component';
import { AbwabTreeComponent, AbwabTreeMenuRequest } from '../../components/abwab-tree/abwab-tree.component';
import { AbwabCardsComponent } from '../../components/abwab-cards/abwab-cards.component';
import { AbwabArchiveViewComponent } from '../../components/abwab-archive-view/abwab-archive-view.component';
import { AbwabSidePanelComponent } from '../../components/abwab-side-panel/abwab-side-panel.component';
import { AbwabAnnouncerComponent } from '../../components/abwab-announcer/abwab-announcer.component';
import { AbwabDoorModalComponent } from '../../components/abwab-door-modal/abwab-door-modal.component';
import {
  AbwabMoveDestination,
  AbwabMovePickerComponent,
} from '../../components/abwab-move-picker/abwab-move-picker.component';
import { AbwabDoorRestoreModalComponent } from '../../components/abwab-door-restore-modal/abwab-door-restore-modal.component';
import { AbwabSectionsModalComponent } from '../../components/abwab-sections-modal/abwab-sections-modal.component';
import { AbwabModalRestoreComponent } from '../../components/abwab-modal-restore/abwab-modal-restore.component';
import { AbwabRelationsModalComponent } from '../../components/abwab-relations-modal/abwab-relations-modal.component';
import { ABWAB_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';

const NO_IDS: ReadonlySet<number> = new Set<number>();

const NO_ROOTS: readonly AbwabNode[] = [];

const REVEAL_HOLD_MS = 3000;

@Component({
  selector: 'qd-abwab-page',
  standalone: true,
  imports: [
    RouterLink,
    AbwabToolbarComponent,
    AbwabTreeComponent,
    AbwabCardsComponent,
    AbwabArchiveViewComponent,
    AbwabSidePanelComponent,
    AbwabAnnouncerComponent,
    AbwabDoorModalComponent,
    AbwabMovePickerComponent,
    AbwabDoorRestoreModalComponent,
    AbwabSectionsModalComponent,
    AbwabRelationsModalComponent,
    AbwabModalRestoreComponent,
    QdContextMenuComponent,
    ExplorerResultCountComponent,
    QdSkeletonRowsComponent,
    QdStateComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './abwab-page.component.html',
  styleUrl: './abwab-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AbwabPageOverlaysController, AbwabModalUrlController],
})
export class AbwabPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly facade = inject(AbwabSnapshotFacade);
  protected readonly selection = inject(AbwabSelectionStore);
  protected readonly writeController = inject(AbwabWriteController);
  protected readonly overlays = inject(AbwabPageOverlaysController);
  protected readonly modalUrl = inject(AbwabModalUrlController);

  protected readonly templatesRoutePath = `/${ABWAB_ROUTE_PATH}/templates`;

  private readonly doorParam = signal<number | null>(null);
  protected readonly activeSectionId = signal<number | null>(null);
  protected readonly viewParam = signal<AbwabView>('tree');
  protected readonly archiveParam = signal(false);
  protected readonly cardParam = signal<number | null>(null);
  protected readonly searchQueryParam = signal('');

  private readonly modalRestoreControl = viewChild(AbwabModalRestoreComponent);
  private readonly headerFallbackFocus = viewChild<ElementRef<HTMLButtonElement>>('headerFallbackFocus');

  protected readonly sections = computed(() => this.facade.snapshot()?.sections ?? []);
  protected readonly byId = computed(() => this.facade.snapshot()?.byId ?? new Map<number, AbwabNode>());

  protected readonly rootCountBySectionId = computed(
    () => this.facade.snapshot()?.rootCountBySectionId ?? new Map<number, number>(),
  );
  protected readonly totalRootCount = computed(() => this.facade.snapshot()?.liveRoots.length ?? 0);

  protected readonly orderScope = computed<AbwabOrderScope>(() => (this.activeSectionId() === null ? 'global' : 'section'));

  protected readonly totalLiveDoorsCount = computed(() => countLiveAbwabDoors(this.byId()));
  protected readonly openScopeDoorsCount = computed(() =>
    countAbwabDoorsInOpenScope(this.sections(), this.activeSectionId(), this.totalLiveDoorsCount()),
  );

  protected readonly visibleRoots = computed(() => {
    const snapshot = this.facade.snapshot();
    return snapshot ? filterAbwabRootsBySection(snapshot.liveRoots, this.activeSectionId()) : [];
  });

  private readonly searchResult = computed(() => searchAbwabNodes(this.visibleRoots(), this.searchQueryParam()));

  protected readonly displayRoots = computed(() => {
    const result = this.searchResult();
    return result.isFiltering ? pruneAbwabNodesToVisible(this.visibleRoots(), result.visibleIds) : this.visibleRoots();
  });

  protected readonly treeMatchedIds = computed(() => this.searchResult().matchedIds);

  protected readonly searchMatchCount = computed(() =>
    this.archiveParam() ? this.archiveSearchResult().matchedIds.size : this.searchResult().matchedIds.size,
  );

  private readonly revealTargetId = signal<number | null>(null);
  private readonly revealSequence = signal(0);
  protected readonly revealedId = signal<number | null>(null);
  protected readonly revealAnnouncement = signal<string | null>(null);
  private revealTimer: ReturnType<typeof setTimeout> | null = null;
  private revealPending = false;

  private doorModalCommitted = false;

  protected readonly revealExpandSeedIds = computed<ReadonlySet<number>>(() => {
    this.revealSequence();
    const targetId = this.revealTargetId();
    if (targetId === null) {
      return NO_IDS;
    }
    const byId = this.byId();
    const chain = new Set<number>();
    let parentId = byId.get(targetId)?.parentId ?? null;
    while (parentId !== null && !chain.has(parentId)) {
      chain.add(parentId);
      parentId = byId.get(parentId)?.parentId ?? null;
    }
    return chain;
  });

  protected readonly expandSeedIds = computed<ReadonlySet<number>>(() => {
    const reveal = this.revealExpandSeedIds();
    const search = this.searchResult().autoExpandedIds;
    if (reveal.size === 0) {
      return search.size === 0 ? NO_IDS : search;
    }
    if (search.size === 0) {
      return reveal;
    }
    return new Set([...reveal, ...search]);
  });

  protected readonly pickerLiveRoots = computed<readonly AbwabNode[]>(() => this.facade.snapshot()?.liveRoots ?? NO_ROOTS);

  protected readonly archivedRoots = computed(() => this.facade.snapshot()?.archivedRoots ?? []);
  private readonly archiveSearchResult = computed(() => searchAbwabNodes(this.archivedRoots(), this.searchQueryParam()));
  protected readonly displayArchivedRoots = computed(() => {
    const result = this.archiveSearchResult();
    return result.isFiltering
      ? pruneAbwabNodesToVisible(this.archivedRoots(), result.visibleIds)
      : this.archivedRoots();
  });

  protected readonly bulkSelectedIds = computed(() => new Set(this.selection.bulkSet().keys()));
  protected readonly bulkNames = computed(() => {
    const snapshot = this.facade.snapshot();
    return Array.from(this.selection.bulkSet().keys()).map((id) => snapshot?.byId.get(id)?.name ?? String(id));
  });

  protected readonly selectedDoor = this.overlays.selectedDoor;

  protected get pageTitle(): string { return ABWAB_LABELS.pageTitle; }
  protected get pageSubtitle(): string { return ABWAB_LABELS.pageSubtitle; }
  protected get addRootLabel(): string { return ABWAB_LABELS.addRootDoorButton; }
  protected get addRootGhostLabel(): string { return ABWAB_LABELS.addRootGhost; }
  protected get archiveButtonLabel(): string { return ABWAB_LABELS.archiveButton; }
  protected get manageSectionsLabel(): string { return ABWAB_LABELS.manageSectionsButton; }
  protected get templatesLabel(): string { return ABWAB_LABELS.templatesButton; }
  protected get treeAriaLabel(): string { return ABWAB_LABELS.treeAriaLabel; }
  protected get archiveTreeAriaLabel(): string { return ABWAB_LABELS.archiveTreeAriaLabel; }
  protected get emptyLabel(): string { return ABWAB_LABELS.emptyTreeMessage; }
  protected get archiveEmptyLabel(): string { return ABWAB_LABELS.archiveEmptyMessage; }
  protected get loadingLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get archiveLabel(): string { return ABWAB_LABELS.archiveOp; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get archiveConfirmTitle(): string { return ABWAB_LABELS.archiveConfirmTitle; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get editOpLabel(): string { return ABWAB_LABELS.editOp; }
  protected get addChildOpLabel(): string { return ABWAB_LABELS.addChildOp; }
  protected get moveOpLabel(): string { return ABWAB_LABELS.moveOp; }
  protected get relationsOpLabel(): string { return ABWAB_LABELS.relationsOp; }
  protected get statAllDoorsLabel(): string { return ABWAB_LABELS.allDoorsTab; }
  protected get statOpenScopeLabel(): string { return ABWAB_LABELS.statOpenScopeDoors; }

  constructor() {
    effect(() => {
      const doorId = this.doorParam();
      const snapshot = this.facade.snapshot();
      if (doorId === null || !snapshot) {
        return;
      }
      const node = snapshot.byId.get(doorId);
      if (node) {
        untracked(() => this.selection.select(doorId, node.version));
      }
    });

    effect(() => {
      this.modalUrl.modal();
      this.facade.snapshot();
      this.doorParam();
      this.selectedDoor();
      untracked(() => this.modalUrl.reconcileOpen());
    });
  }

  ngOnInit(): void {
    this.facade.load();
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const parsed = parseAbwabQueryParams(params);
      this.activeSectionId.set(parsed.section);
      this.doorParam.set(parsed.door);
      this.viewParam.set(parsed.view);
      this.archiveParam.set(parsed.archive);
      this.cardParam.set(parsed.card);
      this.searchQueryParam.set(parsed.q);
      this.modalUrl.syncFromUrl(parsed.modal, parsed.door);
      this.selection.setArchiveViewActive(parsed.archive);
      if (parsed.door === null) {
        this.selection.clearSelection();
      }

      this.revealAnnouncement.set(null);
      const revealTarget = this.revealTargetId();
      if (this.revealPending && revealTarget !== null && parsed.door === revealTarget) {
        this.revealPending = false;
        this.startReveal(revealTarget);
      }
    });
    this.destroyRef.onDestroy(() => this.clearRevealTimer());
  }

  protected onSectionChanged(sectionId: number | null): void {
    this.updateQueryParams(buildAbwabQueryParams({ section: sectionId }));
  }

  protected onViewChanged(view: AbwabView): void {
    this.updateQueryParams(buildAbwabQueryParams({ view }));
  }

  protected onSearchQueryChanged(q: string): void {
    this.updateQueryParams(buildAbwabQueryParams({ q }));
  }

  protected onCardDrilled(id: number): void {
    this.updateQueryParams(buildAbwabQueryParams({ card: id }));
  }

  protected onCardCrumbSelected(id: number | null): void {
    this.updateQueryParams(buildAbwabQueryParams({ card: id }));
  }

  protected onArchiveToggle(): void {
    this.updateQueryParams(buildAbwabQueryParams({ archive: !this.archiveParam() }));
  }

  protected onTreeSelected(doorId: number): void {
    const node = this.facade.snapshot()?.byId.get(doorId);
    if (!node) {
      return;
    }
    this.selection.select(doorId, node.version);
    this.updateQueryParams(buildAbwabQueryParams({ door: doorId }));
  }

  protected onClearSelection(): void {
    this.selection.clearSelection();
    this.updateQueryParams(buildAbwabQueryParams({ door: null }));
  }

  protected onBulkModeToggled(on: boolean): void {
    this.selection.setBulkMode(on);
  }

  protected onBulkToggled(id: number): void {
    const node = this.byId().get(id);
    if (!node) {
      return;
    }
    this.selection.toggleBulk(id, node.version);
  }

  protected onBulkClearRequested(): void {
    this.selection.clearBulk();
  }

  protected confirmArchiveAndClearUrl(): void {
    this.overlays.confirmArchive(() => {
      this.updateQueryParams(buildAbwabQueryParams({ door: null }));
      this.focusTreeRovingItem();
    });
  }

  protected onBulkArchiveConfirmed(): void {
    this.overlays.confirmBulkArchive(() => this.focusTreeRovingItem());
  }

  protected onArchiveConfirmCancelled(): void {
    const cameFromContextMenu = this.overlays.archiveCameFromContextMenu();
    this.overlays.cancelArchiveConfirm();
    if (cameFromContextMenu) {
      this.focusTreeRovingItem();
    }
  }

  protected onBulkArchiveConfirmCancelled(): void {
    this.overlays.cancelBulkArchiveConfirm();
  }

  private focusTreeRovingItem(): void {
    this.focusQueued(() => {
      const root = this.elementRef.nativeElement;
      const roving = root.querySelector<HTMLElement>('[data-testid="abwab-tree"] [tabindex="0"]');
      if (roving) {
        roving.focus();
        return;
      }
      this.headerFallbackFocus()?.nativeElement.focus();
    });
  }

  protected onOrderCommitted(event: { id: number; position: number; scope: AbwabOrderScope }): void {
    const node = this.byId().get(event.id);
    if (!node) {
      return;
    }
    this.writeController
      .reorderDoor(event.id, {
        position: event.position,
        scope: ABWAB_ORDER_SCOPE_TO_WIRE[event.scope],
        version: node.version,
      })
      .subscribe();
  }

  protected onRestoreRequested(id: number): void {
    this.overlays.openRestoreModal(id);
  }

  protected onRelationsRequested(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!node) {
      return;
    }
    this.selection.select(doorId, node.version);
    this.overlays.openRelations();
    this.commitModalOpen('relations', doorId);
  }

  protected onRevealRequested(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!node || node.isArchived) {
      this.closeUrlBackedModal(['relations'], () => this.overlays.closeRelationsModal(), true);
      this.revealAnnouncement.set(ABWAB_LABELS.revealUnavailable);
      return;
    }
    // Read before the overlay closes — closing releases it.
    const anchorId = this.overlays.relationsAnchorDoorId();
    this.overlays.closeRelationsModal();
    this.modalUrl.releaseTracking();
    this.revealAnnouncement.set(null);
    this.revealTargetId.set(doorId);
    this.revealSequence.update((n) => n + 1);
    this.revealPending = true;
    this.updateQueryParams(
      buildAbwabQueryParams({
        door: doorId,
        modal: anchorId === null ? null : { kind: 'relations' as const, closed: true, subjectDoorId: anchorId },
        ...(this.activeSectionId() !== null && node.sectionId !== this.activeSectionId()
          ? { section: node.sectionId }
          : {}),
        ...(this.viewParam() === 'cards' ? { view: 'tree' as AbwabView } : {}),
      }),
    );
  }

  private startReveal(doorId: number): void {
    this.revealedId.set(doorId);
    this.clearRevealTimer();
    this.revealTimer = setTimeout(() => {
      this.revealedId.set(null);
      this.revealTargetId.set(null);
      this.revealTimer = null;
    }, REVEAL_HOLD_MS);
    queueMicrotask(() => {
      const row = this.elementRef.nativeElement.querySelector(`[data-testid="abwab-tree-row-${doorId}"]`);
      row?.scrollIntoView?.({ block: 'nearest' });
    });
  }

  private clearRevealTimer(): void {
    if (this.revealTimer !== null) {
      clearTimeout(this.revealTimer);
      this.revealTimer = null;
    }
  }

  protected onMenuRequested(request: AbwabTreeMenuRequest): void {
    this.overlays.setContextMenuPosition(request.x, request.y);
    this.overlays.requestContextMenu(request.id);
  }


  protected onCreateRootRequested(): void {
    this.modalUrl.open('create');
    this.commitModalOpen('create');
  }

  protected onSectionsRequested(): void {
    this.modalUrl.open('sections');
    this.commitModalOpen('sections');
  }

  protected onAddChildRequested(): void {
    this.openOnSelectedDoor('child');
  }

  protected onEditRequested(): void {
    this.openOnSelectedDoor('edit');
  }

  protected onMoveRequested(): void {
    this.openOnSelectedDoor('move');
  }

  protected onRelationsOpenRequested(): void {
    this.openOnSelectedDoor('relations');
  }

  protected onTreeAddChildRequested(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!node) {
      return;
    }
    this.modalUrl.open('child');
    this.commitModalOpen('child', doorId);
  }

  protected onCtxEdit(): void {
    this.overlays.ctxEdit((id) => this.commitModalOpen('edit', id));
  }

  protected onCtxAddChild(): void {
    this.overlays.ctxAddChild((id) => this.commitModalOpen('child', id));
  }

  protected onCtxMove(): void {
    this.overlays.ctxMove((id) => this.commitModalOpen('move', id));
  }

  protected onCtxRelations(): void {
    this.overlays.ctxRelations((id) => this.commitModalOpen('relations', id));
  }


  protected onDoorModalSaved(): void {
    this.doorModalCommitted = true;
  }

  protected onDoorModalClosed(): void {
    const committed = this.doorModalCommitted;
    this.doorModalCommitted = false;
    this.closeUrlBackedModal(DOOR_MODAL_KINDS, () => this.overlays.closeModal(), committed);
  }

  protected onMovePickerClosed(): void {
    this.closeUrlBackedModal(['move'], () => this.overlays.closeMovePicker());
  }

  protected onMoveConfirmed(destination: AbwabMoveDestination): void {
    this.closeUrlBackedModal(['move'], () => this.overlays.confirmMove(destination), true);
  }

  protected onSectionsModalClosed(): void {
    this.closeUrlBackedModal(['sections'], () => this.overlays.closeSectionsModal());
  }

  protected onRelationsModalClosed(): void {
    this.closeUrlBackedModal(['relations'], () => this.overlays.closeRelationsModal());
  }

  protected readonly retainedSubjectDoorName = computed(() => {
    const subjectDoorId = this.modalUrl.restorableModal()?.subjectDoorId ?? null;
    return subjectDoorId === null ? null : (this.byId().get(subjectDoorId)?.name ?? null);
  });

  protected onModalRestoreRequested(): void {
    const retained = this.modalUrl.restorableModal();
    if (retained === null) {
      return;
    }
    this.updateQueryParams(
      buildAbwabQueryParams({
        ...(retained.subjectDoorId === null ? {} : { door: retained.subjectDoorId }),
        modal: { kind: retained.kind, closed: false, subjectDoorId: null },
      }),
    );
  }

  protected onModalDiscardRequested(): void {
    this.updateQueryParams(buildAbwabQueryParams({ modal: null }), true);
    this.focusQueued(() => this.headerFallbackFocus()?.nativeElement.focus());
  }

  private closeUrlBackedModal(kinds: readonly AbwabModalKind[], close: () => void, discard = false): void {
    const kind = this.modalUrl.urlBackedKind(kinds);
    close();
    if (kind === null) {
      return;
    }
    this.modalUrl.releaseTracking();
    this.updateQueryParams(buildAbwabQueryParams({ modal: discard ? null : { kind, closed: true, subjectDoorId: null } }), true);
    if (!discard) {
      this.focusQueued(() => this.modalRestoreControl()?.focusRestore());
    }
  }

  private focusQueued(focus: () => void): void {
    setTimeout(focus, 0);
  }

  private openOnSelectedDoor(kind: AbwabModalKind): void {
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.modalUrl.open(kind);
    this.commitModalOpen(kind, door.id);
  }

  private commitModalOpen(kind: AbwabModalKind, doorId?: number): void {
    this.modalUrl.trackOpen(kind, doorId ?? null);
    this.updateQueryParams(
      buildAbwabQueryParams({
        ...(doorId === undefined ? {} : { door: doorId }),
        modal: { kind, closed: false, subjectDoorId: null },
      }),
    );
  }

  private updateQueryParams(changes: Record<string, string | null>, replaceUrl = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: changes,
      queryParamsHandling: 'merge',
      replaceUrl,
    });
  }
}
