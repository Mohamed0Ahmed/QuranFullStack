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
  AbwabModalState,
  AbwabNode,
  AbwabOrderScope,
  AbwabView,
  isDoorDependentAbwabModalKind,
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
import { AbwabSectionsModalComponent } from '../../components/abwab-sections-modal/abwab-sections-modal.component';
import { AbwabRelationsModalComponent } from '../../components/abwab-relations-modal/abwab-relations-modal.component';
import { ABWAB_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { ExplorerResultCountComponent } from '../../../../shared/ui/result-count/explorer-result-count.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';

/** Shared empty set, so `revealExpandSeedIds` keeps one identity while no reveal is running —
 * a fresh `new Set()` per evaluation would re-run the tree's expand effect on every tick. */
const NO_IDS: ReadonlySet<number> = new Set<number>();

/** Same reason as `NO_IDS`, for the two pickers fed `liveRoots`: an inline `?? []` in the
 * template allocates a fresh array on every change-detection tick, which marks both OnPush
 * modals dirty for as long as the snapshot is absent — the load window, and every error
 * state until a retry succeeds. */
const NO_ROOTS: readonly AbwabNode[] = [];

/** How long the reveal mark is held. Must match the animation duration in
 * `abwab-tree.component.scss`, which decays over the same span. */
const REVEAL_HOLD_MS = 3000;

/** The three modes the one door modal serves — which of them it is showing is the private
 * signal set the opener wrote, so the URL kind is the page's own record of it. */
const DOOR_MODAL_KINDS: readonly AbwabModalKind[] = ['create', 'child', 'edit'];

/**
 * Route shell for `/abwab` (plan-slice-b.md T415/T501-T511): URL ⇄ state wiring,
 * composing the toolbar, tree/cards, archive view, side panel, announcer and the
 * overlays (door modal, move picker, sections modal, archive confirms, context menu —
 * all owned by `AbwabPageOverlaysController`, split out once this file approached the
 * component-TS soft threshold). Every one of the six URL keys (`section`/`view`/
 * `archive`/`door`/`card`/`q`) is parsed in one subscription so no view is left reading
 * a param nobody restores (a gap phase 4 left: `view`/`archive`/`card`/`q` were parsed
 * and discarded, and `selection.setArchiveViewActive` was never called at all).
 */
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
    AbwabSectionsModalComponent,
    AbwabRelationsModalComponent,
    QdContextMenuComponent,
    ExplorerResultCountComponent,
    QdSkeletonRowsComponent,
    QdStateComponent,
  ],
  templateUrl: './abwab-page.component.html',
  styleUrl: './abwab-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AbwabPageOverlaysController],
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

  protected readonly templatesRoutePath = `/${ABWAB_ROUTE_PATH}/templates`;

  private readonly doorParam = signal<number | null>(null);
  protected readonly activeSectionId = signal<number | null>(null);
  protected readonly viewParam = signal<AbwabView>('tree');
  protected readonly archiveParam = signal(false);
  protected readonly cardParam = signal<number | null>(null);
  protected readonly searchQueryParam = signal('');
  protected readonly modalParam = signal<AbwabModalState | null>(null);

  private readonly modalRestoreControl = viewChild<ElementRef<HTMLButtonElement>>('modalRestoreControl');

  protected readonly sections = computed(() => this.facade.snapshot()?.sections ?? []);
  protected readonly byId = computed(() => this.facade.snapshot()?.byId ?? new Map<number, AbwabNode>());

  /** «كل الأبواب» (no active section) is the superset — its own, independent order (plan.md §4). */
  protected readonly orderScope = computed<AbwabOrderScope>(() => (this.activeSectionId() === null ? 'global' : 'section'));

  /** Item 17's stats bar (Slice B2, T1002) — both numbers, live-only by definition, §5.7. */
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

  protected readonly forceExpandedIds = computed(() => this.searchResult().autoExpandedIds);

  // Reveal-in-tree (audit item 10). `revealTargetId` is what the reveal is *about*;
  // `revealedId` is what currently carries the mark. They are separate because the mark is
  // released on a timer while the target survives until then, and because the mark must not
  // appear until the navigation it depends on has actually landed.
  private readonly revealTargetId = signal<number | null>(null);
  /** Bumped on every reveal, and read by the seed computed purely to invalidate it. Without
   * it, revealing the **same** door twice while the first reveal is still held is a no-op
   * write (`Object.is`), the seed never recomputes, and a chain the user collapsed in between
   * stays collapsed — leaving the reveal marking a row that is not on screen. */
  private readonly revealSequence = signal(0);
  protected readonly revealedId = signal<number | null>(null);
  protected readonly revealAnnouncement = signal<string | null>(null);
  private revealTimer: ReturnType<typeof setTimeout> | null = null;
  /** Set when a reveal navigates, cleared by the param emission that lands it. */
  private revealPending = false;

  /** Which overlay this page currently holds open **on the URL's behalf** — the one piece of
   * state that makes reconciliation a transition rather than a per-emission assertion. It is
   * null while a bulk gesture owns the move picker or the relations modal, which is what
   * keeps those session-transient overlays out of the URL's reach (plan-slice-e.md §2). */
  private openedModalKind: AbwabModalKind | null = null;

  /** Set by the door modal's `saved` output, read by the `closed` that follows it. */
  private doorModalCommitted = false;

  /** The target's ancestor chain, walked up `parentId` the way the cards breadcrumb does.
   * Handed to the tree as a **seed**, not a force: forced-open ids cannot be collapsed, and a
   * reveal that locked the chain open would trade one bug for a worse one. */
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

  /** The unfiltered live roots the move picker and the relations modal both browse. */
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

  /** The retained state the restore control renders from — null while the key is absent,
   * open, or pointing at a subject that cannot be restored (the same live guard the restore
   * effect applies), so an inert key shows no affordance at all. */
  protected readonly restorableModal = computed<AbwabModalState | null>(() => {
    const modal = this.modalParam();
    return modal !== null && modal.closed && this.canOpenModalKind(modal.kind) ? modal : null;
  });

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
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get editOpLabel(): string { return ABWAB_LABELS.editOp; }
  protected get addChildOpLabel(): string { return ABWAB_LABELS.addChildOp; }
  protected get moveOpLabel(): string { return ABWAB_LABELS.moveOp; }
  protected get relationsOpLabel(): string { return ABWAB_LABELS.relationsOp; }
  protected get statAllDoorsLabel(): string { return ABWAB_LABELS.allDoorsTab; }
  protected get statOpenScopeLabel(): string { return ABWAB_LABELS.statOpenScopeDoors; }
  protected modalRestoreLabel(kind: AbwabModalKind): string {
    return ABWAB_LABELS.modalRestoreLabel(ABWAB_LABELS.modalKindNames[kind]);
  }
  protected modalDiscardAriaLabel(kind: AbwabModalKind): string {
    return ABWAB_LABELS.modalDiscardAriaLabel(ABWAB_LABELS.modalKindNames[kind]);
  }

  constructor() {
    // Restores the `door` deep link once both the URL and the snapshot are ready —
    // whichever arrives second. `untracked` avoids the effect re-triggering on its own
    // `selection.select()` write (the door-modal reset effect had exactly this bug).
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

    // Audit item 11's restore ordering, and only the **open** half of reconciliation.
    // Declared after the `door=` effect so a deep link carrying both keys sees the selection
    // already bound in the same flush. The three reads below exist to re-run this effect
    // when the subject settles; the decision itself is `untracked`, because reconciliation
    // only ever reads the URL into state.
    effect(() => {
      const modal = this.modalParam();
      this.facade.snapshot();
      this.doorParam();
      this.selectedDoor();
      untracked(() => this.reconcileModalOpen(modal !== null && !modal.closed ? modal.kind : null));
    });
  }

  /** Both halves act only on the delta between what the page holds open and what the URL
   * asks for, which is what makes the echo of a gesture's own patch a no-op — re-opening an
   * already-open door modal would reset the form under the user.
   *
   * Closing is driven by the param emission and **not** by the effect: between a gesture's
   * navigation and the emission that lands it, the URL still says nothing, and an effect
   * that read that as "close everything" would shut the modal the click just opened. The
   * emission fires exactly when the URL genuinely changed, which is the only moment a close
   * can be inferred from it. */
  private reconcileModalClose(target: AbwabModalKind | null): void {
    if (this.openedModalKind === null || target === this.openedModalKind) {
      return;
    }
    this.closeOverlayFor(this.openedModalKind);
    this.openedModalKind = null;
  }

  private reconcileModalOpen(target: AbwabModalKind | null): void {
    if (target === null || this.openedModalKind !== null || !this.canOpenModalKind(target)) {
      return;
    }
    this.openOverlayFor(target);
    this.openedModalKind = target;
  }

  /** Door-dependent kinds need a subject that is bound **and live**. `byId` holds archived
   * nodes and the `door=` effect checks presence only, so a crafted `modal=edit` on an
   * archived door would otherwise open an editor over something the tree does not show.
   * The fail-closed outcome is the one a dead `door=` already produces: nothing happens and
   * the key sits inert. */
  private canOpenModalKind(kind: AbwabModalKind): boolean {
    if (!isDoorDependentAbwabModalKind(kind)) {
      return this.facade.snapshot() !== null;
    }
    const doorId = this.doorParam();
    if (doorId === null) {
      return false;
    }
    const node = this.byId().get(doorId);
    return !!node && !node.isArchived && this.selectedDoor()?.id === doorId;
  }

  private openOverlayFor(kind: AbwabModalKind): void {
    switch (kind) {
      case 'create':
        this.overlays.openCreateRoot();
        return;
      case 'child':
        this.overlays.openCreateChild();
        return;
      case 'edit':
        this.overlays.openEdit();
        return;
      case 'move':
        this.overlays.openMovePicker();
        return;
      case 'sections':
        this.overlays.openSectionsModal();
        return;
      case 'relations':
        this.overlays.openRelations();
        return;
    }
  }

  private closeOverlayFor(kind: AbwabModalKind): void {
    switch (kind) {
      case 'create':
      case 'child':
      case 'edit':
        this.overlays.closeModal();
        return;
      case 'move':
        this.overlays.closeMovePicker();
        return;
      case 'sections':
        this.overlays.closeSectionsModal();
        return;
      case 'relations':
        this.overlays.closeRelationsModal();
        return;
    }
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
      this.modalParam.set(parsed.modal);
      this.reconcileModalClose(parsed.modal !== null && !parsed.modal.closed ? parsed.modal.kind : null);
      this.selection.setArchiveViewActive(parsed.archive);
      // The URL is the single source of truth for the selection, exactly as it already is
      // for view/archive/card/q. `buildAbwabQueryParams` drops `door` whenever the scope
      // changes (§4.4: a selection is not meaningful across scopes), so without this the
      // store would keep a door the URL has abandoned — and the side panel would keep
      // offering edit/move/archive on a door that is no longer on screen (M22).
      if (parsed.door === null) {
        this.selection.clearSelection();
      }

      // Any navigation retires a stale reveal message, so a failed reveal cannot outlive the
      // next thing that happens and mask a write's own announcement.
      this.revealAnnouncement.set(null);
      // `revealPending` rather than "is nothing marked yet": within the hold window any other
      // navigation (a keystroke writing `q`, say) also emits with `door` still equal to the
      // target, and re-arming the mark and the scroll off that would be wrong.
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
      // No sentinel version: a fabricated token cannot succeed, and bulk is all-or-nothing,
      // so one bogus entry would 409 the whole operation with nothing the user can act on.
      return;
    }
    this.selection.toggleBulk(id, node.version);
  }

  protected onBulkClearRequested(): void {
    this.selection.clearBulk();
  }

  protected confirmArchiveAndClearUrl(): void {
    this.overlays.confirmArchive(() => this.updateQueryParams(buildAbwabQueryParams({ door: null })));
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
    const node = this.byId().get(id);
    if (!node) {
      return;
    }
    this.writeController.restoreDoor(id, node.version).subscribe();
  }

  /** Select-then-act (README "The URL is the single source of truth for the selection"), in the
   * `runContextAction` order: the store is written **synchronously** before `openRelations()`,
   * which reads `selectedDoor()`. Waiting for the URL subscription to echo `door=` back would
   * open the modal on whatever was selected before. */
  protected onRelationsRequested(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!node) {
      return;
    }
    this.selection.select(doorId, node.version);
    this.overlays.openRelations();
    this.commitModalOpen('relations', doorId);
  }

  /**
   * Reveal a related door in the tree (audit item 10). Every state the target can be in is
   * folded into **one** query patch, so there is one navigation and no race between them:
   * `door` always; `section` when the target lives elsewhere (an explicit `door` in the same
   * change overrides the scope-invalidation clear, `abwab-url-sync.ts`); `view: 'tree'` when
   * the cards drill is open, since the item is reveal-in-*tree*; and `q` cleared when a search
   * is filtering, because a reveal that leaves its target pruned breaks the promise the click
   * makes.
   *
   * The archived/missing guard is defensively unreachable — the relations read hides any
   * relation whose endpoint is archived — and is kept anyway, so an impossible state is a
   * visible non-action rather than a silent broken reveal.
   */
  protected onRevealRequested(doorId: number): void {
    this.overlays.closeRelationsModal();
    const node = this.byId().get(doorId);
    if (!node || node.isArchived) {
      this.revealAnnouncement.set(ABWAB_LABELS.revealUnavailable);
      return;
    }
    this.revealAnnouncement.set(null);
    this.revealTargetId.set(doorId);
    this.revealSequence.update((n) => n + 1);
    this.revealPending = true;
    this.updateQueryParams(
      buildAbwabQueryParams({
        door: doorId,
        // Only when the active tab genuinely excludes the target. «كل الأبواب» shows every
        // door, section-less ones included, so switching to the target's own tab there would
        // narrow the view for no reason; a *section* tab that isn't the target's does exclude
        // it, and then the reveal lands where the door actually lives.
        ...(this.activeSectionId() !== null && node.sectionId !== this.activeSectionId()
          ? { section: node.sectionId }
          : {}),
        ...(this.viewParam() === 'cards' ? { view: 'tree' as AbwabView } : {}),
        ...(this.searchQueryParam() !== '' ? { q: '' } : {}),
      }),
    );
  }

  /** Keyed off the param emission rather than the click: the rows that must exist for the
   * scroll to find anything are rendered by the change detection that emission triggers, and
   * in the cross-section / cards / search cases they do not exist before it at all. */
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
      // jsdom gives elements no `scrollIntoView`, and the reveal must not throw where it is
      // simply unavailable — the mark and the expand are the parts that carry the behavior.
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

  // Audit item 11 — the restorable overlays. Every gesture below opens its overlay
  // **synchronously**, exactly as it did before the key existed, and writes `modal=<kind>`
  // in the same patch as any selection it changes. The URL is a record of what is open, not
  // the mechanism that opens it: waiting for the emission would make every click wait on a
  // navigation, and `openedModalKind` makes the echo a no-op when it arrives.

  protected onCreateRootRequested(): void {
    this.openOverlayFor('create');
    this.commitModalOpen('create');
  }

  protected onSectionsRequested(): void {
    this.openOverlayFor('sections');
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

  /** The tree's `＋` selects the row and then asks for the child modal as two separate
   * outputs (its own contract since Slice B), so this patch restates `door` rather than
   * relying on the selection write that just preceded it. */
  protected onTreeAddChildRequested(doorId: number): void {
    const node = this.byId().get(doorId);
    if (!node) {
      return;
    }
    this.openOverlayFor('child');
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

  // Closing keeps the overlay in the URL in a retained state, so the page can offer it back
  // (the words overlay's contract, `detail-overlay-history.service.ts:125-141`). Retains and
  // discards both replace, so the closed state never becomes its own Back target; only
  // opening and restoring push.

  protected onDoorModalSaved(): void {
    this.doorModalCommitted = true;
  }

  protected onDoorModalClosed(): void {
    // A saved door has nothing left to restore — offering to reopen the form it was written
    // in would point at work that is already committed.
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

  protected onModalRestoreRequested(): void {
    const modal = this.modalParam();
    if (modal === null || !modal.closed) {
      return;
    }
    // A push, so Back returns to the closed state rather than skipping past it.
    this.updateQueryParams(buildAbwabQueryParams({ modal: { kind: modal.kind, closed: false } }));
  }

  protected onModalDiscardRequested(): void {
    this.updateQueryParams(buildAbwabQueryParams({ modal: null }), true);
  }

  /** `kinds` is what this overlay can be holding on the URL's behalf. A mismatch means a bulk
   * gesture opened it — the key was never written, so closing must not write one either. */
  private closeUrlBackedModal(kinds: readonly AbwabModalKind[], close: () => void, discard = false): void {
    const kind = this.openedModalKind;
    close();
    if (kind === null || !kinds.includes(kind)) {
      return;
    }
    this.openedModalKind = null;
    this.updateQueryParams(buildAbwabQueryParams({ modal: discard ? null : { kind, closed: true } }), true);
    if (!discard) {
      this.focusRestoreControl();
    }
  }

  /** Queued, not immediate: the control does not exist until the retained state renders, and
   * the modal's focus trap is still releasing on this tick. Same shape as the words shell's
   * `detail-modal-shell.component.ts:91-95`, which solved the identical race. */
  private focusRestoreControl(): void {
    setTimeout(() => this.modalRestoreControl()?.nativeElement.focus(), 0);
  }

  private openOnSelectedDoor(kind: AbwabModalKind): void {
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.openOverlayFor(kind);
    this.commitModalOpen(kind, door.id);
  }

  private commitModalOpen(kind: AbwabModalKind, doorId?: number): void {
    this.openedModalKind = kind;
    this.updateQueryParams(
      buildAbwabQueryParams({
        ...(doorId === undefined ? {} : { door: doorId }),
        modal: { kind, closed: false },
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
