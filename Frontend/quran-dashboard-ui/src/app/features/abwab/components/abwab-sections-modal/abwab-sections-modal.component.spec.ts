import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AbwabSectionsModalComponent } from './abwab-sections-modal.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabSectionDto } from '../../../../core/api/generated/models/abwab-section-dto';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';

const SECTIONS: AbwabTreeSectionDto[] = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 3, doorsInScopeCount: 2 },
];

function success(data: AbwabSectionDto | null = null): AbwabWriteOutcome<AbwabSectionDto | null> {
  return { kind: 'success', data };
}

function conflict(message: string): AbwabWriteOutcome<never> {
  return { kind: 'conflict', message };
}

function render(handlers: {
  createSection?: ReturnType<typeof vi.fn>;
  renameSection?: ReturnType<typeof vi.fn>;
  deleteSection?: ReturnType<typeof vi.fn>;
  sections?: AbwabTreeSectionDto[];
} = {}) {
  getTestBed().resetTestingModule();
  const createSection = handlers.createSection ?? vi.fn().mockReturnValue(of(success({ id: 2, name: 'x', orderValue: 2, version: 1 })));
  const renameSection = handlers.renameSection ?? vi.fn().mockReturnValue(of(success()));
  const deleteSection = handlers.deleteSection ?? vi.fn().mockReturnValue(of(success()));

  TestBed.configureTestingModule({
    imports: [AbwabSectionsModalComponent],
  });
  const fixture = TestBed.createComponent(AbwabSectionsModalComponent);
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('sections', handlers.sections ?? SECTIONS);
  fixture.componentRef.setInput('createSection', createSection);
  fixture.componentRef.setInput('renameSection', renameSection);
  fixture.componentRef.setInput('deleteSection', deleteSection);
  fixture.detectChanges();
  return { fixture, createSection, renameSection, deleteSection };
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

  describe('M27 — delete answers a 409 and keeps the modal open', () => {
    it('shows the backend conflict message inline and never closes the modal', () => {
      const backendMessage = 'لا يمكن حذف القسم لاحتوائه على أبواب حالية';
      const { fixture } = render({ deleteSection: vi.fn().mockReturnValue(of(conflict(backendMessage))) });
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-sections-modal-delete-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-sections-modal-error"]')?.textContent).toContain(
        backendMessage,
      );
      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeTruthy();
      expect(closed).toHaveLength(0);
    });
  });

  describe('M28 — a section holding only archived doors deletes cleanly', () => {
    it('dispatches the delete without an inline error', () => {
      const deleteSection = vi.fn().mockReturnValue(of(success()));
      const { fixture } = render({ deleteSection });
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-sections-modal-delete-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(deleteSection).toHaveBeenCalledWith(1);
      expect(root.querySelector('[data-testid="abwab-sections-modal-error"]')).toBeNull();
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

    it('keeps the close control and the guard out of the scrolling body', () => {
      const { fixture } = render();
      const root = fixture.nativeElement as HTMLElement;

      const foot = root.querySelector('.qd-modal__foot')!;
      expect(foot.querySelector('[data-testid="abwab-sections-modal-close"]')).toBeTruthy();
      expect(root.querySelector('.qd-modal__body')!.contains(foot)).toBe(false);
    });
  });
});
