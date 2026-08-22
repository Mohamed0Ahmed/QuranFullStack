import { ChangeDetectionStrategy, Component, computed, input, output, signal, viewChild } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { searchAbwabNodes } from '../../state/abwab-tree-search';
import { AbwabSearchControlsComponent } from '../abwab-search-controls/abwab-search-controls.component';

export type AbwabDoorPickerStatus = 'ready' | 'loading' | 'error' | 'empty';

let nextPickerId = 0;

interface AbwabDoorPickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
  readonly isMatched: boolean;
  readonly isDisabled: boolean;
  readonly isExcluded: boolean;
}

@Component({
  selector: 'qd-abwab-door-picker',
  standalone: true,
  imports: [
    AbwabSearchControlsComponent,
    QdActionDirective,
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
  protected readonly hideUnrelatedRoots = signal(false);
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly collapsedExcludedIds = signal<ReadonlySet<number>>(new Set());

  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get loadingLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }
  protected get linksLabel(): string { return ABWAB_LABELS.rowHeaderLinks; }
  protected get directChildrenLabel(): string { return ABWAB_LABELS.rowHeaderDirect; }
  protected get allDescendantsLabel(): string { return ABWAB_LABELS.rowHeaderTotal; }
  protected get depthLabel(): string { return ABWAB_LABELS.rowHeaderDepth; }
  protected get relationsLabel(): string { return ABWAB_LABELS.relationsFlagLabel; }
  protected readonly labels = ABWAB_LABELS;

  protected readonly pickerName = `abwab-door-picker-${nextPickerId++}`;

  protected readonly doorsErrorMessage = computed(() => (this.status() === 'error' ? this.errorMessage() : ''));

  private readonly searchResult = computed(() => searchAbwabNodes(
    this.nodes(),
    this.searchQuery(),
    { hideUnrelatedRoots: this.hideUnrelatedRoots() },
  ));
  protected readonly searchMatchCount = computed(() => this.searchResult().matchedIds.size);
  protected readonly searchFoundNothing = computed(() =>
    this.searchResult().isFiltering
    && this.hideUnrelatedRoots()
    && this.nodes().length > 0
    && this.searchResult().displayRoots.length === 0,
  );

  private readonly pickedSet = computed(() => new Set(this.pickedIds()));

  protected readonly rows = computed<readonly AbwabDoorPickerRow[]>(() => {
    const excluded = new Set(this.excludedIds());
    const disabled = new Set(this.disabledIds());
    const expanded = this.expandedIds();
    const searchExpanded = this.searchResult().autoExpandedIds;
    const collapsedExcluded = this.collapsedExcludedIds();
    const rows: AbwabDoorPickerRow[] = [];

    const walk = (node: AbwabNode, depth: number): void => {
      const hasChildren = node.children.length > 0;
      const isExcluded = excluded.has(node.id);
      const defaultExpanded = isExcluded ? !collapsedExcluded.has(node.id) : expanded.has(node.id);
      const isExpanded = defaultExpanded || searchExpanded.has(node.id);
      rows.push({
        node,
        depth,
        hasChildren,
        isExpanded,
        isMatched: this.searchResult().matchedIds.has(node.id),
        isDisabled: disabled.has(node.id),
        isExcluded,
      });
      if (isExpanded) {
        node.children.forEach((child) => walk(child, depth + 1));
      }
    };

    this.searchResult().displayRoots.forEach((root) => walk(root, 0));
    return rows;
  });

  focusSearch(): void {
    this.searchControls()?.focusInput();
  }

  protected isPicked(doorId: number): boolean {
    return this.pickedSet().has(doorId);
  }

  protected rowAriaLabel(row: AbwabDoorPickerRow): string {
    const tag = this.disabledTag();
    return row.isDisabled && tag !== '' ? `${row.node.name} — ${tag}` : row.node.name;
  }

  protected excludedRowAriaLabel(row: AbwabDoorPickerRow): string | null {
    const tag = this.excludedTag();
    if (!row.isExcluded) {
      return null;
    }
    return tag === '' ? row.node.name : `${row.node.name} — ${tag}`;
  }

  protected linksAriaLabel(node: AbwabNode): string {
    return ABWAB_LABELS.rowLinksAriaLabel(node.name, node.linkCount);
  }

  protected childCountAriaLabel(count: number): string {
    return ABWAB_LABELS.rowChildCountAriaLabel(count);
  }

  protected descendantCountAriaLabel(count: number): string {
    return ABWAB_LABELS.rowDescendantCountAriaLabel(count);
  }

  protected depthAriaLabel(depth: number): string {
    return ABWAB_LABELS.rowDepthAriaLabel(depth);
  }

  protected relationsAriaLabel(node: AbwabNode): string {
    return ABWAB_LABELS.rowRelationsAriaLabel(node.name, node.relationCount);
  }

  protected expandAriaLabel(row: AbwabDoorPickerRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(row.node.name);
  }

  protected toggleExpanded(event: Event, row: AbwabDoorPickerRow): void {
    event.stopPropagation();
    if (row.isExcluded) {
      const next = new Set(this.collapsedExcludedIds());
      if (!next.delete(row.node.id)) {
        next.add(row.node.id);
      }
      this.collapsedExcludedIds.set(next);
      return;
    }
    const next = new Set(this.expandedIds());
    if (!next.delete(row.node.id)) {
      next.add(row.node.id);
    }
    this.expandedIds.set(next);
  }

  protected togglePicked(row: AbwabDoorPickerRow): void {
    if (row.isDisabled || row.isExcluded) {
      return;
    }
    this.toggled.emit(row.node.id);
  }

  reset(): void {
    this.searchQuery.set('');
    this.hideUnrelatedRoots.set(false);
    this.expandedIds.set(new Set());
    this.collapsedExcludedIds.set(new Set());
  }
}
