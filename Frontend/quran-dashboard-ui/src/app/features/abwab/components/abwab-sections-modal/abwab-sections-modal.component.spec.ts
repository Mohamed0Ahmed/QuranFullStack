import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';

import { AbwabSectionsModalComponent } from './abwab-sections-modal.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabSectionDto } from '../../../../core/api/generated/models/abwab-section-dto';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { ScrollLockService } from '../../../../shared/ui/modal-scroll-lock/scroll-lock.service';

const SECTIONS: AbwabTreeSectionDto[] = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 3, doorsInScopeCount: 2 },
];

function success(data: AbwabSectionDto | null = null): AbwabWriteOutcome<AbwabSectionDto | null> {
  return { kind: 'success', data };
}

function conflict(message: string): AbwabWriteOutcome<never> {
  return { kind: 'conflict', message };
}

function click(root: HTMLElement, testId: string): void {
  (root.querySelector(`[data-testid="${testId}"]`) as HTMLElement).click();
}

/** The modal's own trap, not the nested confirm's — `query` returns the first match in DOM
 * order and the confirm dialog renders after the modal section. */
function trapOf(fixture: { debugElement: DebugElement }): CdkTrapFocus {
  return fixture.debugElement.query(By.directive(CdkTrapFocus)).injector.get(CdkTrapFocus);
}

function render(handlers: {
  createSection?: ReturnType<typeof vi.fn>;
  renameSection?: ReturnType<typeof vi.fn>;
  deleteSection?: ReturnType<typeof vi.fn>;
  reorderSection?: ReturnType<typeof vi.fn>;
  sections?: AbwabTreeSectionDto[];
} = {}) {
  getTestBed().resetTestingModule();
  const createSection = handlers.createSection ?? vi.fn().mockReturnValue(of(success({ id: 2, name: 'x', orderValue: 2, version: 1 })));
  const renameSection = handlers.renameSection ?? vi.fn().mockReturnValue(of(success()));
  const deleteSection = handlers.deleteSection ?? vi.fn().mockReturnValue(of(success()));
  const reorderSection = handlers.reorderSection ?? vi.fn().mockReturnValue(of(success({ id: 1, name: 'x', orderValue: 1, version: 4 })));

  TestBed.configureTestingModule({
    imports: [AbwabSectionsModalComponent],
  });
  const fixture = TestBed.createComponent(AbwabSectionsModalComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('sections', handlers.sections ?? SECTIONS);
  fixture.componentRef.setInput('createSection', createSection);
  fixture.componentRef.setInput('renameSection', renameSection);
  fixture.componentRef.setInput('deleteSection', deleteSection);
  fixture.componentRef.setInput('reorderSection', reorderSection);
  fixture.detectChanges();
  return { fixture, createSection, renameSection, deleteSection, reorderSection };
}

describe('AbwabSectionsModalComponent', () => {
  it('lists existing sections', () => {
    const { fixture } = render();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="abwab-sections-modal-row-1"]')?.textContent).toContain(
      'اللغة العربية',
    );
  });

  it('adds a section from the name input', () => {
    const { fixture, createSection } = render();
    const root = fixture.nativeElement as HTMLElement;
    const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-name-input"]')!;
    input.value = 'قسم جديد';
    input.dispatchEvent(new Event('input'));
    (root.querySelector('[data-testid="abwab-sections-modal-add"]') as HTMLElement).click();

    expect(createSection).toHaveBeenCalledWith('قسم جديد');
  });

  it('renames a section using its current version', () => {
    const { fixture, renameSection } = render();
    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-testid="abwab-sections-modal-rename-1"]') as HTMLElement).click();
    fixture.detectChanges();

    const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-rename-input-1"]')!;
    input.value = 'اسم معدّل';
    input.dispatchEvent(new Event('input'));
    (root.querySelector('[data-testid="abwab-sections-modal-rename-save-1"]') as HTMLElement).click();

    expect(renameSection).toHaveBeenCalledWith(1, 'اسم معدّل', 3);
  });

  describe('T601-602 — the order editor: click opens, Enter commits, Escape/blur cancel', () => {
    function keydown(el: Element, key: string): void {
      el.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
    }

    it('click opens the editor seeded with the current order', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-order-input-1"]')!;
      expect(input).toBeTruthy();
      expect(input.value).toBe('1');
    });

    it('Enter submits reorderSection with the id, the typed position, and the live version', () => {
      const { fixture, reorderSection } = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-order-input-1"]')!;
      input.value = '2';
      keydown(input, 'Enter');

      expect(reorderSection).toHaveBeenCalledWith(1, 2, 3);
    });

    it('Escape cancels the edit and the modal stays open (§4.2-9)', () => {
      const { fixture, reorderSection } = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-order-input-1"]')!;
      keydown(input, 'Escape');
      fixture.detectChanges();

      expect(reorderSection).not.toHaveBeenCalled();
      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-sections-modal-order-input-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-sections-modal-order-1"]')).toBeTruthy();
    });

    it('blur cancels without submitting', () => {
      const { fixture, reorderSection } = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-order-input-1"]')!;
      input.value = '2';
      input.dispatchEvent(new FocusEvent('blur', { bubbles: true }));
      fixture.detectChanges();

      expect(reorderSection).not.toHaveBeenCalled();
      expect(root.querySelector('[data-testid="abwab-sections-modal-order-input-1"]')).toBeNull();
    });

    it('a non-integer or zero input submits nothing', () => {
      const { fixture, reorderSection } = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-order-input-1"]')!;
      input.value = '0';
      keydown(input, 'Enter');

      expect(reorderSection).not.toHaveBeenCalled();
    });

    it('a 409 outcome renders the error strip', () => {
      const backendMessage = 'تم تعديل القسم من مستخدم آخر، يرجى التحديث والمحاولة مرة أخرى';
      const { fixture } = render({ reorderSection: vi.fn().mockReturnValue(of(conflict(backendMessage))) });
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();

      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-sections-modal-order-input-1"]')!;
      input.value = '2';
      keydown(input, 'Enter');
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-sections-modal-error"]')?.textContent).toContain(
        backendMessage,
      );
    });

    // An open order edit is not unsaved work (§4.2-15) — Escape closes immediately, and the
    // static-sibling reset on reopen still clears it so a reopen never shows a stale edit.
    it('resetDraft on reopen clears an open order edit', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-sections-modal-order-1"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-sections-modal-order-input-1"]')).toBeTruthy();

      fixture.componentRef.setInput('open', false);
      fixture.detectChanges();
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-sections-modal-order-input-1"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-sections-modal-order-1"]')).toBeTruthy();
    });
  });

  describe('M27 — delete answers a 409 and keeps the modal open', () => {
    it('shows the backend conflict message in the confirm dialog and never closes the modal', () => {
      const backendMessage = 'لا يمكن حذف القسم لاحتوائه على أبواب حالية';
      const { fixture } = render({ deleteSection: vi.fn().mockReturnValue(of(conflict(backendMessage))) });
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-sections-modal-delete-1');
      fixture.detectChanges();
      click(root, 'abwab-sections-modal-delete-confirm-confirm');
      fixture.detectChanges();

      // The failure belongs to the decision that caused it, not to the modal-level line, which
      // stays the create/rename surface.
      expect(root.querySelector('[data-testid="abwab-sections-modal-delete-error"]')?.textContent).toContain(
        backendMessage,
      );
      expect(root.querySelector('[data-testid="abwab-sections-modal-error"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-sections-modal-delete-confirm"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeTruthy();
      expect(closed).toHaveLength(0);
    });
  });

  describe('M28 — a section holding only archived doors deletes cleanly', () => {
    it('dispatches the delete once confirmed and closes the confirm dialog', () => {
      const deleteSection = vi.fn().mockReturnValue(of(success()));
      const { fixture } = render({ deleteSection });
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-sections-modal-delete-1');
      fixture.detectChanges();
      click(root, 'abwab-sections-modal-delete-confirm-confirm');
      fixture.detectChanges();

      expect(deleteSection).toHaveBeenCalledWith(1);
      expect(root.querySelector('[data-testid="abwab-sections-modal-delete-confirm"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-sections-modal-error"]')).toBeNull();
    });

    it('asks before deleting — the row button alone dispatches nothing', () => {
      const deleteSection = vi.fn().mockReturnValue(of(success()));
      const { fixture } = render({ deleteSection });
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-sections-modal-delete-1');
      fixture.detectChanges();

      expect(deleteSection).not.toHaveBeenCalled();
      expect(root.querySelector('[data-testid="abwab-sections-modal-delete-confirm"]')?.textContent).toContain(
        SECTIONS[0].name,
      );
    });
  });

  it('emits closed when the close control is used', () => {
    const { fixture } = render();
    const closed: void[] = [];
    fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

    (fixture.nativeElement.querySelector('[data-testid="abwab-sections-modal-close"]') as HTMLElement).click();

    expect(closed).toHaveLength(1);
  });

  describe('dialog semantics', () => {
    function escape(fixture: ReturnType<typeof render>['fixture']): void {
      (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="abwab-sections-modal"]')!
        .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      fixture.detectChanges();
    }

    function type(fixture: ReturnType<typeof render>['fixture'], testId: string, value: string): void {
      const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
        `[data-testid="${testId}"]`,
      )!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    }

    it('names itself as a dialog for assistive technology', () => {
      const { fixture } = render();
      const dialog = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-sections-modal"]')!;
      const titleId = dialog.getAttribute('aria-labelledby');

      expect(dialog.getAttribute('role')).toBe('dialog');
      expect(dialog.getAttribute('aria-modal')).toBe('true');
      expect(dialog.querySelector(`#${titleId}`)?.textContent).toContain('الأقسام');
    });

    it('closes on Escape when nothing is half-typed', () => {
      const { fixture } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      escape(fixture);

      expect(closed).toHaveLength(1);
    });

    it('guards a typed section name instead of discarding it silently', () => {
      const { fixture } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      type(fixture, 'abwab-sections-modal-name-input', 'قسم لم يُحفظ');
      escape(fixture);

      expect(closed).toHaveLength(0);
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-sections-modal-discard-confirm"]')).toBeTruthy();

      (root.querySelector('[data-testid="abwab-sections-modal-discard-confirm-no"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-sections-modal-discard-confirm"]')).toBeNull();
      expect(closed).toHaveLength(0);

      escape(fixture);
      (root.querySelector('[data-testid="abwab-sections-modal-discard-confirm-yes"]') as HTMLElement).click();
      expect(closed).toHaveLength(1);
    });

    it('treats an opened rename as dirty only once the draft differs from the saved name', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      (root.querySelector('[data-testid="abwab-sections-modal-rename-1"]') as HTMLElement).click();
      fixture.detectChanges();
      escape(fixture);
      expect(closed).toHaveLength(1);

      type(fixture, 'abwab-sections-modal-rename-input-1', 'اسم آخر');
      escape(fixture);
      expect(root.querySelector('[data-testid="abwab-sections-modal-discard-confirm"]')).toBeTruthy();
      expect(closed).toHaveLength(1);
    });

    // The page hosts this modal as a static sibling, so the instance survives every close and its
    // draft signals survive with it. Without a reset on open, «تجاهل التغييرات» would only hide
    // the draft: the next open would show the typed name back and be dirty before a keystroke.
    it('actually discards the draft, so a reopen starts clean rather than dirty', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      type(fixture, 'abwab-sections-modal-name-input', 'قسم لم يُحفظ');
      escape(fixture);
      (root.querySelector('[data-testid="abwab-sections-modal-discard-confirm-yes"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(closed).toHaveLength(1);

      fixture.componentRef.setInput('open', false);
      fixture.detectChanges();
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      expect(
        (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
          '[data-testid="abwab-sections-modal-name-input"]',
        )!.value,
      ).toBe('');

      // Dirty-before-a-keystroke is the symptom the user would actually hit: Escape must close.
      escape(fixture);
      expect(closed).toHaveLength(2);
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-sections-modal-discard-confirm"]'),
      ).toBeNull();
    });

    it('drops an abandoned rename draft too, rather than reopening in edit mode', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-sections-modal-rename-1"]') as HTMLElement).click();
      fixture.detectChanges();
      type(fixture, 'abwab-sections-modal-rename-input-1', 'اسم آخر');
      escape(fixture);
      (root.querySelector('[data-testid="abwab-sections-modal-discard-confirm-yes"]') as HTMLElement).click();
      fixture.detectChanges();

      fixture.componentRef.setInput('open', false);
      fixture.detectChanges();
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      const reopened = fixture.nativeElement as HTMLElement;
      expect(reopened.querySelector('[data-testid="abwab-sections-modal-rename-input-1"]')).toBeNull();
      expect(reopened.querySelector('[data-testid="abwab-sections-modal-rename-1"]')).toBeTruthy();
    });

    it('leaves no stale error behind on the next open', () => {
      const { fixture } = render({
        createSection: vi.fn().mockReturnValue(of(conflict('يوجد قسم بنفس الاسم'))),
      });

      type(fixture, 'abwab-sections-modal-name-input', 'قسم مكرر');
      ((fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-sections-modal-add"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-sections-modal-error"]'),
      ).toBeTruthy();

      fixture.componentRef.setInput('open', false);
      fixture.detectChanges();
      fixture.componentRef.setInput('open', true);
      fixture.detectChanges();

      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-sections-modal-error"]'),
      ).toBeNull();
    });

    // Where focus lands is a browser fact (jsdom's zero-size boxes make the CDK's focusable check
    // reject every candidate, so auto-capture never fires). What is assertable here is the
    // contract that produces it — and this modal's first tabbable control is inside the dialog.
    it('traps focus and captures it, with the first control inside the dialog', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;
      const dialog = root.querySelector('[data-testid="abwab-sections-modal"]')!;

      expect(trapOf(fixture).enabled).toBe(true);
      expect(dialog.hasAttribute('cdkTrapFocusAutoCapture')).toBe(true);
      // The order trigger is a real <button> (§4.2-16) and renders before rename/delete in DOM
      // order, so it — not rename — is now the row's first focusable control.
      expect(dialog.querySelector('button')).toBe(root.querySelector('[data-testid="abwab-sections-modal-order-1"]'));
    });

    it('yields its trap to the delete confirm while that dialog is open, and takes it back on cancel', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-sections-modal-delete-1');
      fixture.detectChanges();

      // Two live traps would fight over focus; the nested confirm owns it while it is open.
      expect(trapOf(fixture).enabled).toBe(false);

      click(root, 'abwab-sections-modal-delete-confirm-cancel');
      fixture.detectChanges();

      expect(trapOf(fixture).enabled).toBe(true);
    });

    // Two `qdModalScrollLock` holders are alive while the confirm is nested. The service is
    // reference-counted for exactly this, but abwab is the first feature to actually nest, so
    // this pins that closing the inner dialog does not unlock the page under the outer modal.
    it('keeps page scroll locked when the nested delete confirm closes', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-sections-modal-delete-1');
      fixture.detectChanges();
      click(root, 'abwab-sections-modal-delete-confirm-cancel');
      fixture.detectChanges();

      expect(TestBed.inject(ScrollLockService).isLocked()).toBe(true);
    });

    it('keeps the close control and the guard out of the scrolling body', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;

      const foot = root.querySelector('.qd-modal__foot')!;
      expect(foot.querySelector('[data-testid="abwab-sections-modal-close"]')).toBeTruthy();
      expect(root.querySelector('.qd-modal__body')!.contains(foot)).toBe(false);
    });
  });
});
