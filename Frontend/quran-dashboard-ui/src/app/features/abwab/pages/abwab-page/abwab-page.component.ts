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
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabSelectionStore } from '../../state/abwab-selection.store';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { AbwabPageOverlaysController } from '../../state/abwab-page-overlays.controller';
import { AbwabModalUrlController } from '../../state/abwab-modal-url.controller';
import { AbwabPermissionsController } from '../../state/abwab-permissions.controller';
import { AbwabRevealController } from '../../state/abwab-reveal.controller';
import { AbwabPageInteractionsController } from '../../state/abwab-page-interactions.controller';
import {
  countAbwabDoorsInOpenScope,
  countLiveAbwabDoors,
  filterAbwabRootsBySection,
  pruneAbwabNodesToVisible,
  searchAbwabNodes,
} from '../../state/abwab-tree.builder';
import { parseAbwabQueryParams } from '../../state/abwab-url-sync';
import {
  AbwabNode,
  AbwabOrderScope,
  AbwabView,
} from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabToolbarComponent } from '../../components/abwab-toolbar/abwab-toolbar.component';
import { AbwabTreeComponent } from '../../components/abwab-tree/abwab-tree.component';
import { AbwabCardsComponent } from '../../components/abwab-cards/abwab-cards.component';
import { AbwabArchiveViewComponent } from '../../components/abwab-archive-view/abwab-archive-view.component';
import { AbwabSidePanelComponent } from '../../components/abwab-side-panel/abwab-side-panel.component';
import { AbwabAnnouncerComponent } from '../../components/abwab-announcer/abwab-announcer.component';
import { AbwabDoorModalComponent } from '../../components/abwab-door-modal/abwab-door-modal.component';
import { AbwabMovePickerComponent } from '../../components/abwab-move-picker/abwab-move-picker.component';
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
  providers: [
    AbwabPermissionsController,
    AbwabPageOverlaysController,
    AbwabModalUrlController,
    AbwabRevealController,
    AbwabPageInteractionsController,
  ],
})
export class AbwabPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly facade = inject(AbwabSnapshotFacade);
  protected readonly selection = inject(AbwabSelectionStore);
  protected readonly writeController = inject(AbwabWriteController);
  protected readonly overlays = inject(AbwabPageOverlaysController);
  protected readonly modalUrl = inject(AbwabModalUrlController);
  protected readonly permissions = inject(AbwabPermissionsController);
  protected readonly reveal = inject(AbwabRevealController);
  protected readonly interactions = inject(AbwabPageInteractionsController);

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

  protected readonly searchIsFiltering = computed(() => this.searchResult().isFiltering);
  protected readonly searchVisibleIds = computed(() => this.searchResult().visibleIds);

  protected readonly treeMatchedIds = computed(() => this.searchResult().matchedIds);

  protected readonly searchMatchCount = computed(() =>
    this.archiveParam() ? this.archiveSearchResult().matchedIds.size : this.searchResult().matchedIds.size,
  );

  protected readonly revealedId = this.reveal.revealedId;
  protected readonly revealAnnouncement = this.reveal.announcement;
  private readonly revealExpandSeedIds = this.reveal.expandSeedIds;

  protected readonly expandSeedIds = computed<ReadonlySet<number>>(() => {
    const reveal = this.revealExpandSeedIds();
    return reveal.size === 0 ? NO_IDS : reveal;
  });

  protected readonly searchExpandedIds = computed<ReadonlySet<number>>(() => {
    const search = this.searchResult().autoExpandedIds;
    return search.size === 0 ? NO_IDS : search;
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

  protected readonly archiveEmptyStateMessage = computed<string>(() =>
    this.archiveSearchResult().isFiltering && this.archivedRoots().length > 0
      ? ABWAB_LABELS.archiveNoSearchMatchesMessage
      : ABWAB_LABELS.archiveEmptyMessage,
  );

  protected readonly bulkSelectedIds = computed(() => new Set(this.selection.bulkSet().keys()));
  protected readonly bulkNames = computed(() => {
    const snapshot = this.facade.snapshot();
    return Array.from(this.selection.bulkSet().keys()).map((id) => snapshot?.byId.get(id)?.name ?? String(id));
  });

  protected readonly selectedDoor = this.overlays.selectedDoor;
  protected readonly canDoorModalSave = computed(() =>
    this.overlays.modalDoor() === null ? this.permissions.canCreateDoor() : this.permissions.canEditDoor(),
  );

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
      this.permissions.isResolved();
      this.modalUrl.modal();
      this.facade.snapshot();
      this.doorParam();
      this.selectedDoor();
      untracked(() => this.modalUrl.reconcileOpen());
    });

    effect(() => {
      if (!this.permissions.isResolved()) {
        return;
      }
      const unauthorizedModal = this.modalUrl.unauthorizedWriteModal();
      if (unauthorizedModal === null) {
        return;
      }
      untracked(() => this.interactions.clearUnauthorizedWriteModal());
    });

    effect(() => {
      const resolved = this.permissions.isResolved();
      this.permissions.canCreateDoor();
      this.permissions.canEditDoor();
      this.permissions.canMoveDoor();
      this.permissions.canRestoreDoor();
      this.permissions.canManageSections();
      this.permissions.canArchiveDoor();
      this.permissions.canUseBulkMode();
      if (!resolved) {
        return;
      }
      untracked(() => this.overlays.closeUnavailableWriteState());
    });

    effect(() => {
      const sectionId = this.activeSectionId();
      const snapshot = this.facade.snapshot();
      if (sectionId === null || !snapshot) {
        return;
      }
      if (snapshot.sections.some((section) => section.id === sectionId)) {
        return;
      }
      untracked(() => {
        this.activeSectionId.set(null);
        this.selection.setSectionScope(null);
        this.interactions.clearSectionQueryParam();
      });
    });
  }

  ngOnInit(): void {
    this.facade.load();
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const parsed = parseAbwabQueryParams(params);
      const door = parsed.archive ? null : parsed.door;
      this.activeSectionId.set(parsed.section);
      this.doorParam.set(door);
      this.viewParam.set(parsed.view);
      this.archiveParam.set(parsed.archive);
      this.cardParam.set(parsed.card);
      this.searchQueryParam.set(parsed.q);
      this.modalUrl.syncFromUrl(parsed.modal, door);
      this.selection.setArchiveViewActive(parsed.archive);
      this.selection.setSectionScope(parsed.section);
      if (door === null) {
        this.selection.clearSelection();
      }

      this.reveal.syncFromUrl(door, this.elementRef.nativeElement);
    });
    this.destroyRef.onDestroy(() => this.reveal.destroy());
  }

  protected confirmArchiveAndClearUrl(): void {
    this.interactions.confirmArchiveAndClearUrl(() => this.focusTreeRovingItem());
  }

  protected onBulkArchiveConfirmed(): void {
    this.interactions.onBulkArchiveConfirmed(() => this.focusTreeRovingItem());
  }

  protected onArchiveConfirmCancelled(): void {
    this.interactions.onArchiveConfirmCancelled(() => this.focusTreeRovingItem());
  }

  private focusTreeRovingItem(): void {
    this.focusRovingItem('abwab-tree');
  }

  private focusRovingItem(containerTestId: string): void {
    this.focusQueued(() => {
      const root = this.elementRef.nativeElement;
      const roving = root.querySelector<HTMLElement>(`[data-testid="${containerTestId}"] [tabindex="0"]`);
      if (roving) {
        roving.focus();
        return;
      }
      this.headerFallbackFocus()?.nativeElement.focus();
    });
  }

  protected onDoorRestored(): void {
    this.interactions.onDoorRestored(() => this.focusRovingItem('abwab-archive-view'));
  }

  protected onRevealRequested(doorId: number): void {
    this.interactions.onRevealRequested(doorId, this.activeSectionId(), this.viewParam());
  }

  protected onDoorModalClosed(): void {
    this.interactions.onDoorModalClosed(() => this.modalRestoreControl()?.focusRestore());
  }

  protected onMovePickerClosed(): void {
    this.interactions.onMovePickerClosed(() => this.modalRestoreControl()?.focusRestore());
  }

  protected onSectionsModalClosed(): void {
    this.interactions.onSectionsModalClosed(() => this.modalRestoreControl()?.focusRestore());
  }

  protected onRelationsModalClosed(): void {
    this.interactions.onRelationsModalClosed(() => this.modalRestoreControl()?.focusRestore());
  }

  protected readonly retainedSubjectDoorName = computed(() => {
    const subjectDoorId = this.modalUrl.restorableModal()?.subjectDoorId ?? null;
    return subjectDoorId === null ? null : (this.byId().get(subjectDoorId)?.name ?? null);
  });

  protected onModalRestoreRequested(): void {
    this.interactions.onModalRestoreRequested();
  }

  protected onModalDiscardRequested(): void {
    this.interactions.onModalDiscardRequested(() => this.headerFallbackFocus()?.nativeElement.focus());
  }

  private focusQueued(focus: () => void): void {
    setTimeout(focus, 0);
  }
}
