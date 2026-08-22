import { ChangeDetectionStrategy, Component, computed, input, output, signal, viewChild } from '@angular/core';

import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { searchAbwabNodes } from '../../state/abwab-tree-search';
import { AbwabSearchControlsComponent } from '../abwab-search-controls/abwab-search-controls.component';
import { AbwabTreeComponent } from '../abwab-tree/abwab-tree.component';

export type AbwabDoorPickerStatus = 'ready' | 'loading' | 'error' | 'empty';

const NO_IDS: ReadonlySet<number> = new Set<number>();

@Component({
  selector: 'qd-abwab-door-picker',
  standalone: true,
  imports: [
    AbwabSearchControlsComponent,
    AbwabTreeComponent,
    QdSkeletonRowsComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
  ],
  templateUrl: './abwab-door-picker.component.html',
  styleUrl: './abwab-door-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorPickerComponent {
  readonly nodes = input.required<readonly AbwabNode[]>();
  readonly pickedIds = input.required<readonly number[]>();
  readonly excludedIds = input<readonly number[]>([]);
  readonly disabledIds = input<readonly number[]>([]);
  readonly disabledTag = input('');
  readonly excludedTag = input('');
  readonly single = input(false);
  readonly status = input<AbwabDoorPickerStatus>('ready');
  readonly errorMessage = input('');
  readonly emptyMessage = input.required<string>();
  readonly searchPlaceholder = input.required<string>();
  readonly testIdPrefix = input.required<string>();

  readonly toggled = output<number>();
  readonly retry = output<void>();

  private readonly searchControls = viewChild(AbwabSearchControlsComponent);

  protected readonly searchQuery = signal('');
  protected readonly hideUnrelatedRoots = signal(true);
  private readonly expandedIds = signal<ReadonlySet<number>>(NO_IDS);
  protected readonly labels = ABWAB_LABELS;

  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get loadingLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }

  protected readonly doorsErrorMessage = computed(() => (this.status() === 'error' ? this.errorMessage() : ''));
  private readonly searchResult = computed(() => searchAbwabNodes(
    this.nodes(),
    this.searchQuery(),
    { hideUnrelatedRoots: this.hideUnrelatedRoots() },
  ));
  protected readonly displayRoots = computed(() => this.searchResult().displayRoots);
  protected readonly matchedIds = computed(() => this.searchResult().matchedIds);
  protected readonly searchExpandedIds = computed(() => this.searchResult().autoExpandedIds);
  protected readonly searchMatchCount = computed(() => this.searchResult().matchedIds.size);
  protected readonly searchFoundNothing = computed(() =>
    this.searchResult().isFiltering
    && this.hideUnrelatedRoots()
    && this.nodes().length > 0
    && this.searchResult().displayRoots.length === 0,
  );
  protected readonly pickedSet = computed<ReadonlySet<number>>(() => new Set(this.pickedIds()));
  protected readonly excludedSet = computed<ReadonlySet<number>>(() => new Set(this.excludedIds()));
  protected readonly disabledSet = computed<ReadonlySet<number>>(() => new Set(this.disabledIds()));
  protected readonly searchMatches = computed(() => {
    const excluded = this.excludedSet();
    const disabled = this.disabledSet();
    return this.searchResult().matches.filter((node) => !excluded.has(node.id) && !disabled.has(node.id));
  });
  protected readonly selectedId = computed(() => this.single() ? (this.pickedIds()[0] ?? null) : null);
  protected readonly expandSeedIds = computed<ReadonlySet<number>>(() => {
    const excluded = this.excludedSet();
    if (excluded.size === 0) {
      return this.expandedIds();
    }
    const byId = new Map<number, AbwabNode>();
    const visit = (node: AbwabNode): void => {
      byId.set(node.id, node);
      node.children.forEach(visit);
    };
    this.nodes().forEach(visit);
    const next = new Set(this.expandedIds());
    for (const id of excluded) {
      let current = byId.get(id)?.parentId ?? null;
      while (current !== null) {
        next.add(current);
        current = byId.get(current)?.parentId ?? null;
      }
    }
    return next;
  });

  focusSearch(): void {
    this.searchControls()?.focusInput();
  }

  protected toggle(doorId: number): void {
    this.toggled.emit(doorId);
  }

  protected rememberExpanded(ids: ReadonlySet<number>): void {
    this.expandedIds.set(ids);
  }

  reset(): void {
    this.searchQuery.set('');
    this.hideUnrelatedRoots.set(true);
    this.expandedIds.set(NO_IDS);
  }
}
