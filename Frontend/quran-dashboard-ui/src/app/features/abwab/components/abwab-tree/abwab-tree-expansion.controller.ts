import { signal } from '@angular/core';

import { AbwabNode } from '../../models/abwab.models';

export class AbwabTreeExpansionController {
  private readonly manualExpandedIdsSignal = signal<ReadonlySet<number>>(new Set());
  private readonly transientExpandedIdsSignal = signal<ReadonlySet<number>>(new Set());

  effectiveIds(searchExpandedIds: ReadonlySet<number>): ReadonlySet<number> {
    const manualIds = this.manualExpandedIdsSignal();
    const transientIds = this.transientExpandedIdsSignal();
    return transientIds.size === 0 && searchExpandedIds.size === 0
      ? manualIds
      : new Set([...manualIds, ...transientIds, ...searchExpandedIds]);
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
    if (!expanded) {
      this.removeTransientIds([id]);
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
    const expandedIds = this.baseExpandedIds();
    return collectIds(roots, true).some((id) => !expandedIds.has(id));
  }

  canCollapseAll(roots: readonly AbwabNode[]): boolean {
    return this.hasExpandedId(collectIds(roots, true));
  }

  canExpandBranch(node: AbwabNode): boolean {
    const expandedIds = this.baseExpandedIds();
    return collectIds([node], true).some((id) => !expandedIds.has(id));
  }

  canCollapseBranch(node: AbwabNode): boolean {
    return this.hasExpandedId(collectIds([node], true));
  }

  private addIds(ids: readonly number[]): ReadonlySet<number> {
    const next = new Set(this.manualExpandedIdsSignal());
    ids.forEach((id) => next.add(id));
    this.manualExpandedIdsSignal.set(next);
    return next;
  }

  private removeIds(ids: readonly number[]): ReadonlySet<number> {
    const next = new Set(this.manualExpandedIdsSignal());
    ids.forEach((id) => next.delete(id));
    this.manualExpandedIdsSignal.set(next);
    this.removeTransientIds(ids);
    return next;
  }

  private hasExpandedId(ids: readonly number[]): boolean {
    const expandedIds = this.baseExpandedIds();
    return ids.some((id) => expandedIds.has(id));
  }

  private baseExpandedIds(): ReadonlySet<number> {
    const manualIds = this.manualExpandedIdsSignal();
    const transientIds = this.transientExpandedIdsSignal();
    return transientIds.size === 0 ? manualIds : new Set([...manualIds, ...transientIds]);
  }

  private removeTransientIds(ids: readonly number[]): void {
    const next = new Set(this.transientExpandedIdsSignal());
    ids.forEach((id) => next.delete(id));
    this.transientExpandedIdsSignal.set(next);
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
