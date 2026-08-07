import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { Observable, Subject, of } from 'rxjs';

import { AbwabRelationsModalComponent, AbwabRelationTarget } from './abwab-relations-modal.component';
import { AbwabNode, AbwabRelationVm } from '../../models/abwab.models';
import { AbwabRelationsLoadResult } from '../../state/abwab-relations.controller';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';

function node(id: number, name: string, children: readonly AbwabNode[] = []): AbwabNode {
  return {
    id,
    name,
    description: null,
    representativeAyahText: null,
    aliases: [],
    sectionId: 1,
    sectionRetired: false,
    parentId: null,
    orderValue: id,
    globalOrderValue: id,
    version: 1,
    isArchived: false,
    depth: 0,
    liveChildCount: children.length,
    liveDescendantCount: children.length,
    maxRelativeDepth: children.length > 0 ? 1 : 0,
    relationCount: 0,
    children,
  };
}

const ROOTS: readonly AbwabNode[] = [
  node(1, 'الباب المرساة'),
  node(2, 'الصبر'),
  node(3, 'الشكر', [node(4, 'حمد الله')]),
];

function relation(
  id: number,
  otherDoorId: number,
  otherDoorName: string,
  kind: AbwabRelationVm['kind'],
  direction: AbwabRelationVm['direction'] = null,
): AbwabRelationVm {
  return { id, otherDoorId, otherDoorName, kind, direction };
}

/** The modal's own trap, not the nested confirm's — `query` returns the first match in DOM
 * order and the confirm dialog renders after the modal section. */
function trapOf(fixture: { debugElement: DebugElement }): CdkTrapFocus {
  return fixture.debugElement.query(By.directive(CdkTrapFocus)).injector.get(CdkTrapFocus);
}

interface RenderOptions {
  readonly relations?: readonly AbwabRelationVm[];
  readonly loadResult?: AbwabRelationsLoadResult;
  /** Defaults to the fixture list's own length. Pass it only to state a disagreement between the
   * snapshot's count and what the server returns — that disagreement IS the case under test. */
  readonly anchorRelationCount?: number;
  readonly anchorPickMode?: boolean;
  readonly bulkTargets?: readonly AbwabRelationTarget[];
  readonly addOutcome?: AbwabWriteOutcome<never>;
  readonly liveRoots?: readonly AbwabNode[];
  /** Replaces the synchronous `of(...)` so the in-flight window is observable at all. */
  readonly loadStream?: Observable<AbwabRelationsLoadResult>;
  readonly deleteOutcome?: AbwabWriteOutcome<never>;
  /** Same purpose as `loadStream`, for the delete: the confirm's busy window only exists while
   * the write is unresolved. */
  readonly deleteStream?: Observable<AbwabWriteOutcome<unknown>>;
}

function render(options: RenderOptions = {}) {
  getTestBed().resetTestingModule();
  const loadResult: AbwabRelationsLoadResult =
    options.loadResult ?? { kind: 'success', relations: options.relations ?? [] };
  const loadRelations = vi.fn().mockReturnValue(options.loadStream ?? of(loadResult));
  const refetchRelations = vi.fn().mockReturnValue(of(loadResult));
  const addRelations = vi.fn().mockReturnValue(of(options.addOutcome ?? { kind: 'success', data: [] }));
  const deleteRelation = vi
    .fn()
    .mockReturnValue(options.deleteStream ?? of(options.deleteOutcome ?? { kind: 'success', data: null }));

  TestBed.configureTestingModule({ imports: [AbwabRelationsModalComponent] });
  const fixture = TestBed.createComponent(AbwabRelationsModalComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('anchorDoorId', 1);
  fixture.componentRef.setInput('anchorDoorName', 'الباب المرساة');
  fixture.componentRef.setInput(
    'anchorRelationCount',
    options.anchorRelationCount ?? (options.relations?.length ?? 0),
  );
  fixture.componentRef.setInput('anchorPickMode', options.anchorPickMode ?? false);
  fixture.componentRef.setInput('bulkTargets', options.bulkTargets ?? []);
  fixture.componentRef.setInput('liveRoots', options.liveRoots ?? ROOTS);
  fixture.componentRef.setInput('loadRelations', loadRelations);
  fixture.componentRef.setInput('refetchRelations', refetchRelations);
  fixture.componentRef.setInput('addRelations', addRelations);
  fixture.componentRef.setInput('deleteRelation', deleteRelation);
  fixture.componentRef.setInput('canCreateRelation', true);
  fixture.componentRef.setInput('canDeleteRelation', true);
  fixture.detectChanges();

  const root = fixture.nativeElement as HTMLElement;
  const el = (testId: string) => root.querySelector<HTMLElement>(`[data-testid="${testId}"]`);
  const click = (testId: string) => {
    el(testId)!.click();
    fixture.detectChanges();
  };
  const search = (query: string) => {
    const input = el('abwab-relations-modal-search') as HTMLInputElement;
    input.value = query;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  const pickedRows = () =>
    Array.from(root.querySelectorAll<HTMLElement>('[data-testid^="abwab-relations-modal-pick-checkbox-"]')).filter(
      (checkbox) => (checkbox as HTMLInputElement).checked,
    );

  return { fixture, root, el, click, search, pickedRows, loadRelations, refetchRelations, addRelations, deleteRelation };
}

describe('AbwabRelationsModalComponent', () => {
  it('keeps relation reading and reveal available while hiding add and delete for a view-only visitor', () => {
    const { fixture, root, el, addRelations, deleteRelation } = render({
      relations: [relation(10, 2, 'الصبر', 'similarity')],
    });
    fixture.componentRef.setInput('canCreateRelation', false);
    fixture.componentRef.setInput('canDeleteRelation', false);
    fixture.detectChanges();
    const revealed: number[] = [];
    fixture.componentInstance.revealRequested.subscribe((id) => revealed.push(id));

    expect(root.querySelector('[data-testid="abwab-relations-modal-add"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-relations-modal-type-similarity"]')).toBeNull();
    expect(root.querySelector('[data-testid="qd-chip-remove"]')).toBeNull();
    expect(root.textContent).toContain('الصبر');

    const chip = root.querySelector<HTMLElement>('[data-testid="qd-chip"]')!;
    chip.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    const internals = fixture.componentInstance as unknown as {
      add(): void;
      remove(relation: AbwabRelationVm): void;
      confirmRemove(): void;
    };
    internals.add();
    internals.remove(relation(10, 2, 'الصبر', 'similarity'));
    internals.confirmRemove();

    expect(revealed).toEqual([2]);
    expect(addRelations).not.toHaveBeenCalled();
    expect(deleteRelation).not.toHaveBeenCalled();
  });

  describe('the four display groups', () => {
    it('renders one group per non-empty display key, ordered, with the global count', () => {
      const { root, el } = render({
        relations: [
          relation(10, 2, 'الصبر', 'similarity'),
          relation(11, 3, 'الشكر', 'opposition'),
          // The anchor being the more comprehensive side puts the OTHER door under «أقل شمولية».
          relation(12, 4, 'حمد الله', 'comprehensiveness', 'anchor-more'),
          relation(13, 5, 'الذكر', 'comprehensiveness', 'anchor-less'),
        ],
      });

      const groups = Array.from(root.querySelectorAll('[data-testid^="abwab-relations-modal-group-"]'));
      expect(groups.map((group) => group.getAttribute('data-testid'))).toEqual([
        'abwab-relations-modal-group-similarity',
        'abwab-relations-modal-group-opposition',
        'abwab-relations-modal-group-more-comprehensive',
        'abwab-relations-modal-group-less-comprehensive',
      ]);
      expect(el('abwab-relations-modal-group-less-comprehensive')?.textContent).toContain('حمد الله');
      expect(el('abwab-relations-modal-group-more-comprehensive')?.textContent).toContain('الذكر');
      expect(el('abwab-relations-modal-count')?.textContent?.trim()).toBe('4');
    });

    it('omits empty groups and shows the empty state when the door has no relations', () => {
      const { root, el } = render({ relations: [relation(10, 2, 'الصبر', 'similarity')] });

      expect(root.querySelectorAll('[data-testid^="abwab-relations-modal-group-"]')).toHaveLength(1);

      const { root: emptyRoot, el: emptyEl } = render({ relations: [] });
      expect(emptyRoot.querySelectorAll('[data-testid^="abwab-relations-modal-group-"]')).toHaveLength(0);
      expect(emptyEl('abwab-relations-modal-empty')?.textContent).toContain(ABWAB_LABELS.relationsEmpty);
    });
  });

  // The snapshot's own relation count decides whether the modal asks the server at all, so every
  // cell below is "what is on screen" AND "was a request issued" — the second half is the point.
  describe('the count-discriminated read', () => {
    it('answers a zero-count door from the snapshot, without asking the server', () => {
      const { el, loadRelations } = render({ anchorRelationCount: 0 });

      expect(loadRelations).not.toHaveBeenCalled();
      expect(el('abwab-relations-modal-empty')?.textContent).toContain(ABWAB_LABELS.relationsEmpty);
      expect(el('abwab-relations-modal-loading')).toBeNull();
    });

    it('shows skeleton rows while a count>0 door loads, and neither the empty text nor a 0 chip', () => {
      const pending = new Subject<AbwabRelationsLoadResult>();
      const { fixture, el, loadRelations } = render({ anchorRelationCount: 2, loadStream: pending });

      expect(loadRelations).toHaveBeenCalledWith(1);
      expect(el('abwab-relations-modal-loading')).toBeTruthy();
      // The two claims the old always-`[]`-then-fetch modal made before it had been told anything.
      expect(el('abwab-relations-modal-empty')).toBeNull();
      expect(el('abwab-relations-modal-count')).toBeNull();

      pending.next({ kind: 'success', relations: [relation(10, 2, 'الصبر', 'similarity')] });
      fixture.detectChanges();

      expect(el('abwab-relations-modal-loading')).toBeNull();
      expect(el('abwab-relations-modal-group-similarity')?.textContent).toContain('الصبر');
      expect(el('abwab-relations-modal-count')?.textContent?.trim()).toBe('1');
    });

    // The count says whether to ask; the answer says what is true. A door whose count is stale by
    // one still shows what the server actually returned.
    it('lets the fetched list overrule a count that disagrees with it', () => {
      const { el } = render({ anchorRelationCount: 3, relations: [] });

      expect(el('abwab-relations-modal-empty')?.textContent).toContain(ABWAB_LABELS.relationsEmpty);
      expect(el('abwab-relations-modal-count')?.textContent?.trim()).toBe('0');
    });

    it('re-runs the whole discriminator when the anchor changes under an open modal', () => {
      const { fixture, el, loadRelations } = render({
        anchorRelationCount: 1,
        relations: [relation(10, 2, 'الصبر', 'similarity')],
      });
      expect(el('abwab-relations-modal-group-similarity')).toBeTruthy();

      fixture.componentRef.setInput('anchorDoorId', 3);
      fixture.componentRef.setInput('anchorRelationCount', 0);
      fixture.detectChanges();

      // The previous anchor's list must not survive as an answer about the new one.
      expect(el('abwab-relations-modal-group-similarity')).toBeNull();
      expect(el('abwab-relations-modal-empty')).toBeTruthy();
      expect(loadRelations).toHaveBeenCalledTimes(1);

      fixture.componentRef.setInput('anchorDoorId', 2);
      fixture.componentRef.setInput('anchorRelationCount', 1);
      fixture.detectChanges();

      expect(loadRelations).toHaveBeenCalledTimes(2);
      expect(loadRelations).toHaveBeenLastCalledWith(2);
    });

    // The count is snapshot-derived and the snapshot is refetched after every write, so a
    // count-tracking effect would reset the user's half-built draft under them.
    it('does not restart the draft when only the count moves', () => {
      const { fixture, el, click, loadRelations } = render({ anchorRelationCount: 0 });

      click('abwab-relations-modal-pick-2');
      fixture.componentRef.setInput('anchorRelationCount', 4);
      fixture.detectChanges();

      expect((el('abwab-relations-modal-pick-checkbox-2') as HTMLInputElement).checked).toBe(true);
      expect(loadRelations).not.toHaveBeenCalled();
    });

    // What a cache hit is, from this component's side: an answer already in hand. The skeleton
    // branch must not flicker through on the way to rendering it.
    it('paints no skeleton when the read answers without waiting', () => {
      const { root, el } = render({ anchorRelationCount: 1, relations: [relation(10, 2, 'الصبر', 'similarity')] });

      expect(root.querySelector('[data-testid="qd-skeleton-rows"]')).toBeNull();
      expect(el('abwab-relations-modal-loading')).toBeNull();
      expect(el('abwab-relations-modal-group-similarity')).toBeTruthy();
    });

    // The cached list is the one thing that cannot answer for a door the user just wrote to, and
    // the snapshot refresh that would evict it has not landed when this runs.
    it('takes the uncached read after a write, never the cache-aware one', () => {
      const { fixture, root, click, loadRelations, refetchRelations } = render({
        anchorRelationCount: 1,
        relations: [relation(10, 2, 'الصبر', 'similarity')],
      });
      expect(loadRelations).toHaveBeenCalledTimes(1);

      click('abwab-relations-modal-pick-3');
      click('abwab-relations-modal-add');

      expect(refetchRelations).toHaveBeenCalledWith(1);
      expect(loadRelations).toHaveBeenCalledTimes(1);

      // Two gestures since slice L — the chip opens the confirm and the confirm dispatches — but
      // the contract under test is unchanged: the delete's refresh takes the uncached read.
      (root.querySelector('[data-testid="qd-chip-remove"]') as HTMLElement).click();
      fixture.detectChanges();
      click('abwab-relations-delete-confirm-confirm');
      expect(refetchRelations).toHaveBeenCalledTimes(2);
      expect(loadRelations).toHaveBeenCalledTimes(1);
    });

    it('reads no count and issues no fetch in anchor-pick mode', () => {
      const { el, loadRelations } = render({
        anchorPickMode: true,
        anchorRelationCount: 5,
        bulkTargets: [{ id: 2, name: 'الصبر' }],
      });

      expect(loadRelations).not.toHaveBeenCalled();
      expect(el('abwab-relations-modal-loading')).toBeNull();
      expect(el('abwab-relations-modal-empty')).toBeNull();
      expect(el('abwab-relations-modal-targets')).toBeTruthy();
    });
  });

  describe('a failed read is recoverable, a failed write is not sticky', () => {
    it('offers one retry, goes back to the skeleton, and clears the error once it succeeds', () => {
      const pending = new Subject<AbwabRelationsLoadResult>();
      const { fixture, root, el, loadRelations } = render({
        anchorRelationCount: 1,
        loadStream: pending,
      });

      pending.next({ kind: 'error', message: ABWAB_LABELS.relationsLoadError });
      fixture.detectChanges();

      expect(el('abwab-relations-modal-error')?.textContent).toContain(ABWAB_LABELS.relationsLoadError);
      expect(el('abwab-relations-modal-loading')).toBeNull();
      // A failed read has no answer to give, so it must not fall through to «لا توجد علاقات».
      expect(el('abwab-relations-modal-empty')).toBeNull();

      const retry = root.querySelector('[data-testid="qd-state-action"]') as HTMLButtonElement;
      expect(retry.textContent?.trim()).toBe(ABWAB_LABELS.retryButton);

      retry.click();
      fixture.detectChanges();

      expect(loadRelations).toHaveBeenCalledTimes(2);
      expect(el('abwab-relations-modal-loading')).toBeTruthy();
      expect(el('abwab-relations-modal-error')).toBeNull();

      pending.next({ kind: 'success', relations: [relation(10, 2, 'الصبر', 'similarity')] });
      fixture.detectChanges();

      expect(el('abwab-relations-modal-error')).toBeNull();
      expect(el('abwab-relations-modal-group-similarity')).toBeTruthy();
    });

    // §17 permits exactly one action, and the add button already is the retry for its own failure.
    it('gives a write failure its message without a second retry control', () => {
      const { root, el, click } = render({
        anchorRelationCount: 0,
        addOutcome: { kind: 'conflict', message: 'العلاقة موجودة بالفعل' },
      });

      click('abwab-relations-modal-pick-2');
      click('abwab-relations-modal-add');

      expect(el('abwab-relations-modal-error')?.textContent).toContain('العلاقة موجودة بالفعل');
      expect(root.querySelector('[data-testid="qd-state-action"]')).toBeNull();
      // The list is still valid and still on screen — a write error is not a read failure.
      expect(el('abwab-relations-modal-empty')).toBeTruthy();
    });

    it('clears a stuck read error once a later load succeeds', () => {
      const stream = new Subject<AbwabRelationsLoadResult>();
      const { fixture, el } = render({ anchorRelationCount: 1, loadStream: stream });

      stream.next({ kind: 'error', message: ABWAB_LABELS.relationsLoadError });
      fixture.detectChanges();
      expect(el('abwab-relations-modal-error')).toBeTruthy();

      // Not the retry button: any successful load clears it, including the post-write refresh.
      stream.next({ kind: 'success', relations: [] });
      fixture.detectChanges();

      expect(el('abwab-relations-modal-error')).toBeNull();
    });
  });

  it('clears the picked doors when the type segment changes', () => {
    const { click, pickedRows } = render();

    click('abwab-relations-modal-pick-2');
    expect(pickedRows()).toHaveLength(1);

    click('abwab-relations-modal-type-opposition');
    expect(pickedRows()).toHaveLength(0);
  });

  describe('already-linked doors, per (pair, type)', () => {
    it('disables and tags a door linked under the active type, and re-enables it under another', () => {
      const { el, click } = render({ relations: [relation(10, 2, 'الصبر', 'similarity')] });

      const linkedRow = el('abwab-relations-modal-pick-2')!;
      expect((el('abwab-relations-modal-pick-checkbox-2') as HTMLInputElement).disabled).toBe(true);
      expect(linkedRow.textContent).toContain(ABWAB_LABELS.relationAlreadyLinked);

      linkedRow.click();
      expect((el('abwab-relations-modal-pick-checkbox-2') as HTMLInputElement).checked).toBe(false);

      click('abwab-relations-modal-type-opposition');
      expect((el('abwab-relations-modal-pick-checkbox-2') as HTMLInputElement).disabled).toBe(false);
      expect(el('abwab-relations-modal-pick-2')!.textContent).not.toContain(ABWAB_LABELS.relationAlreadyLinked);
    });
  });

  describe('anchor-pick mode', () => {
    const bulkTargets: readonly AbwabRelationTarget[] = [
      { id: 2, name: 'الصبر' },
      { id: 3, name: 'الشكر' },
    ];

    it('single-selects the anchor and counts the fixed targets on the add button', () => {
      const { el, click, pickedRows } = render({ anchorPickMode: true, bulkTargets });

      expect(el('abwab-relations-modal-add')?.textContent?.trim()).toBe(ABWAB_LABELS.relationAddButton(0));

      click('abwab-relations-modal-pick-1');
      click('abwab-relations-modal-pick-4');

      expect(pickedRows()).toHaveLength(1);
      expect((el('abwab-relations-modal-pick-checkbox-4') as HTMLInputElement).checked).toBe(true);
      // Two relations are created by the one call, so the button counts the targets, not the pick.
      expect(el('abwab-relations-modal-add')?.textContent?.trim()).toBe(ABWAB_LABELS.relationAddButton(2));
    });

    // Behavior alone is not the whole contract: a checkbox promises "pick any number", and this
    // mode accepts exactly one. The control and the placeholder both have to say so, or the only
    // way to learn the rule is to pick a second door and watch the first vanish.
    it('renders the choice as a radio group and says "one door" in the search placeholder', () => {
      const { el } = render({ anchorPickMode: true, bulkTargets });

      const anchorBox = el('abwab-relations-modal-pick-checkbox-1') as HTMLInputElement;
      expect(anchorBox.type).toBe('radio');
      // One group, and a group name that cannot be shared with another picker on the page: radio
      // grouping is document-scoped by name, and emulated encapsulation does not scope it.
      expect(anchorBox.name).toBe(el('abwab-relations-modal-pick-checkbox-4')!.getAttribute('name'));
      expect(anchorBox.name).not.toBe('abwab-relations-modal-pick');
      expect(anchorBox.name).toMatch(/^abwab-door-picker-\d+$/);
      expect(el('abwab-relations-modal-search')?.getAttribute('placeholder')).toBe(
        ABWAB_LABELS.relationsBulkAnchorPlaceholder,
      );
    });

    it('leaves door mode on checkboxes, where several targets really are pickable', () => {
      const { el } = render();

      expect((el('abwab-relations-modal-pick-checkbox-2') as HTMLInputElement).type).toBe('checkbox');
      expect(el('abwab-relations-modal-search')?.getAttribute('placeholder')).toBe(
        ABWAB_LABELS.relationPickerPlaceholder,
      );
    });

    // Arrow keys never fire a bare `change` here: engines dispatch arrow-key radio selection as a
    // simulated click (cancelable, bubbling), the input's own (click) handler cancels the default
    // activation, and the bubbled click lands on the row's togglePicked — the same path a mouse
    // click takes. F-95: the (change) handler that once shadowed this path was dead code.
    it('keyboard radio selection works through the row click path (arrow keys synthesize clicks)', () => {
      const { fixture, el, pickedRows } = render({ anchorPickMode: true, bulkTargets });

      const target = el('abwab-relations-modal-pick-checkbox-4') as HTMLInputElement;
      const synthesizedClick = new MouseEvent('click', { bubbles: true, cancelable: true });
      target.dispatchEvent(synthesizedClick);
      fixture.detectChanges();

      expect(synthesizedClick.defaultPrevented).toBe(true);
      expect(pickedRows()).toHaveLength(1);
      expect((el('abwab-relations-modal-pick-checkbox-4') as HTMLInputElement).checked).toBe(true);
      expect(el('abwab-relations-modal-add')?.textContent?.trim()).toBe(ABWAB_LABELS.relationAddButton(2));
    });

    it('opens with focus on the picker search in this mode too', async () => {
      const { fixture, el } = render({ anchorPickMode: true, bulkTargets });
      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(document.activeElement).toBe(el('abwab-relations-modal-search'));
      // The queued focus above is only half the contract: the trap's own auto-capture must aim at
      // the same input, or the modal opens on the first type tab and is then corrected.
      expect(el('abwab-relations-modal-search')!.hasAttribute('cdkFocusInitial')).toBe(true);
    });

    it('never offers a fixed target as the anchor, keeps its subtree, and adds anchor-first', () => {
      const { el, click, addRelations } = render({ anchorPickMode: true, bulkTargets });

      expect(el('abwab-relations-modal-pick-2')).toBeNull();
      expect(el('abwab-relations-modal-pick-3')).toBeNull();
      // A door may relate to its own ancestor, so an excluded parent hides only itself.
      expect(el('abwab-relations-modal-pick-4')).toBeTruthy();

      click('abwab-relations-modal-pick-1');
      click('abwab-relations-modal-add');

      expect(addRelations).toHaveBeenCalledWith(1, 'similarity', null, [2, 3]);
    });
  });

  describe('the direction pill names the side the picker chooses', () => {
    it('names the targets in door mode and the anchor in anchor-pick mode', () => {
      const { el, click } = render();
      click('abwab-relations-modal-type-comprehensiveness');

      expect(el('abwab-relations-modal-direction-anchor-more')?.textContent?.trim()).toBe(
        ABWAB_LABELS.relationDirectionAnchorMore,
      );
      expect(el('abwab-relations-modal-direction-anchor-less')?.textContent?.trim()).toBe(
        ABWAB_LABELS.relationDirectionAnchorLess,
      );

      const bulk = render({ anchorPickMode: true, bulkTargets: [{ id: 5, name: 'الذكر' }] });
      bulk.click('abwab-relations-modal-type-comprehensiveness');

      expect(bulk.el('abwab-relations-modal-direction-anchor-more')?.textContent?.trim()).toBe(
        ABWAB_LABELS.relationsBulkDirectionAnchorMore,
      );
      expect(bulk.el('abwab-relations-modal-direction-anchor-less')?.textContent?.trim()).toBe(
        ABWAB_LABELS.relationsBulkDirectionAnchorLess,
      );
    });

    it('hides the direction row for the types that carry no direction', () => {
      const { el, click } = render();
      expect(el('abwab-relations-modal-direction')).toBeNull();

      click('abwab-relations-modal-type-comprehensiveness');
      expect(el('abwab-relations-modal-direction')).toBeTruthy();
    });
  });

  describe('picker search', () => {
    it('keeps a parent whose descendant matches and expands it to reveal the match', () => {
      const { el, search } = render();

      expect(el('abwab-relations-modal-pick-4')).toBeNull();

      search('حمد');

      expect(el('abwab-relations-modal-pick-3')).toBeTruthy();
      expect(el('abwab-relations-modal-pick-4')).toBeTruthy();
      expect(el('abwab-relations-modal-pick-2')).toBeNull();
    });

    // "Your query matched nothing" and "there is nothing to pick" are different answers, and the
    // second one is false here — the doors are on screen the moment the query is cleared.
    it('says the search matched nothing rather than claiming the tree is empty', () => {
      const { el, search } = render();

      search('لا وجود لهذا الباب');

      expect(el('abwab-relations-modal-no-matches')?.textContent).toContain(ABWAB_LABELS.pickerNoMatches);
      expect(el('abwab-relations-modal-doors-empty')).toBeNull();

      search('');
      expect(el('abwab-relations-modal-no-matches')).toBeNull();
      expect(el('abwab-relations-modal-pick-2')).toBeTruthy();
    });

    // The mirror case: with no doors at all, a typed query does not make "no matches" the honest
    // answer — there is genuinely nothing to pick, and that is the host's sentence to say.
    it('still answers an empty tree with the host wording, query typed or not', () => {
      const { el, search } = render({ liveRoots: [] });

      expect(el('abwab-relations-modal-doors-empty')?.textContent).toContain(ABWAB_LABELS.relationPickerEmptyDoors);
      // Distinct from the modal's own «لا توجد علاقات» — one prefix now serves the host and the
      // picker, so the two empty states must not answer to the same testid.
      expect(el('abwab-relations-modal-empty')?.textContent).toContain(ABWAB_LABELS.relationsEmpty);

      search('أي شيء');
      expect(el('abwab-relations-modal-no-matches')).toBeNull();
      expect(el('abwab-relations-modal-doors-empty')?.textContent).toContain(ABWAB_LABELS.relationPickerEmptyDoors);
    });
  });

  describe('closing', () => {
    it('emits closed on Escape and on a backdrop click', () => {
      const { fixture, el } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      el('abwab-relations-modal')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      el('abwab-relations-modal-backdrop')!.click();

      expect(closed).toHaveLength(2);
    });

    it('opens with focus on the picker search, inside the trapped dialog', async () => {
      const { fixture, root, el } = render();
      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(document.activeElement).toBe(el('abwab-relations-modal-search'));
      expect(root.querySelector('[data-testid="abwab-relations-modal"]')!.contains(document.activeElement)).toBe(true);
    });

    it('keeps the actions out of the scrolling body', () => {
      const { root } = render();

      const foot = root.querySelector('.qd-modal__foot')!;
      expect(foot.querySelector('[data-testid="abwab-relations-modal-add"]')).toBeTruthy();
      expect(root.querySelector('.qd-modal__body')!.contains(foot)).toBe(false);
    });

    it('clicking inside the dialog does not close it', () => {
      const { fixture, el } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      el('abwab-relations-modal')!.click();

      expect(closed).toHaveLength(0);
    });
  });

  it('resets the picks and the error when it is reopened', () => {
    const { fixture, el, click } = render({
      addOutcome: { kind: 'conflict', message: 'العلاقة موجودة بالفعل' },
    });

    click('abwab-relations-modal-pick-2');
    click('abwab-relations-modal-add');
    expect(el('abwab-relations-modal-error')?.textContent).toContain('العلاقة موجودة بالفعل');

    fixture.componentRef.setInput('open', false);
    fixture.detectChanges();
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();

    expect(el('abwab-relations-modal-error')).toBeNull();
    expect(el('abwab-relations-modal-selected')?.textContent?.trim()).toBe(ABWAB_LABELS.relationNoneSelected);
  });
  describe('audit item 10 — the relation name is a second control on the chip', () => {
    it('emits revealRequested with the other door’s id, and names the control for a screen reader', () => {
      const { fixture, root } = render({ relations: [relation(10, 42, 'الصبر', 'similarity')] });
      const revealed: number[] = [];
      fixture.componentInstance.revealRequested.subscribe((id: number) => revealed.push(id));

      const label = root.querySelector('[data-testid="qd-chip-label"]') as HTMLButtonElement;
      expect(label.getAttribute('aria-label')).toBe(ABWAB_LABELS.relationRevealAriaLabel('الصبر'));
      expect(label.textContent?.trim()).toBe('الصبر');

      label.click();
      fixture.detectChanges();

      // The id carried is the OTHER door's, not the relation row's — a reveal of relation 10
      // would be a reveal of nothing.
      expect(revealed).toEqual([42]);
    });

    // Slice L (L2) rewrote the second half of this case: the remove control no longer deletes, it
    // opens a confirm. What is still worth pinning here is that the two chip controls stay
    // independent — the delete-confirm behaviour itself is the describe below.
    it('keeps remove independent: removing does not reveal, revealing does not remove', () => {
      const { fixture, root, el, deleteRelation } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
      });
      const revealed: number[] = [];
      fixture.componentInstance.revealRequested.subscribe((id: number) => revealed.push(id));

      (root.querySelector('[data-testid="qd-chip-label"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(revealed).toEqual([42]);
      expect(el('abwab-relations-delete-confirm')).toBeNull();
      expect(deleteRelation).not.toHaveBeenCalled();

      (root.querySelector('[data-testid="qd-chip-remove"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(el('abwab-relations-delete-confirm')).not.toBeNull();
      expect(revealed).toEqual([42]);
    });
  });

  // Slice L (L2). This block replaces the old "remove deletes immediately" pin: a relation delete
  // is two-sided and irreversible from this modal, so it now goes through `qd-confirm-dialog`.
  describe('the delete confirm', () => {
    const removeChip = (root: HTMLElement, fixture: { detectChanges: () => void }) => {
      (root.querySelector('[data-testid="qd-chip-remove"]') as HTMLElement).click();
      fixture.detectChanges();
    };

    it('opens the confirm instead of dispatching, and dispatches only once confirmed', () => {
      const { fixture, root, el, click, deleteRelation } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
      });

      removeChip(root, fixture);
      expect(deleteRelation).not.toHaveBeenCalled();
      expect(el('abwab-relations-delete-confirm')).not.toBeNull();

      click('abwab-relations-delete-confirm-confirm');
      expect(deleteRelation).toHaveBeenCalledWith(10);
      expect(el('abwab-relations-delete-confirm')).toBeNull();
    });

    it('names both doors and the group in the body, and states the two-sided consequence', () => {
      const { fixture, root, el } = render({
        // anchor-more on the anchor puts the OTHER door in the «أقل شمولية» group, so the body
        // must read "الصبر is LESS comprehensive than الباب المرساة".
        relations: [relation(10, 42, 'الصبر', 'comprehensiveness', 'anchor-more')],
      });

      removeChip(root, fixture);

      const body = el('abwab-relations-delete-confirm')?.textContent ?? '';
      expect(body).toContain(
        ABWAB_LABELS.relationDeleteConfirmBody('الباب المرساة', 'الصبر', 'less-comprehensive'),
      );
      expect(body).toContain(ABWAB_LABELS.relationDeleteConfirmSides);
    });

    // F-62, copying the sections modal's pin (abwab-sections-modal.component.spec.ts): the one
    // permitted nesting is a confirm above one authoring modal, and the host yields its trap while
    // that confirm is open — two live traps fight over focus.
    it('yields its focus trap while the confirm is open and takes it back on cancel', () => {
      const { fixture, root, click } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
      });

      expect(trapOf(fixture).enabled).toBe(true);

      removeChip(root, fixture);
      expect(trapOf(fixture).enabled).toBe(false);

      click('abwab-relations-delete-confirm-cancel');
      expect(trapOf(fixture).enabled).toBe(true);
    });

    it('cancels without dispatching and leaves the list untouched', () => {
      const { fixture, root, el, click, deleteRelation } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
      });

      removeChip(root, fixture);
      click('abwab-relations-delete-confirm-cancel');

      expect(deleteRelation).not.toHaveBeenCalled();
      expect(el('abwab-relations-delete-confirm')).toBeNull();
      expect(root.querySelectorAll('[data-testid="qd-chip-remove"]').length).toBe(1);
    });

    it('holds the dialog open while the write is out and refuses a second confirm', () => {
      const pending = new Subject<AbwabWriteOutcome<unknown>>();
      const { fixture, root, el, click, deleteRelation } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
        deleteStream: pending,
      });

      removeChip(root, fixture);
      click('abwab-relations-delete-confirm-confirm');

      expect(el('abwab-relations-delete-confirm')).not.toBeNull();
      const confirmButton = el('abwab-relations-delete-confirm-confirm') as HTMLButtonElement;
      expect(confirmButton.disabled).toBe(true);
      expect((el('abwab-relations-delete-confirm-cancel') as HTMLButtonElement).disabled).toBe(true);
      expect(confirmButton.getAttribute('aria-busy')).toBe('true');

      // The guard, not just the disabled attribute: a programmatic second confirm must be inert.
      click('abwab-relations-delete-confirm-confirm');
      expect(deleteRelation).toHaveBeenCalledTimes(1);

      pending.next({ kind: 'success', data: null });
      fixture.detectChanges();
      expect(el('abwab-relations-delete-confirm')).toBeNull();
    });

    it('renders a failed write inside the dialog and keeps it open', () => {
      const { fixture, root, el, click, refetchRelations } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
        deleteOutcome: { kind: 'error', message: 'تعذر حذف العلاقة.' },
      });

      removeChip(root, fixture);
      click('abwab-relations-delete-confirm-confirm');

      expect(el('abwab-relations-delete-confirm')).not.toBeNull();
      expect(el('abwab-relations-delete-confirm-error')?.textContent).toContain('تعذر حذف العلاقة.');
      // The modal's shared line belongs to the read and the add; a delete failure must not land
      // there, where its retry would offer to re-run the load.
      expect(el('abwab-relations-modal-error')).toBeNull();
      expect(refetchRelations).not.toHaveBeenCalled();
      expect((el('abwab-relations-delete-confirm-confirm') as HTMLButtonElement).disabled).toBe(false);
    });

    it('refetches the list after a successful delete', () => {
      const { fixture, root, click, refetchRelations } = render({
        relations: [relation(10, 42, 'الصبر', 'similarity')],
      });

      removeChip(root, fixture);
      click('abwab-relations-delete-confirm-confirm');

      expect(refetchRelations).toHaveBeenCalledWith(1);
    });
  });
});
