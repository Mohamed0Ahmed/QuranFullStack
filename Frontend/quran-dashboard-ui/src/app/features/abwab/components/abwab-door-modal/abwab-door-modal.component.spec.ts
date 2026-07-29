import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AbwabDoorModalComponent } from './abwab-door-modal.component';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { CreateDoorCommand } from '../../../../core/api/generated/models/create-door-command';
import { EditDoorBody } from '../../../../core/api/generated/models/edit-door-body';
import { ABWAB_LABELS } from '../../models/abwab.labels';

const EXISTING_DOOR: AbwabDoorDto = {
  id: 5,
  name: 'الألوهية',
  description: 'وصف',
  representativeAyahText: null,
  aliases: ['التوحيد'],
  parentId: null,
  sectionId: 1,
  orderValue: 1,
  version: 3,
};

function render(overrides: Record<string, unknown> = {}, controllerStub: Partial<AbwabWriteController> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    imports: [AbwabDoorModalComponent],
    providers: [{ provide: AbwabWriteController, useValue: controllerStub }],
  });
  const fixture = TestBed.createComponent(AbwabDoorModalComponent);
  fixture.componentRef.setInput('open', true);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

function setName(fixture: ReturnType<typeof render>, value: string): void {
  const input = (fixture.nativeElement as HTMLElement).querySelector(
    '[data-testid="abwab-door-modal-name"]',
  ) as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
  fixture.detectChanges();
}

function clickSave(fixture: ReturnType<typeof render>): void {
  ((fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-door-modal-save"]') as HTMLElement).click();
  fixture.detectChanges();
}

describe('AbwabDoorModalComponent', () => {
  describe('M10 — create under a parent sends parentId and never sends the active section id', () => {
    it('nulls sectionId when a parentId is present, even though a section tab is active', () => {
      let captured: CreateDoorCommand | null = null;
      const fixture = render(
        { parentId: 7, parentName: 'العلم بالله', activeSectionId: 4 },
        { createDoor: (command: CreateDoorCommand) => { captured = command; return of({ kind: 'success', data: EXISTING_DOOR }); } },
      );

      setName(fixture, 'الأسماء والصفات');
      clickSave(fixture);

      expect(captured).not.toBeNull();
      expect(captured!.parentId).toBe(7);
      expect(captured!.sectionId).toBeNull();
    });
  });

  describe('M11 — create at root sends the active section id, or null under «كل الأبواب»', () => {
    it('sends the active section id when creating at root with a section tab active', () => {
      let captured: CreateDoorCommand | null = null;
      const fixture = render(
        { parentId: null, activeSectionId: 4 },
        { createDoor: (command: CreateDoorCommand) => { captured = command; return of({ kind: 'success', data: EXISTING_DOOR }); } },
      );

      setName(fixture, 'باب جديد');
      clickSave(fixture);

      expect(captured!.parentId).toBeNull();
      expect(captured!.sectionId).toBe(4);
    });

    it('sends null under «كل الأبواب» (no active section)', () => {
      let captured: CreateDoorCommand | null = null;
      const fixture = render(
        { parentId: null, activeSectionId: null },
        { createDoor: (command: CreateDoorCommand) => { captured = command; return of({ kind: 'success', data: EXISTING_DOOR }); } },
      );

      setName(fixture, 'باب جديد');
      clickSave(fixture);

      expect(captured!.sectionId).toBeNull();
    });
  });

  describe('M12 — the dirty guard blocks close with unsaved input; the inline error surface renders the backend message', () => {
    it('does not close immediately when the form is dirty, and closes once discard is confirmed', () => {
      const fixture = render({}, {});
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'تعديل غير محفوظ');
      (fixture.nativeElement as HTMLElement)
        .querySelector<HTMLElement>('[data-testid="abwab-door-modal-cancel"]')
        ?.click();
      fixture.detectChanges();

      expect(closed).toHaveLength(0);
      const confirmStrip = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="abwab-door-modal-discard-confirm"]',
      );
      expect(confirmStrip).toBeTruthy();

      (confirmStrip as HTMLElement)
        .querySelector<HTMLElement>('[data-testid="abwab-door-modal-discard-confirm-yes"]')
        ?.click();
      fixture.detectChanges();

      expect(closed).toHaveLength(1);
    });

    it('closes immediately when the form was never touched', () => {
      const fixture = render({}, {});
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      (fixture.nativeElement as HTMLElement)
        .querySelector<HTMLElement>('[data-testid="abwab-door-modal-cancel"]')
        ?.click();

      expect(closed).toHaveLength(1);
    });

    it('renders the backend failure message inline rather than closing', () => {
      const fixture = render(
        { parentId: null, activeSectionId: null },
        { createDoor: () => of({ kind: 'invalid', message: 'اسم الباب مكرر في هذا النطاق' }) },
      );
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'باب مكرر');
      clickSave(fixture);

      const errorEl = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-door-modal-error"]');
      expect(errorEl?.textContent?.trim()).toBe('اسم الباب مكرر في هذا النطاق');
      expect(closed).toHaveLength(0);
    });
  });

  describe('M13 — alias chips add on Enter and remove through qd-chip\'s remove output', () => {
    it('adds a chip when Enter is pressed in the alias input, and clears the input', () => {
      const fixture = render();
      const aliasInput = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="abwab-door-modal-alias-input"]',
      ) as HTMLInputElement;

      aliasInput.value = 'اسم بديل';
      aliasInput.dispatchEvent(new Event('input', { bubbles: true }));
      fixture.detectChanges();
      aliasInput.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      fixture.detectChanges();

      const chips = (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid="qd-chip"]');
      expect(Array.from(chips).some((c) => c.textContent?.includes('اسم بديل'))).toBe(true);
      expect(aliasInput.value).toBe('');
    });

    it('removes the alias when the chip\'s remove button is clicked', () => {
      const fixture = render({ door: EXISTING_DOOR }); // starts with alias «التوحيد»
      const root = fixture.nativeElement as HTMLElement;

      expect(
        Array.from(root.querySelectorAll('[data-testid="qd-chip"]')).some((c) => c.textContent?.includes('التوحيد')),
      ).toBe(true);

      const removeButton = root.querySelector('[data-testid="qd-chip-remove"]') as HTMLButtonElement;
      removeButton.click();
      fixture.detectChanges();

      expect(root.querySelectorAll('[data-testid="qd-chip"]')).toHaveLength(0);
    });
  });

  describe('edit mode', () => {
    it('prefills the form and shows the tracking-data box only on edit', () => {
      const fixture = render({ door: EXISTING_DOOR });
      const root = fixture.nativeElement as HTMLElement;

      expect((root.querySelector('[data-testid="abwab-door-modal-name"]') as HTMLInputElement).value).toBe(
        'الألوهية',
      );
      expect(root.querySelector('[data-testid="abwab-door-modal-meta"]')).toBeTruthy();
    });

    it('does not show the tracking-data box in create mode', () => {
      const fixture = render();
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-door-modal-meta"]'),
      ).toBeNull();
    });

    it('sends the door\'s own version token on edit, not a caller-supplied one', () => {
      let captured: EditDoorBody | null = null;
      const fixture = render(
        { door: EXISTING_DOOR },
        { updateDoor: (_id: number, body: EditDoorBody) => { captured = body; return of({ kind: 'success', data: EXISTING_DOOR }); } },
      );

      setName(fixture, 'الألوهية المعدّلة');
      clickSave(fixture);

      expect(captured!.version).toBe(EXISTING_DOOR.version);
    });
  });
});
