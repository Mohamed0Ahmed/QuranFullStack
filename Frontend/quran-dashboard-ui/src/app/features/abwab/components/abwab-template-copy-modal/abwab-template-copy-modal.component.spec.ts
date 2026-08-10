import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Observable, Subject, of } from 'rxjs';

import { AbwabTemplateCopyModalComponent } from './abwab-template-copy-modal.component';
import { AbwabDoorPickerComponent } from '../abwab-door-picker/abwab-door-picker.component';
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
  readonly applyResponse?: Observable<AbwabWriteOutcome<AbwabDoorDto[] | null>>;
}

function render(options: RenderOptions = {}) {
  getTestBed().resetTestingModule();
  const applyTemplate = vi
    .fn()
    .mockReturnValue(
      options.applyResponse ??
        of(options.applyOutcome ?? ({ kind: 'success', data: null } as AbwabWriteOutcome<AbwabDoorDto[] | null>)),
    );

  TestBed.configureTestingModule({ imports: [AbwabTemplateCopyModalComponent] });
  const fixture = TestBed.createComponent(AbwabTemplateCopyModalComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('templateName', 'قالب الأخلاق');
  fixture.componentRef.setInput('templateNodeCount', 4);
  fixture.componentRef.setInput('liveRoots', options.liveRoots ?? ROOTS);
  fixture.componentRef.setInput('doorsLoading', options.doorsLoading ?? false);
  fixture.componentRef.setInput('doorsError', options.doorsError ?? null);
  fixture.componentRef.setInput('applyTemplate', applyTemplate);
  fixture.componentRef.setInput('canApply', true);
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
  it('disables and rejects a stale template apply without its exact permission', () => {
    const { fixture, el, click, applyTemplate } = render();
    fixture.componentRef.setInput('canApply', false);
    fixture.detectChanges();

    click('abwab-template-copy-modal-pick-1');
    expect((el('abwab-template-copy-modal-confirm') as HTMLButtonElement).disabled).toBe(true);
    (fixture.componentInstance as unknown as { confirm(): void }).confirm();

    expect(applyTemplate).not.toHaveBeenCalled();
  });

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

  // F-61: the apply carries no version token, so nothing downstream can tell a duplicate apply
  // from a deliberate one — the modal itself has to refuse the second click.
  it('does not re-issue the apply while the first one is still in flight', () => {
    const inFlight = new Subject<AbwabWriteOutcome<AbwabDoorDto[] | null>>();
    const { fixture, el, click, applyTemplate } = render({ applyResponse: inFlight });
    const confirmButton = () => el('abwab-template-copy-modal-confirm') as HTMLButtonElement;

    click('abwab-template-copy-modal-pick-1');
    click('abwab-template-copy-modal-confirm');
    click('abwab-template-copy-modal-confirm');

    expect(applyTemplate).toHaveBeenCalledTimes(1);
    expect(confirmButton().disabled).toBe(true);

    // A failed apply has to hand the button back, or the retry is impossible.
    inFlight.next({ kind: 'conflict', message: 'يوجد باب بنفس الاسم تحت الهدف' });
    fixture.detectChanges();

    expect(confirmButton().disabled).toBe(false);
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

  // The apply failure DROPS the polite announcer (abwab-templates.controller.ts), so this region
  // is the failure's only channel. role="alert" announces reliably only if the element already
  // exists when the text is inserted — the region must be in the DOM, empty and quiet, from open.
  describe('the reserved error live region', () => {
    it('exists in the DOM, empty and quiet, before any failure', () => {
      const { el } = render();
      const region = el('abwab-template-copy-modal-error');
      const box = region?.querySelector('[data-testid="qd-state-error"]');

      expect(box).toBeTruthy();
      expect(box?.getAttribute('role')).toBe('alert');
      expect(box?.classList.contains('qd-state--reserve-empty')).toBe(true);
      expect(box?.textContent?.trim()).toBe('');
    });

    it('lands the apply failure in the SAME pre-existing element, which sheds its quiet class', () => {
      const message = 'يوجد باب بنفس الاسم تحت الهدف';
      const { el, click } = render({ applyOutcome: { kind: 'conflict', message } });
      const boxBefore = el('abwab-template-copy-modal-error')?.querySelector('[data-testid="qd-state-error"]');
      expect(boxBefore).toBeTruthy();

      click('abwab-template-copy-modal-pick-1');
      click('abwab-template-copy-modal-confirm');
      const boxAfter = el('abwab-template-copy-modal-error')?.querySelector('[data-testid="qd-state-error"]');

      expect(boxAfter).toBe(boxBefore);
      expect(boxAfter?.textContent).toContain(message);
      expect(boxAfter?.classList.contains('qd-state--reserve-empty')).toBe(false);
    });
  });

  describe('the doors snapshot states are separated', () => {
    // F-98 pin: 'ready' must be reachable — a silent reversion of the final branch to 'empty'
    // passed every other test in this file.
    it('hands the door picker status "ready" when live roots are present and loading is resolved', () => {
      const { fixture } = render();

      const picker = fixture.debugElement.query(By.directive(AbwabDoorPickerComponent))
        .componentInstance as AbwabDoorPickerComponent;

      expect(picker.status()).toBe('ready');
    });

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
    // The same F-92 shape the delete confirms carry: a dismissal mid-apply would abandon a write
    // that still lands, with no feedback about its outcome.
    it('refuses to dismiss while the apply is in flight', () => {
      const inFlight = new Subject<AbwabWriteOutcome<AbwabDoorDto[] | null>>();
      const { fixture, el, click } = render({ applyResponse: inFlight });
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      click('abwab-template-copy-modal-pick-1');
      click('abwab-template-copy-modal-confirm');

      el('abwab-template-copy-modal')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      el('abwab-template-copy-modal-backdrop')!.click();
      el('abwab-template-copy-modal-cancel')!.click();
      fixture.detectChanges();

      expect(closed).toHaveLength(0);

      // A resolved apply hands the dismissal paths back.
      inFlight.next({ kind: 'error', message: 'تعذر الاتصال بالخادم. حاول مرة أخرى.' });
      fixture.detectChanges();
      el('abwab-template-copy-modal-backdrop')!.click();

      expect(closed).toHaveLength(1);
    });

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

    // Sticky footer and single body scroller are `qd-modal-shell`'s now, so the two regions are
    // read from the shell's published test ids instead of the retired `.qd-modal__*` classes.
    it('keeps the actions out of the scrolling body', () => {
      const { root } = render();

      const foot = root.querySelector('[data-testid="abwab-template-copy-modal-footer"]')!;
      expect(foot.querySelector('[data-testid="abwab-template-copy-modal-confirm"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-template-copy-modal-body"]')!.contains(foot)).toBe(false);
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
