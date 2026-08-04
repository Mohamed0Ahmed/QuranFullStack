import { describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';

import { AbwabDoorModalComponent } from './abwab-door-modal.component';
import { AbwabWriteController, AbwabWriteOutcome } from '../../state/abwab-write.controller';
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
  globalOrderValue: null,
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

const SECTIONS = [
  { id: 7, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 0 },
  { id: 4, name: 'العقيدة', orderValue: 2, version: 1, doorsInScopeCount: 0 },
];

function fieldOf(fixture: ReturnType<typeof render>): Element | null {
  return (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-door-modal-section-field"]');
}

function pickSection(fixture: ReturnType<typeof render>, sectionId: number): void {
  const select = (fixture.nativeElement as HTMLElement).querySelector(
    '[data-testid="abwab-door-modal-section-select"]',
  ) as HTMLSelectElement;
  select.value = String(sectionId);
  select.dispatchEvent(new Event('change', { bubbles: true }));
  fixture.detectChanges();
}

function setName(fixture: ReturnType<typeof render>, value: string): void {
  const input = (fixture.nativeElement as HTMLElement).querySelector(
    '[data-testid="abwab-door-modal-name"]',
  ) as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
  fixture.detectChanges();
}

/** The trap places its initial focus once the app reports stable, so an assertion has to wait for
 * that and then let the task it schedules run (detail-modal-shell spec precedent). */
async function settleFocus(fixture: ReturnType<typeof render>): Promise<void> {
  await fixture.whenStable();
  await new Promise((resolve) => setTimeout(resolve, 0));
}

function escape(fixture: ReturnType<typeof render>): void {
  (fixture.nativeElement as HTMLElement)
    .querySelector('[data-testid="abwab-door-modal"]')!
    .dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
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

    // «كل الأبواب» is the one root create with nowhere to put the door, and the backend refuses it.
    // The shell asks instead of letting the write fail.
    it('blocks a root create under «كل الأبواب» until a section is chosen, then sends it', () => {
      let captured: CreateDoorCommand | null = null;
      const fixture = render(
        { parentId: null, activeSectionId: null, sections: SECTIONS },
        { createDoor: (command: CreateDoorCommand) => { captured = command; return of({ kind: 'success', data: EXISTING_DOOR }); } },
      );

      setName(fixture, 'باب جديد');
      clickSave(fixture);

      expect(captured).toBeNull();
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-door-modal-section-error"]')?.textContent?.trim())
        .toBe(ABWAB_LABELS.doorModalSectionRequiredError);
      expect(root.querySelector('[data-testid="abwab-door-modal-section-select"]')?.getAttribute('aria-invalid'))
        .toBe('true');

      pickSection(fixture, 7);
      clickSave(fixture);

      expect(captured!.sectionId).toBe(7);
      expect(captured!.parentId).toBeNull();
    });

    // No section exists to choose, so an empty <select> plus a post-submit error would be a dead end
    // the user cannot act on. Same handling as the restore modal's equivalent state.
    it('replaces the selector with a hint when no live section exists, and still blocks the create', () => {
      let captured: CreateDoorCommand | null = null;
      const fixture = render(
        { parentId: null, activeSectionId: null, sections: [] },
        { createDoor: (command: CreateDoorCommand) => { captured = command; return of({ kind: 'success', data: EXISTING_DOOR }); } },
      );

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-door-modal-section-select"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-door-modal-no-sections"]')?.textContent?.trim())
        .toBe(ABWAB_LABELS.doorModalNoSectionsHint);

      setName(fixture, 'باب بلا قسم متاح');
      clickSave(fixture);

      expect(captured).toBeNull();
    });

    // The selector answers a question the other two cases have already answered — showing it there
    // would invite a contradiction the backend then has to refuse.
    it('shows the section selector only for a root create under «كل الأبواب»', () => {
      const onAllDoors = render({ parentId: null, activeSectionId: null, sections: SECTIONS }, {});
      expect(fieldOf(onAllDoors)).toBeTruthy();

      const onSectionTab = render({ parentId: null, activeSectionId: 4, sections: SECTIONS }, {});
      expect(fieldOf(onSectionTab)).toBeNull();

      const underParent = render({ parentId: 9, activeSectionId: null, sections: SECTIONS }, {});
      expect(fieldOf(underParent)).toBeNull();

      const editing = render({ door: EXISTING_DOOR, activeSectionId: null, sections: SECTIONS }, {});
      expect(fieldOf(editing)).toBeNull();
    });
  });

  // F-63: the create command carries no version, so a second dispatch is indistinguishable from a
  // deliberate second door. The save button is what has to refuse it.
  describe('the save button refuses a second submit while the write is in flight', () => {
    function saveButton(fixture: ReturnType<typeof render>): HTMLButtonElement {
      return (fixture.nativeElement as HTMLElement)
        .querySelector('[data-testid="abwab-door-modal-save"]') as HTMLButtonElement;
    }

    it('creates one door for two clicks, and re-enables save when the create fails', () => {
      const inFlight = new Subject<AbwabWriteOutcome<AbwabDoorDto>>();
      const createDoor = vi.fn().mockReturnValue(inFlight);
      const fixture = render({ parentId: null, activeSectionId: 4 }, { createDoor });

      setName(fixture, 'باب جديد');
      clickSave(fixture);
      clickSave(fixture);

      expect(createDoor).toHaveBeenCalledTimes(1);
      expect(saveButton(fixture).disabled).toBe(true);

      inFlight.next({ kind: 'conflict', message: 'يوجد باب بنفس الاسم' });
      fixture.detectChanges();

      expect(saveButton(fixture).disabled).toBe(false);
    });

    it('leaves save enabled when a validation failure stops the submit before any write', () => {
      const createDoor = vi.fn().mockReturnValue(new Subject<AbwabWriteOutcome<AbwabDoorDto>>());
      const fixture = render({ parentId: null, activeSectionId: null, sections: SECTIONS }, { createDoor });

      setName(fixture, 'باب جديد');
      clickSave(fixture);

      expect(createDoor).not.toHaveBeenCalled();
      expect(saveButton(fixture).disabled).toBe(false);

      pickSection(fixture, 7);
      clickSave(fixture);

      expect(createDoor).toHaveBeenCalledTimes(1);
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
        { parentId: null, activeSectionId: 4 },
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
    it('prefills the form from the door under edit', () => {
      const fixture = render({ door: EXISTING_DOOR });
      const root = fixture.nativeElement as HTMLElement;

      expect((root.querySelector('[data-testid="abwab-door-modal-name"]') as HTMLInputElement).value).toBe(
        'الألوهية',
      );
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

    it('shares the create path\'s in-flight guard, so two clicks send one edit', () => {
      const updateDoor = vi.fn().mockReturnValue(new Subject<AbwabWriteOutcome<AbwabDoorDto>>());
      const fixture = render({ door: EXISTING_DOOR }, { updateDoor });

      setName(fixture, 'الألوهية المعدّلة');
      clickSave(fixture);
      clickSave(fixture);

      expect(updateDoor).toHaveBeenCalledTimes(1);
    });
  });

  describe('dialog semantics', () => {
    it('opens with focus on the name field, inside the trapped dialog', async () => {
      const fixture = render();
      await settleFocus(fixture);

      const root = fixture.nativeElement as HTMLElement;
      const dialog = root.querySelector('[data-testid="abwab-door-modal"]')!;
      const nameField = root.querySelector('[data-testid="abwab-door-modal-name"]')!;
      expect(document.activeElement).toBe(nameField);
      expect(dialog.contains(document.activeElement)).toBe(true);
      // The queued focus above is only half the contract: the trap's own auto-capture must aim at
      // the same field, or the modal opens with two focus moves instead of one.
      expect(nameField.hasAttribute('cdkFocusInitial')).toBe(true);
    });

    it('closes on Escape when nothing was edited', () => {
      const fixture = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      escape(fixture);

      expect(closed).toHaveLength(1);
    });

    it('raises the discard guard instead of closing when Escape lands on an edited form', () => {
      const fixture = render();
      const closed: void[] = [];
      fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

      setName(fixture, 'مسودة');
      escape(fixture);

      expect(closed).toHaveLength(0);
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-door-modal-discard-confirm"]'),
      ).toBeTruthy();
    });

    // The error surface is `qd-state`, which reserves a message row and carries the shared
    // container's padding — rendered unconditionally it is a 105px empty danger box on every
    // open, which is exactly what shipped once and was caught by a screenshot rather than here.
    // Asserting the message text alone cannot fail on that bug; asserting absence can.
    it('renders no error surface at all until a write actually fails', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-door-modal-error"]')).toBeNull();
      expect(root.querySelector('[data-testid="qd-state-error"]')).toBeNull();

      setName(fixture, 'باب جديد');
      expect(root.querySelector('[data-testid="abwab-door-modal-error"]')).toBeNull();
    });

    it('keeps the actions out of the scrolling body so the guard cannot scroll away', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const foot = root.querySelector('.qd-modal__foot')!;
      const body = root.querySelector('.qd-modal__body')!;
      expect(foot.querySelector('[data-testid="abwab-door-modal-save"]')).toBeTruthy();
      expect(body.contains(foot)).toBe(false);
    });
  });
});
