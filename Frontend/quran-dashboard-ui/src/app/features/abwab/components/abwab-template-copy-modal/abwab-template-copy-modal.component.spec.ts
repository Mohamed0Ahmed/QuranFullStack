import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AbwabTemplateCopyModalComponent } from './abwab-template-copy-modal.component';
import { AbwabNode } from '../../models/abwab.models';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
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

const ROOTS: readonly AbwabNode[] = [node(1, 'الصبر'), node(2, 'الشكر', [node(3, 'حمد الله')])];

interface RenderOptions {
  readonly liveRoots?: readonly AbwabNode[];
  readonly doorsLoading?: boolean;
  readonly doorsError?: string | null;
  readonly applyOutcome?: AbwabWriteOutcome<AbwabDoorDto[] | null>;
}

function render(options: RenderOptions = {}) {
  getTestBed().resetTestingModule();
  const applyTemplate = vi
    .fn()
    .mockReturnValue(of(options.applyOutcome ?? ({ kind: 'success', data: null } as AbwabWriteOutcome<AbwabDoorDto[] | null>)));

  TestBed.configureTestingModule({ imports: [AbwabTemplateCopyModalComponent] });
  const fixture = TestBed.createComponent(AbwabTemplateCopyModalComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('templateName', 'قالب الأخلاق');
  fixture.componentRef.setInput('templateNodeCount', 4);
  fixture.componentRef.setInput('liveRoots', options.liveRoots ?? ROOTS);
  fixture.componentRef.setInput('doorsLoading', options.doorsLoading ?? false);
  fixture.componentRef.setInput('doorsError', options.doorsError ?? null);
  fixture.componentRef.setInput('applyTemplate', applyTemplate);
  fixture.detectChanges();

  const root = fixture.nativeElement as HTMLElement;
  const el = (testId: string) => root.querySelector<HTMLElement>(`[data-testid="${testId}"]`);
  const click = (testId: string) => {
    el(testId)!.click();
    fixture.detectChanges();
  };
  const search = (query: string) => {
    const input = el('abwab-template-copy-modal-search') as HTMLInputElement;
    input.value = query;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  return { fixture, root, el, click, search, applyTemplate };
}

describe('AbwabTemplateCopyModalComponent', () => {
  describe('picker search', () => {
    it('keeps a parent whose descendant matches and expands it to reveal the match', () => {
      const { el, search } = render();

      expect(el('abwab-template-copy-modal-pick-3')).toBeNull();

      search('حمد');

      expect(el('abwab-template-copy-modal-pick-2')).toBeTruthy();
      expect(el('abwab-template-copy-modal-pick-3')).toBeTruthy();
      expect(el('abwab-template-copy-modal-pick-1')).toBeNull();
    });
  });

  describe('multi-select', () => {
    it('toggles rows independently and names each checkbox by its door', () => {
      const { el, click } = render();

      expect(el('abwab-template-copy-modal-pick-checkbox-1')?.getAttribute('aria-label')).toBe('الصبر');

      click('abwab-template-copy-modal-pick-1');
      click('abwab-template-copy-modal-pick-2');
      expect((el('abwab-template-copy-modal-pick-checkbox-1') as HTMLInputElement).checked).toBe(true);
      expect((el('abwab-template-copy-modal-pick-checkbox-2') as HTMLInputElement).checked).toBe(true);

      click('abwab-template-copy-modal-pick-1');
      expect((el('abwab-template-copy-modal-pick-checkbox-1') as HTMLInputElement).checked).toBe(false);
      expect((el('abwab-template-copy-modal-pick-checkbox-2') as HTMLInputElement).checked).toBe(true);
    });

    it('counts the targets, not their union, on the confirm button', () => {
      const { el, click, search, applyTemplate } = render();

      expect(el('abwab-template-copy-modal-confirm')?.textContent?.trim()).toBe(
        ABWAB_LABELS.templateCopyConfirmButton(0),
      );

      // A door and its own descendant are two independent copies, not one.
      search('حمد');
      click('abwab-template-copy-modal-pick-2');
      click('abwab-template-copy-modal-pick-3');

      expect(el('abwab-template-copy-modal-confirm')?.textContent?.trim()).toBe(
        ABWAB_LABELS.templateCopyConfirmButton(2),
      );

      click('abwab-template-copy-modal-confirm');
      expect(applyTemplate).toHaveBeenCalledWith([2, 3]);
    });
  });

  it('keeps the selection and shows the message when the apply conflicts', () => {
    const message = 'يوجد باب بنفس الاسم تحت الهدف';
    const { fixture, el, click } = render({ applyOutcome: { kind: 'conflict', message } });
    const closed: void[] = [];
    fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

    click('abwab-template-copy-modal-pick-1');
    click('abwab-template-copy-modal-confirm');

    expect(el('abwab-template-copy-modal-error')?.textContent).toContain(message);
    expect((el('abwab-template-copy-modal-pick-checkbox-1') as HTMLInputElement).checked).toBe(true);
    expect(closed).toHaveLength(0);
  });

  describe('the doors snapshot states are separated', () => {
    it('shows skeleton rows while loading, the error with a retry, and empty only when resolved', () => {
      const loading = render({ liveRoots: [], doorsLoading: true });
      expect(loading.el('abwab-template-copy-modal-loading')).toBeTruthy();
      expect(loading.el('abwab-template-copy-modal-doors-empty')).toBeNull();

      const failed = render({ liveRoots: [], doorsError: 'تعذر تحميل الأبواب' });
      expect(failed.el('abwab-template-copy-modal-doors-error')?.textContent).toContain('تعذر تحميل الأبواب');
      const retries: void[] = [];
      failed.fixture.componentInstance.retryDoors.subscribe(() => retries.push(undefined));
      (failed.el('abwab-template-copy-modal-doors-error')!.querySelector('button') as HTMLElement).click();
      expect(retries).toHaveLength(1);

      const empty = render({ liveRoots: [] });
      expect(empty.el('abwab-template-copy-modal-doors-empty')?.textContent).toContain(ABWAB_LABELS.templateCopyEmptyDoors);
    });

    // «لا توجد أبواب حية لنسخ القالب إليها» is a claim about the tree, and a search that matches
    // nothing is not evidence for it — the doors come back the moment the query is cleared.
    it('does not answer an unmatched search with "there are no live doors"', () => {
      const { el, search } = render();

      search('لا وجود لهذا الباب');

      expect(el('abwab-template-copy-modal-no-matches')?.textContent).toContain(ABWAB_LABELS.pickerNoMatches);
      expect(el('abwab-template-copy-modal-doors-empty')).toBeNull();
    });

    // And the mirror: a typed query over a genuinely empty tree is still «لا توجد أبواب حية».
    // Guarding on "a query is typed" alone would trade one wrong answer for the opposite one.
    it('does not let a typed query turn "no live doors" into "no matches"', () => {
      const { el, search } = render({ liveRoots: [] });

      search('أي شيء');

      expect(el('abwab-template-copy-modal-no-matches')).toBeNull();
      expect(el('abwab-template-copy-modal-doors-empty')?.textContent).toContain(ABWAB_LABELS.templateCopyEmptyDoors);
    });
  });

  describe('closing', () => {
    it('emits closed on Escape and on a backdrop click', () => {
      const { fixture, el } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      el('abwab-template-copy-modal')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      el('abwab-template-copy-modal-backdrop')!.click();

      expect(closed).toHaveLength(2);
    });

    it('opens with focus on the picker search, inside the trapped dialog', async () => {
      const { fixture, root, el } = render();
      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(document.activeElement).toBe(el('abwab-template-copy-modal-search'));
      expect(root.querySelector('[data-testid="abwab-template-copy-modal"]')!.contains(document.activeElement)).toBe(
        true,
      );
    });

    it('keeps the actions out of the scrolling body', () => {
      const { root } = render();

      const foot = root.querySelector('.qd-modal__foot')!;
      expect(foot.querySelector('[data-testid="abwab-template-copy-modal-confirm"]')).toBeTruthy();
      expect(root.querySelector('.qd-modal__body')!.contains(foot)).toBe(false);
    });

    it('clicking inside the dialog does not close it', () => {
      const { fixture, el } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      el('abwab-template-copy-modal')!.click();

      expect(closed).toHaveLength(0);
    });
  });
});
