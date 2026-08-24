import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import {
  AbwabNode,
  AbwabRelationGroupKey,
  AbwabRelationVm,
  abwabRelationGroupKey,
} from '../../models/abwab.models';
import { buildAbwabNodePaths } from '../../state/abwab-tree-paths';
import { ABWAB_RELATIONS_MODAL_PRESENTATION } from './abwab-relations-modal.presentation';

type AbwabRelationsOverviewStatus = 'loading' | 'ready' | 'error';

@Component({
  selector: 'qd-abwab-relations-overview',
  standalone: true,
  imports: [QdActionDirective, QdSkeletonRowsComponent, QdTabDirective, QdTabsComponent],
  templateUrl: './abwab-relations-overview.component.html',
  styleUrl: './abwab-relations-overview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabRelationsOverviewComponent {
  readonly relations = input<readonly AbwabRelationVm[]>([]);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  readonly status = input<AbwabRelationsOverviewStatus>('ready');
  readonly activeGroupKey = input.required<AbwabRelationGroupKey>();
  readonly canCreateRelation = input(false);
  readonly canDeleteRelation = input(false);

  readonly activeGroupKeyChange = output<AbwabRelationGroupKey>();
  readonly addRequested = output<AbwabRelationGroupKey>();
  readonly revealRequested = output<number>();
  readonly deleteRequested = output<AbwabRelationVm>();

  protected readonly presentation = ABWAB_RELATIONS_MODAL_PRESENTATION;
  protected readonly tabs = this.presentation.overviewTabs;
  protected readonly panelId = 'abwab-relations-overview-panel';

  private readonly relationsByGroup = computed(() => {
    const groups = new Map<AbwabRelationGroupKey, AbwabRelationVm[]>(
      this.tabs.map((tab) => [tab.key, []]),
    );
    for (const relation of this.relations()) {
      groups.get(abwabRelationGroupKey(relation))?.push(relation);
    }
    return groups;
  });

  protected readonly activeRelations = computed(
    () => this.relationsByGroup().get(this.activeGroupKey()) ?? [],
  );
  protected readonly activeTab = computed(
    () => this.tabs.find((tab) => tab.key === this.activeGroupKey()) ?? this.tabs[0],
  );
  private readonly pathsById = computed(() => buildAbwabNodePaths(this.liveRoots()));

  protected countFor(key: AbwabRelationGroupKey): number {
    return this.relationsByGroup().get(key)?.length ?? 0;
  }

  protected tabId(key: AbwabRelationGroupKey): string {
    return `abwab-relations-overview-tab-${key}`;
  }

  protected doorPath(relation: AbwabRelationVm): string {
    return this.pathsById().get(relation.otherDoorId) ?? relation.otherDoorName;
  }
}
