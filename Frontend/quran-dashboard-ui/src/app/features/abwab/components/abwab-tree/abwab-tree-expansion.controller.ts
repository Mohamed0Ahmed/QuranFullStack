import { signal } from '@angular/core';

import { AbwabNode } from '../../models/abwab.models';

export class AbwabTreeExpansionController {
  private readonly manualExpandedIdsSignal = signal<ReadonlySet<number>>(new Set());
  private readonly transientExpandedIdsSignal = signal<ReadonlySet<number>>(new Set());
  private readonly searchExpandedIdsSignal = signal<ReadonlySet<number>>(new Set());
  private readonly searchMatchedIdsSignal = signal<ReadonlySet<number>>(new Set());
  private readonly suppressedSearchIdsSignal = signal<ReadonlySet<number>>(new Set());

  effectiveIds(): ReadonlySet<number> {
    const manualIds = this.manualExpandedIdsSignal();
    const transientIds = this.transientExpandedIdsSignal();
    const searchIds = this.searchExpandedIdsSignal();
    const suppressedSearchIds = this.suppressedSearchIdsSignal();
    const effectiveSearchIds = suppressedSearchIds.size === 0
      ? searchIds
      : new Set([...searchIds].filter((id) => !suppressedSearchIds.has(id)));
    return transientIds.size === 0 && effectiveSearchIds.size === 0
      ? manualIds
      : new Set([...manualIds, ...transientIds, ...effectiveSearchIds]);
  }

  setSearchExpansion(expandedIds: ReadonlySet<number>, matchedIds: ReadonlySet<number>): void {
    const resultsChanged = !setsEqual(this.searchExpandedIdsSignal(), expandedIds)
      || !setsEqual(this.searchMatchedIdsSignal(), matchedIds);
    this.searchExpandedIdsSignal.set(new Set(expandedIds));
    this.searchMatchedIdsSignal.set(new Set(matchedIds));
    if (resultsChanged) {
      this.suppressedSearchIdsSignal.set(new Set());
    }
  }

  seed(ids: ReadonlySet<number>): void {
    if (ids.size > 0) {
      this.manualExpandedIdsSignal.update((current) => new Set([...current, ...ids]));
    }
  }

  setTransient(ids: ReadonlySet<number>): void {
    this.transientExpandedIdsSignal.set(new Set(ids));
  }

  setExpanded(id: number, expanded: boolean): ReadonlySet<number> {
    const next = new Set(this.manualExpandedIdsSignal());
    expanded ? next.add(id) : next.delete(id);
    this.manualExpandedIdsSignal.set(next);
    if (expanded) {
      this.removeSuppressedSearchIds([id]);
    } else {
      this.removeTransientIds([id]);
      this.addSuppressedSearchIds([id]);
    }
    return next;
  }

  expandAll(roots: readonly AbwabNode[]): ReadonlySet<number> {
    return this.addIds(collectIds(roots, true));
  }

  collapseAll(roots: readonly AbwabNode[]): ReadonlySet<number> {
    return this.removeIds(collectIds(roots, false));
  }

  expandBranch(node: AbwabNode): ReadonlySet<number> {
    return this.addIds(collectIds([node], true));
  }

  collapseBranch(node: AbwabNode): ReadonlySet<number> {
    return this.removeIds(collectIds([node], false));
  }

  canExpandAll(roots: readonly AbwabNode[]): boolean {
    const expandedIds = this.effectiveIds();
    return collectIds(roots, true).some((id) => !expandedIds.has(id));
  }

  canCollapseAll(roots: readonly AbwabNode[]): boolean {
    return this.hasExpandedId(collectIds(roots, true));
  }

  canExpandBranch(node: AbwabNode): boolean {
    const expandedIds = this.effectiveIds();
    return collectIds([node], true).some((id) => !expandedIds.has(id));
  }

  canCollapseBranch(node: AbwabNode): boolean {
    return this.hasExpandedId(collectIds([node], true));
  }

  private addIds(ids: readonly number[]): ReadonlySet<number> {
    const next = new Set(this.manualExpandedIdsSignal());
    ids.forEach((id) => next.add(id));
    this.manualExpandedIdsSignal.set(next);
    this.removeSuppressedSearchIds(ids);
    return next;
  }

  private removeIds(ids: readonly number[]): ReadonlySet<number> {
    const next = new Set(this.manualExpandedIdsSignal());
    ids.forEach((id) => next.delete(id));
    this.manualExpandedIdsSignal.set(next);
    this.removeTransientIds(ids);
    this.addSuppressedSearchIds(ids);
    return next;
  }

  private hasExpandedId(ids: readonly number[]): boolean {
    const expandedIds = this.effectiveIds();
    return ids.some((id) => expandedIds.has(id));
  }

  private removeTransientIds(ids: readonly number[]): void {
    const next = new Set(this.transientExpandedIdsSignal());
    ids.forEach((id) => next.delete(id));
    this.transientExpandedIdsSignal.set(next);
  }

  private addSuppressedSearchIds(ids: readonly number[]): void {
    const searchIds = this.searchExpandedIdsSignal();
    const next = new Set(this.suppressedSearchIdsSignal());
    ids.forEach((id) => {
      if (searchIds.has(id)) {
        next.add(id);
      }
    });
    this.suppressedSearchIdsSignal.set(next);
  }

  private removeSuppressedSearchIds(ids: readonly number[]): void {
    const next = new Set(this.suppressedSearchIdsSignal());
    ids.forEach((id) => next.delete(id));
    this.suppressedSearchIdsSignal.set(next);
  }
}

export class AbwabTreeExpansionCommands {
  constructor(
    private readonly expansion: AbwabTreeExpansionController,
    private readonly roots: () => readonly AbwabNode[],
    private readonly nodeById: (id: number) => AbwabNode | undefined,
    private readonly commit: (expandedIds: ReadonlySet<number>) => void,
  ) {}

  canExpandAll(): boolean { return this.expansion.canExpandAll(this.roots()); }
  canCollapseAll(): boolean { return this.expansion.canCollapseAll(this.roots()); }
  canExpandBranch(id: number): boolean {
    const node = this.nodeById(id);
    return node ? this.expansion.canExpandBranch(node) : false;
  }
  canCollapseBranch(id: number): boolean {
    const node = this.nodeById(id);
    return node ? this.expansion.canCollapseBranch(node) : false;
  }
  expandAll(): void { this.commit(this.expansion.expandAll(this.roots())); }
  collapseAll(): void { this.commit(this.expansion.collapseAll(this.roots())); }
  expandBranch(id: number): void { this.commitBranch(id, (node) => this.expansion.expandBranch(node)); }
  collapseBranch(id: number): void { this.commitBranch(id, (node) => this.expansion.collapseBranch(node)); }

  private commitBranch(id: number, change: (node: AbwabNode) => ReadonlySet<number>): void {
    const node = this.nodeById(id);
    if (node) {
      this.commit(change(node));
    }
  }
}

function collectIds(nodes: readonly AbwabNode[], expandableOnly: boolean): number[] {
  const ids: number[] = [];
  const visit = (node: AbwabNode): void => {
    if (!expandableOnly || node.children.length > 0) {
      ids.push(node.id);
    }
    node.children.forEach(visit);
  };
  nodes.forEach(visit);
  return ids;
}

function setsEqual(left: ReadonlySet<number>, right: ReadonlySet<number>): boolean {
  return left.size === right.size && [...left].every((id) => right.has(id));
}
