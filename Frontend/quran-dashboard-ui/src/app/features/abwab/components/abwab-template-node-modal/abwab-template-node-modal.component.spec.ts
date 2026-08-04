import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AbwabTemplateNodeModalComponent } from './abwab-template-node-modal.component';

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
});
