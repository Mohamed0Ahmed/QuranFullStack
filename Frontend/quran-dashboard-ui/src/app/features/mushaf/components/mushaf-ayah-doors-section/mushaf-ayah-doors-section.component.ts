import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';

import { AbwabDoorLinksPanelComponent } from '../../../abwab/components/abwab-door-links-panel/abwab-door-links-panel.component';
import { AbwabRelationsModalComponent } from '../../../abwab/components/abwab-relations-modal/abwab-relations-modal.component';
import { AbwabNode, AbwabRelationDirectionKind, AbwabRelationKind } from '../../../abwab/models/abwab.models';
import { ABWAB_LABELS } from '../../../abwab/models/abwab.labels';
import { AbwabDoorLinksFacade } from '../../../abwab/state/abwab-door-links.facade';
import { AbwabPermissionsController } from '../../../abwab/state/abwab-permissions.controller';
import { AbwabRelationsController } from '../../../abwab/state/abwab-relations.controller';
import { AbwabSnapshotFacade } from '../../../abwab/state/abwab-snapshot.facade';
import { buildAbwabNodePaths } from '../../../abwab/state/abwab-tree-paths';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { MushafAyahDoorsStore } from '../../state/mushaf-ayah-doors.store';
import type { QuranVerseKey } from '../../../../shared/quran/quran-location';

@Component({
  selector: 'qd-mushaf-ayah-doors-section',
  standalone: true,
  imports: [
    AbwabDoorLinksPanelComponent,
    AbwabRelationsModalComponent,
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdSkeletonRowsComponent,
  ],
  templateUrl: './mushaf-ayah-doors-section.component.html',
  styleUrl: './mushaf-ayah-doors-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AbwabPermissionsController],
})
export class MushafAyahDoorsSectionComponent implements OnDestroy {
  readonly verseKey = input<QuranVerseKey | null>(null);

  protected readonly store = inject(MushafAyahDoorsStore);
  protected readonly tree = inject(AbwabSnapshotFacade);
  protected readonly doorLinks = inject(AbwabDoorLinksFacade);
  protected readonly permissions = inject(AbwabPermissionsController);
  private readonly relations = inject(AbwabRelationsController);
  private readonly relationsDoorId = signal<number | null>(null);

  protected readonly rows = this.store.doors;
  protected readonly labels = ABWAB_LABELS;
  protected readonly loading = computed(
    () => this.store.loadState().isLoading || (this.tree.isLoading() && !this.tree.snapshot()),
  );
  protected readonly errorMessage = computed(
    () => this.store.loadState().errorMessage ?? (!this.tree.snapshot() ? this.tree.errorMessage() : null),
  );
  protected readonly openLinksDoor = computed(() => {
    const doorId = this.doorLinks.openDoorId();
    if (doorId === null || !this.store.relatedDoorIds().has(doorId)) {
      return null;
    }
    const door = this.tree.snapshot()?.byId.get(doorId) ?? null;
    return door?.isArchived === false ? door : null;
  });
  protected readonly relationsDoor = computed(() => {
    const doorId = this.relationsDoorId();
    const door = doorId === null ? null : this.tree.snapshot()?.byId.get(doorId) ?? null;
    return door?.isArchived === false ? door : null;
  });
  protected readonly liveRoots = computed(() => this.tree.snapshot()?.liveRoots ?? []);
  private readonly pathsById = computed(() => buildAbwabNodePaths(this.liveRoots()));

  protected readonly loadRelations = (doorId: number) => this.relations.loadFor(doorId);
  protected readonly refetchRelations = (doorId: number) => this.relations.refetchFor(doorId);
  protected readonly addRelations = (
    doorId: number,
    kind: AbwabRelationKind,
    direction: AbwabRelationDirectionKind | null,
    targetDoorIds: readonly number[],
  ) => this.relations.addRelations(doorId, kind, direction, targetDoorIds);
  protected readonly deleteRelation = (relationId: number) =>
    this.relations.deleteRelation(relationId);

  constructor() {
    effect(() => {
      const verseKey = this.verseKey();
      untracked(() => {
        this.doorLinks.close();
        this.relationsDoorId.set(null);
        this.store.load(verseKey);
      });
    });

    effect(() => {
      const openDoorId = this.doorLinks.openDoorId();
      const snapshot = this.tree.snapshot();
      if (openDoorId !== null && snapshot && this.openLinksDoor() === null) {
        untracked(() => this.doorLinks.close());
      }
    });

    effect(() => {
      const doorId = this.relationsDoorId();
      const snapshot = this.tree.snapshot();
      if (doorId !== null && snapshot && this.relationsDoor() === null) {
        untracked(() => this.relationsDoorId.set(null));
      }
    });
  }

  ngOnDestroy(): void {
    this.doorLinks.close();
    this.relationsDoorId.set(null);
  }

  protected retry(): void {
    if (this.store.loadState().errorMessage) {
      this.store.retry();
    }
    if (!this.tree.snapshot() && this.tree.errorMessage()) {
      this.tree.load();
    }
  }

  protected doorPath(door: AbwabNode): string {
    return this.pathsById().get(door.id) ?? door.name;
  }

  protected toggleLinks(door: AbwabNode): void {
    this.relationsDoorId.set(null);
    this.doorLinks.toggleDoor(door.id);
  }

  protected openRelations(door: AbwabNode): void {
    this.doorLinks.close();
    this.relationsDoorId.set(door.id);
  }

  protected closeRelations(): void {
    this.relationsDoorId.set(null);
  }

  protected revealRelation(doorId: number): void {
    const door = this.tree.snapshot()?.byId.get(doorId);
    if (door && !door.isArchived) {
      this.relationsDoorId.set(doorId);
    }
  }
}
