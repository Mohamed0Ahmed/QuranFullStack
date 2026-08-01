import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

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
    parentId: null,
    orderValue: id,
    globalOrderValue: id,
    version: 1,
    isArchived: false,
    depth: 0,
    liveChildCount: children.length,
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

interface RenderOptions {
  readonly relations?: readonly AbwabRelationVm[];
  readonly loadResult?: AbwabRelationsLoadResult;
  readonly anchorPickMode?: boolean;
  readonly bulkTargets?: readonly AbwabRelationTarget[];
  readonly addOutcome?: AbwabWriteOutcome<never>;
  readonly liveRoots?: readonly AbwabNode[];
}

function render(options: RenderOptions = {}) {
  getTestBed().resetTestingModule();
  const loadResult: AbwabRelationsLoadResult =
    options.loadResult ?? { kind: 'success', relations: options.relations ?? [] };
  const loadRelations = vi.fn().mockReturnValue(of(loadResult));
  const addRelations = vi.fn().mockReturnValue(of(options.addOutcome ?? { kind: 'success', data: [] }));
  const deleteRelation = vi.fn().mockReturnValue(of({ kind: 'success', data: null }));

  TestBed.configureTestingModule({ imports: [AbwabRelationsModalComponent] });
  const fixture = TestBed.createComponent(AbwabRelationsModalComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('anchorDoorId', 1);
  fixture.componentRef.setInput('anchorDoorName', 'الباب المرساة');
  fixture.componentRef.setInput('anchorPickMode', options.anchorPickMode ?? false);
  fixture.componentRef.setInput('bulkTargets', options.bulkTargets ?? []);
  fixture.componentRef.setInput('liveRoots', options.liveRoots ?? ROOTS);
  fixture.componentRef.setInput('loadRelations', loadRelations);
  fixture.componentRef.setInput('addRelations', addRelations);
  fixture.componentRef.setInput('deleteRelation', deleteRelation);
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

  return { fixture, root, el, click, search, pickedRows, loadRelations, addRelations, deleteRelation };
}

describe('AbwabRelationsModalComponent', () => {
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

    // Arrow keys move a radio group's selection and fire `change` without the click the row
    // listens to; without that path wired the control would tick while the anchor stayed put.
    it('follows keyboard radio selection, not just clicks', () => {
      const { fixture, el, pickedRows } = render({ anchorPickMode: true, bulkTargets });

      const target = el('abwab-relations-modal-pick-checkbox-4') as HTMLInputElement;
      target.checked = true;
      target.dispatchEvent(new Event('change', { bubbles: true }));
      fixture.detectChanges();

      expect(pickedRows()).toHaveLength(1);
      expect((el('abwab-relations-modal-pick-checkbox-4') as HTMLInputElement).checked).toBe(true);
      expect(el('abwab-relations-modal-add')?.textContent?.trim()).toBe(ABWAB_LABELS.relationAddButton(2));
    });

    it('opens with focus on the picker search in this mode too', async () => {
      const { fixture, el } = render({ anchorPickMode: true, bulkTargets });
      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(document.activeElement).toBe(el('abwab-relations-modal-search'));
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
});
