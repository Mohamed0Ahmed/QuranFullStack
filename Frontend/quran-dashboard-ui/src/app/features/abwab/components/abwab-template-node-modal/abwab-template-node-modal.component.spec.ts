import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';

import { AbwabTemplateNodeModalComponent } from './abwab-template-node-modal.component';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';

function render() {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabTemplateNodeModalComponent] });
  const fixture = TestBed.createComponent(AbwabTemplateNodeModalComponent);
  const submitNode = vi.fn().mockReturnValue(of({ kind: 'success', data: null }));
  fixture.componentRef.setInput('open', true);
  fixture.componentRef.setInput('submitNode', submitNode);
  fixture.detectChanges();
  return { fixture, submitNode, root: fixture.nativeElement as HTMLElement };
}

function escape(fixture: ReturnType<typeof render>['fixture']): void {
  (fixture.nativeElement as HTMLElement)
    .querySelector('[data-testid="abwab-template-node-modal"]')!
    .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  fixture.detectChanges();
}

function setName(fixture: ReturnType<typeof render>['fixture'], value: string): void {
  const input = (fixture.nativeElement as HTMLElement).querySelector(
    '[data-testid="abwab-template-node-modal-name"]',
  ) as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
  fixture.detectChanges();
}

describe('AbwabTemplateNodeModalComponent', () => {
  describe('Escape (F-49) — it answers the topmost surface, and is never a dead key', () => {
    it('closes on Escape when nothing was edited', () => {
      const { fixture } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      escape(fixture);

      expect(closed).toHaveLength(1);
    });

    it('raises the discard guard instead of closing when Escape lands on an edited form', () => {
      const { fixture, root } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'مسودة');
      escape(fixture);

      expect(closed).toHaveLength(0);
      expect(root.querySelector('[data-testid="abwab-template-node-modal-discard-confirm"]')).toBeTruthy();
    });

    it('dismisses the discard strip on Escape and keeps the modal open', () => {
      const { fixture, root } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'مسودة');
      escape(fixture);
      expect(root.querySelector('[data-testid="abwab-template-node-modal-discard-confirm"]')).toBeTruthy();

      escape(fixture);

      expect(root.querySelector('[data-testid="abwab-template-node-modal-discard-confirm"]')).toBeNull();
      expect(closed).toHaveLength(0);
      expect(root.querySelector('[data-testid="abwab-template-node-modal"]')).toBeTruthy();
    });
  });

  describe('submit and validation (F-65)', () => {
    function save(fixture: ReturnType<typeof render>['fixture']): void {
      (fixture.nativeElement as HTMLElement)
        .querySelector<HTMLButtonElement>('[data-testid="abwab-template-node-modal-save"]')!
        .click();
      fixture.detectChanges();
    }

    function errorText(root: HTMLElement): string | undefined {
      return root.querySelector('[data-testid="abwab-template-node-modal-error"]')?.textContent ?? undefined;
    }

    it('rejects an empty name: sets the required error and never calls submitNode', () => {
      const { fixture, submitNode, root } = render();

      save(fixture);

      expect(submitNode).not.toHaveBeenCalled();
      expect(errorText(root)).toContain(ABWAB_LABELS.nameRequiredError);
    });

    it('keeps the modal open and shows the message when the write fails', () => {
      const { fixture, submitNode, root } = render();
      submitNode.mockReturnValue(of({ kind: 'conflict', message: 'اسم مكرر' }));
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'باب جديد');
      save(fixture);

      expect(closed).toHaveLength(0);
      expect(root.querySelector('[data-testid="abwab-template-node-modal"]')).toBeTruthy();
      expect(errorText(root)).toContain('اسم مكرر');
    });

    it('emits closed on a successful save, handing the entered fields to submitNode', () => {
      const { fixture, submitNode } = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'باب جديد');
      save(fixture);

      expect(submitNode).toHaveBeenCalledTimes(1);
      expect(submitNode).toHaveBeenCalledWith(expect.objectContaining({ name: 'باب جديد' }));
      expect(closed).toHaveLength(1);
    });

    // M3 — F-63's twin: the door modal's in-flight submit guard, mirrored on this modal.
    it('two clicks send one save: the submit is refused while the write is in flight', () => {
      const pending = new Subject<AbwabWriteOutcome<unknown>>();
      const { fixture, submitNode, root } = render();
      submitNode.mockReturnValue(pending);
      const saveButton = () =>
        root.querySelector<HTMLButtonElement>('[data-testid="abwab-template-node-modal-save"]')!;

      setName(fixture, 'باب جديد');
      save(fixture);
      save(fixture);

      expect(submitNode).toHaveBeenCalledTimes(1);
      expect(saveButton().disabled).toBe(true);

      // A failed save has to hand the button back, or the retry is impossible.
      pending.next({ kind: 'conflict', message: 'اسم مكرر' });
      fixture.detectChanges();

      expect(saveButton().disabled).toBe(false);
    });
  });
});
