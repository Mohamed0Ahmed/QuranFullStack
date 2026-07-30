import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabSelectionStore } from '../../state/abwab-selection.store';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { AbwabPageOverlaysController } from '../../state/abwab-page-overlays.controller';
import { filterAbwabRootsBySection, pruneAbwabNodesToVisible, searchAbwabNodes } from '../../state/abwab-tree.builder';
import { parseAbwabQueryParams, buildAbwabQueryParams } from '../../state/abwab-url-sync';
import { ABWAB_ORDER_SCOPE_TO_WIRE, AbwabNode, AbwabOrderScope, AbwabView } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabToolbarComponent } from '../../components/abwab-toolbar/abwab-toolbar.component';
import { AbwabTreeComponent, AbwabTreeMenuRequest } from '../../components/abwab-tree/abwab-tree.component';
import { AbwabCardsComponent } from '../../components/abwab-cards/abwab-cards.component';
import { AbwabArchiveViewComponent } from '../../components/abwab-archive-view/abwab-archive-view.component';
import { AbwabSidePanelComponent } from '../../components/abwab-side-panel/abwab-side-panel.component';
import { AbwabAnnouncerComponent } from '../../components/abwab-announcer/abwab-announcer.component';
import { AbwabDoorModalComponent } from '../../components/abwab-door-modal/abwab-door-modal.component';
import { AbwabMovePickerComponent } from '../../components/abwab-move-picker/abwab-move-picker.component';
import { AbwabSectionsModalComponent } from '../../components/abwab-sections-modal/abwab-sections-modal.component';
import { AbwabRelationsModalComponent } from '../../components/abwab-relations-modal/abwab-relations-modal.component';
import { ABWAB_ROUTE_PATH } from '../../../../core/navigation/route-paths';
import { QdContextMenuComponent } from '../../../../shared/ui/context-menu/context-menu.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';

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
    QdSkeletonRowsComponent,
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

  protected readonly sections = computed(() => this.facade.snapshot()?.sections ?? []);
  protected readonly byId = computed(() => this.facade.snapshot()?.byId ?? new Map<number, AbwabNode>());

  /** «كل الأبواب» (no active section) is the superset — its own, independent order (plan.md §4). */
  protected readonly orderScope = computed<AbwabOrderScope>(() => (this.activeSectionId() === null ? 'global' : 'section'));

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
  protected get editOpLabel(): string { return ABWAB_LABELS.editOp; }
  protected get addChildOpLabel(): string { return ABWAB_LABELS.addChildOp; }
  protected get moveOpLabel(): string { return ABWAB_LABELS.moveOp; }
  protected get relationsOpLabel(): string { return ABWAB_LABELS.relationsOp; }

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
      this.selection.setArchiveViewActive(parsed.archive);
      // The URL is the single source of truth for the selection, exactly as it already is
      // for view/archive/card/q. `buildAbwabQueryParams` drops `door` whenever the scope
      // changes (§4.4: a selection is not meaningful across scopes), so without this the
      // store would keep a door the URL has abandoned — and the side panel would keep
      // offering edit/move/archive on a door that is no longer on screen (M22).
      if (parsed.door === null) {
        this.selection.clearSelection();
      }
    });
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

  protected onMenuRequested(request: AbwabTreeMenuRequest): void {
    this.overlays.setContextMenuPosition(request.x, request.y);
    this.overlays.requestContextMenu(request.id);
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge' });
  }
}
