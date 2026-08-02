import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AbwabDoorRestoreModalComponent } from './abwab-door-restore-modal.component';
import { AbwabWriteController } from '../../state/abwab-write.controller';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabNode } from '../../models/abwab.models';
import { buildAbwabTreeSnapshot } from '../../state/abwab-tree.builder';
import { ABWAB_LABELS } from '../../models/abwab.labels';

const SECTIONS = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 0 },
  { id: 2, name: 'العقيدة', orderValue: 2, version: 1, doorsInScopeCount: 0 },
];

function doorDto(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    description: null,
    representativeAyahText: null,
    aliases: [],
    directChildCount: 0,
    relationCount: 0,
    globalOrderValue: null,
    isArchived: true,
    orderValue: overrides.id,
    parentId: null,
    sectionId: 1,
    sectionRetired: false,
    version: 1,
    ...overrides,
  };
}

/**
 * Nodes come out of the real builder rather than hand-written literals: `sectionRetired` has to
 * survive the DTO → node mapping, and a literal would assert the fixture instead. A dropped mapping
 * reads as `undefined` — falsy — so the modal would silently stop asking for a destination the
 * backend then demands.
 */
function nodesFrom(doors: readonly AbwabTreeDoorDto[]): ReadonlyMap<number, AbwabNode> {
  const tree: AbwabTreeDto = { doors: [...doors], sections: SECTIONS, version: 'v1' };
  return buildAbwabTreeSnapshot(tree).byId;
}

function render(
  door: AbwabNode | null,
  overrides: Record<string, unknown> = {},
  controllerStub: Partial<AbwabWriteController> = {},
) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    imports: [AbwabDoorRestoreModalComponent],
    providers: [{ provide: AbwabWriteController, useValue: controllerStub }],
  });
  const fixture = TestBed.createComponent(AbwabDoorRestoreModalComponent);
  fixture.componentRef.setInput('door', door);
  fixture.componentRef.setInput('sections', SECTIONS);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

const query = (fixture: ReturnType<typeof render>, testId: string): HTMLElement | null =>
  (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);

const select = (fixture: ReturnType<typeof render>): HTMLSelectElement | null =>
  query(fixture, 'abwab-door-restore-modal-section-select') as HTMLSelectElement | null;

const confirmButton = (fixture: ReturnType<typeof render>): HTMLButtonElement =>
  query(fixture, 'qd-confirm-dialog-confirm') as HTMLButtonElement;

function pick(fixture: ReturnType<typeof render>, sectionId: number): void {
  const el = select(fixture)!;
  el.value = String(sectionId);
  el.dispatchEvent(new Event('change', { bubbles: true }));
  fixture.detectChanges();
}

describe('AbwabDoorRestoreModalComponent', () => {
  it('renders nothing until a door is given', () => {
    const fixture = render(null);

    expect(query(fixture, 'qd-confirm-dialog')).toBeNull();
  });

  describe('a root whose section is still live', () => {
    const node = () => nodesFrom([doorDto({ id: 3, name: 'باب مؤرشف', sectionId: 2 })]).get(3)!;

    it('prefills the selector with its own section and allows confirming straight away', () => {
      const fixture = render(node());

      expect(select(fixture)!.value).toBe('2');
      expect(confirmButton(fixture).disabled).toBe(false);
      expect(query(fixture, 'abwab-door-restore-modal-retired-hint')).toBeNull();
    });

    // The key is omitted, not sent as the same id: an absent sectionId is the backend's "back where
    // it came from", which is exactly what an unchanged selection means.
    it('omits the destination when the prefilled section is left alone', () => {
      const calls: unknown[] = [];
      const fixture = render(node(), {}, {
        restoreDoor: (id: number, options: unknown) => {
          calls.push([id, options]);
          return of({ kind: 'success', data: null });
        },
      } as unknown as Partial<AbwabWriteController>);

      confirmButton(fixture).click();

      expect(calls).toEqual([[3, { version: 1 }]]);
    });

    it('sends the destination when the section is changed on the way back', () => {
      const calls: unknown[] = [];
      const fixture = render(node(), {}, {
        restoreDoor: (id: number, options: unknown) => {
          calls.push([id, options]);
          return of({ kind: 'success', data: null });
        },
      } as unknown as Partial<AbwabWriteController>);

      pick(fixture, 1);
      confirmButton(fixture).click();

      expect(calls).toEqual([[3, { sectionId: 1, version: 1 }]]);
    });
  });

  describe('a root whose section was retired', () => {
    const node = () =>
      nodesFrom([doorDto({ id: 3, name: 'باب في قسم محذوف', sectionId: 9, sectionRetired: true })]).get(3)!;

    it('starts empty, says why, and blocks confirm until a section is chosen', () => {
      const fixture = render(node());

      expect(select(fixture)!.value).toBe('');
      expect(query(fixture, 'abwab-door-restore-modal-retired-hint')?.textContent?.trim())
        .toBe(ABWAB_LABELS.restoreModalRetiredHint);
      expect(confirmButton(fixture).disabled).toBe(true);

      pick(fixture, 1);

      expect(confirmButton(fixture).disabled).toBe(false);
    });

    it('writes nothing while the destination is missing', () => {
      const calls: unknown[] = [];
      const fixture = render(node(), {}, {
        restoreDoor: (...args: unknown[]) => {
          calls.push(args);
          return of({ kind: 'success', data: null });
        },
      } as unknown as Partial<AbwabWriteController>);

      confirmButton(fixture).click();

      expect(calls).toEqual([]);
    });

    // Nothing to pick and no way forward — say so rather than showing an empty control.
    it('replaces the selector with a hint when no live section exists', () => {
      const fixture = render(node(), { sections: [] });

      expect(select(fixture)).toBeNull();
      expect(query(fixture, 'abwab-door-restore-modal-no-sections')?.textContent?.trim())
        .toBe(ABWAB_LABELS.restoreModalNoSectionsHint);
      expect(confirmButton(fixture).disabled).toBe(true);
    });
  });

  // A child derives its live parent's current section, so there is no question to put to the user.
  it('offers no selector for a child door, and states where it returns', () => {
    const nodes = nodesFrom([
      doorDto({ id: 1, name: 'الأب', isArchived: false }),
      doorDto({ id: 2, name: 'الابن', parentId: 1 }),
    ]);
    const fixture = render(nodes.get(2)!, { ancestors: [nodes.get(1)!] });

    expect(select(fixture)).toBeNull();
    expect(query(fixture, 'abwab-door-restore-modal-child-hint')?.textContent?.trim())
      .toBe(ABWAB_LABELS.restoreModalChildHint);
    expect(query(fixture, 'abwab-door-restore-modal-path')?.textContent).toContain('الأب');
  });

  it('keeps the modal open and shows the failure inline when the write is refused', () => {
    const node = nodesFrom([doorDto({ id: 3, name: 'باب مؤرشف' })]).get(3)!;
    const closed: void[] = [];
    const fixture = render(node, {}, {
      restoreDoor: () => of({ kind: 'conflict', message: 'تم تعديل الباب من مستخدم آخر' }),
    } as unknown as Partial<AbwabWriteController>);
    fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

    confirmButton(fixture).click();
    fixture.detectChanges();

    expect(query(fixture, 'abwab-door-restore-modal-error')?.textContent)
      .toContain('تم تعديل الباب من مستخدم آخر');
    expect(closed).toHaveLength(0);
    expect(confirmButton(fixture).disabled).toBe(false);
  });

  it('closes and reports success once the write lands', () => {
    const node = nodesFrom([doorDto({ id: 3, name: 'باب مؤرشف' })]).get(3)!;
    const events: string[] = [];
    const fixture = render(node, {}, {
      restoreDoor: () => of({ kind: 'success', data: null }),
    } as unknown as Partial<AbwabWriteController>);
    fixture.componentInstance.restored.subscribe(() => events.push('restored'));
    fixture.componentInstance.closed.subscribe(() => events.push('closed'));

    confirmButton(fixture).click();

    expect(events).toEqual(['restored', 'closed']);
  });

  it('emits closed on cancel without writing', () => {
    const node = nodesFrom([doorDto({ id: 3, name: 'باب مؤرشف' })]).get(3)!;
    const calls: unknown[] = [];
    const fixture = render(node, {}, {
      restoreDoor: (...args: unknown[]) => {
        calls.push(args);
        return of({ kind: 'success', data: null });
      },
    } as unknown as Partial<AbwabWriteController>);
    const closed: void[] = [];
    fixture.componentInstance.closed.subscribe(() => closed.push(undefined));

    (query(fixture, 'qd-confirm-dialog-cancel') as HTMLElement).click();

    expect(closed).toHaveLength(1);
    expect(calls).toEqual([]);
  });
});
