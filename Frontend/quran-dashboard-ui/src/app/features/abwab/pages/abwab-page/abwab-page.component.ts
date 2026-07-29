import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AbwabSnapshotFacade } from '../../state/abwab-snapshot.facade';
import { AbwabSelectionStore } from '../../state/abwab-selection.store';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { filterAbwabRootsBySection } from '../../state/abwab-tree.builder';
import { parseAbwabQueryParams, buildAbwabQueryParams } from '../../state/abwab-url-sync';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabToolbarComponent } from '../../components/abwab-toolbar/abwab-toolbar.component';
import { AbwabTreeComponent } from '../../components/abwab-tree/abwab-tree.component';
import { AbwabSidePanelComponent } from '../../components/abwab-side-panel/abwab-side-panel.component';
import { AbwabAnnouncerComponent } from '../../components/abwab-announcer/abwab-announcer.component';
import { AbwabDoorModalComponent } from '../../components/abwab-door-modal/abwab-door-modal.component';

/**
 * Route shell for `/abwab` (plan-slice-b.md T415): URL ⇄ state wiring, composing the
 * toolbar, tree, side panel, announcer and door modal. Cards, bulk mode, the move
 * picker, the archive view, and sections management are phase-5 additions (T501–T511);
 * nothing for them is rendered here yet — a control with no working target is a dead
 * control.
 */
@Component({
  selector: 'qd-abwab-page',
  standalone: true,
  imports: [
    AbwabToolbarComponent,
    AbwabTreeComponent,
    AbwabSidePanelComponent,
    AbwabAnnouncerComponent,
    AbwabDoorModalComponent,
  ],
  templateUrl: './abwab-page.component.html',
  styleUrl: './abwab-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly facade = inject(AbwabSnapshotFacade);
  protected readonly selection = inject(AbwabSelectionStore);
  protected readonly writeController = inject(AbwabWriteController);

  private readonly doorParam = signal<number | null>(null);

  protected readonly sections = computed(() => this.facade.snapshot()?.sections ?? []);
  protected readonly activeSectionId = signal<number | null>(null);

  protected readonly visibleRoots = computed(() => {
    const snapshot = this.facade.snapshot();
    return snapshot ? filterAbwabRootsBySection(snapshot.liveRoots, this.activeSectionId()) : [];
  });

  protected readonly selectedDoor = computed<AbwabDoorDto | null>(() => {
    const id = this.selection.selectedDoorId();
    const node = id !== null ? this.facade.snapshot()?.byId.get(id) : undefined;
    if (!node) {
      return null;
    }
    return {
      id: node.id,
      name: node.name,
      description: node.description,
      representativeAyahText: node.representativeAyahText,
      aliases: [...node.aliases],
      parentId: node.parentId,
      sectionId: node.sectionId,
      orderValue: node.orderValue,
      version: node.version,
    };
  });

  protected readonly modalOpen = signal(false);
  protected readonly modalDoor = signal<AbwabDoorDto | null>(null);
  protected readonly modalParentId = signal<number | null>(null);
  protected readonly modalParentName = signal<string | null>(null);
  protected readonly archiveConfirming = signal(false);

  protected get pageTitle(): string { return ABWAB_LABELS.pageTitle; }
  protected get pageSubtitle(): string { return ABWAB_LABELS.pageSubtitle; }
  protected get addRootLabel(): string { return ABWAB_LABELS.addRootDoorButton; }
  protected get treeAriaLabel(): string { return ABWAB_LABELS.treeAriaLabel; }
  protected get emptyLabel(): string { return ABWAB_LABELS.emptyTreeMessage; }
  protected get loadingLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get archiveLabel(): string { return ABWAB_LABELS.archiveOp; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

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
    });
  }

  protected onSectionChanged(sectionId: number | null): void {
    this.updateQueryParams(buildAbwabQueryParams({ section: sectionId }));
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

  protected openCreateRoot(): void {
    this.modalDoor.set(null);
    this.modalParentId.set(null);
    this.modalParentName.set(null);
    this.modalOpen.set(true);
  }

  protected openCreateChild(): void {
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.modalDoor.set(null);
    this.modalParentId.set(door.id);
    this.modalParentName.set(door.name);
    this.modalOpen.set(true);
  }

  protected openEdit(): void {
    const door = this.selectedDoor();
    if (!door) {
      return;
    }
    this.modalDoor.set(door);
    this.modalParentId.set(null);
    this.modalParentName.set(null);
    this.modalOpen.set(true);
  }

  protected onModalClosed(): void {
    this.modalOpen.set(false);
  }

  protected onModalSaved(): void {
    // The write controller already refreshed the snapshot and rebound selection (§4.6);
    // the modal closes itself via its own `closed` emit on the same success path.
  }

  protected requestArchive(): void {
    if (this.selectedDoor()) {
      this.archiveConfirming.set(true);
    }
  }

  protected archiveConfirmMessage(): string {
    const door = this.selectedDoor();
    return door ? this.writeController.archiveConfirmMessageFor(door.id) : '';
  }

  protected confirmArchive(): void {
    const door = this.selectedDoor();
    this.archiveConfirming.set(false);
    if (!door) {
      return;
    }
    this.writeController.archiveDoor(door.id, door.version).subscribe((outcome) => {
      if (outcome.kind === 'success') {
        this.selection.clearSelection();
        this.updateQueryParams(buildAbwabQueryParams({ door: null }));
      }
    });
  }

  protected cancelArchiveConfirm(): void {
    this.archiveConfirming.set(false);
  }

  private updateQueryParams(changes: Record<string, string | null>): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge' });
  }
}
