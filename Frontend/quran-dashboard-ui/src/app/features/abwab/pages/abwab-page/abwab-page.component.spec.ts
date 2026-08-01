import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { AbwabPageComponent } from './abwab-page.component';
import { AbwabApi } from '../../data-access/abwab.api';
import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { ABWAB_LABELS } from '../../models/abwab.labels';

function door(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    aliases: [],
    description: null,
    directChildCount: 0,
    relationCount: 0,
    globalOrderValue: overrides.parentId == null && !overrides.isArchived ? overrides.id : null,
    isArchived: false,
    orderValue: overrides.id,
    parentId: null,
    representativeAyahText: null,
    sectionId: null,
    version: 1,
    ...overrides,
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

const TREE: AbwabTreeDto = {
  doors: [
    // orderValue and globalOrderValue deliberately differ here (1 vs 9, 2 vs 8) — a test
    // that reads the wrong field is caught rather than passing on coincidentally equal values.
    door({ id: 1, name: 'العلم بالله', sectionId: 1, orderValue: 1, globalOrderValue: 9 }),
    door({ id: 2, name: 'الرسول', sectionId: 1, orderValue: 2, globalOrderValue: 8 }),
    door({ id: 3, name: 'باب مؤرشف', isArchived: true, orderValue: 3 }),
  ],
  sections: [{ id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 2 }],
  version: 'v1',
};

describe('AbwabPageComponent', () => {
  let router: Router;
  let archiveDoor: ReturnType<typeof vi.fn>;
  let restoreDoor: ReturnType<typeof vi.fn>;
  let reorderDoor: ReturnType<typeof vi.fn>;
  let moveDoor: ReturnType<typeof vi.fn>;
  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();
    queryParamMap$.next(convertToParamMap({}));
    archiveDoor = vi.fn().mockReturnValue(of(ok(null)));
    restoreDoor = vi.fn().mockReturnValue(of(ok({ door: TREE.doors[0], detachedFromArchivedSection: false })));
    reorderDoor = vi.fn().mockReturnValue(of(ok(TREE.doors[0])));
    moveDoor = vi.fn().mockReturnValue(of(ok(TREE.doors[0])));

    await TestBed.configureTestingModule({
      imports: [AbwabPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: AbwabApi,
          useValue: {
            getTree: vi.fn().mockReturnValue(of(ok(TREE))),
            archiveDoor,
            restoreDoor,
            reorderDoor,
            moveDoor,
          },
        },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  function render() {
    const fixture = TestBed.createComponent(AbwabPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('composes the toolbar, tree, side panel and announcer', () => {
    const root = render().nativeElement as HTMLElement;

    expect(root.querySelector('qd-abwab-toolbar')).toBeTruthy();
    expect(root.querySelector('qd-abwab-tree')).toBeTruthy();
    expect(root.querySelector('qd-abwab-side-panel')).toBeTruthy();
    expect(root.querySelector('qd-abwab-announcer')).toBeTruthy();
  });

  describe('M31 — archived doors are unreachable from the live tree and tabs', () => {
    it('never renders the archived door as a tree row', () => {
      const root = render().nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-row-1"]')).toBeTruthy();
    });

    it('only lists real (non-derived) sections as tabs — an archived door never manufactures a tab', () => {
      const root = render().nativeElement as HTMLElement;
      const tabLabels = Array.from(root.querySelectorAll('[role="tab"]')).map((el) => el.textContent?.trim());
      expect(tabLabels).toEqual(['كل الأبواب', 'اللغة العربية']);
    });
  });

  it('selecting a tree row updates the side panel and writes door=<id> to the URL', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
      new MouseEvent('click', { bubbles: true }),
    );
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')?.textContent).toContain(
      'العلم بالله',
    );
    expect(router.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ door: '1' }) }),
    );
  });

  it('choosing a section tab writes section=<id> to the URL', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="abwab-toolbar-tab-1"]') as HTMLElement).click();

    expect(router.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ section: '1' }) }),
    );
  });

  it('the header add-root control opens the create modal at root scope', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="abwab-page-add-root"]') as HTMLElement).click();
    fixture.detectChanges();

    const modal = root.querySelector('[data-testid="abwab-door-modal"]');
    expect(modal).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-door-modal-name"]')).toBeTruthy();
  });

  it('side panel add-child opens the create modal scoped to the selected door as parent', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
      new MouseEvent('click', { bubbles: true }),
    );
    fixture.detectChanges();
    (root.querySelector('[data-testid="abwab-side-panel-op-add-child"]') as HTMLElement).click();
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="abwab-door-modal-context"]')?.textContent).toContain('العلم بالله');
  });

  it('archive requires confirmation, then dispatches and clears the selection', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
      new MouseEvent('click', { bubbles: true }),
    );
    fixture.detectChanges();
    (root.querySelector('[data-testid="abwab-side-panel-op-archive"]') as HTMLElement).click();
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="abwab-page-archive-confirm"]')).toBeTruthy();
    (root.querySelector('[data-testid="abwab-page-archive-confirm-yes"]') as HTMLElement).click();
    fixture.detectChanges();

    expect(archiveDoor).toHaveBeenCalledWith(1, { version: 1 });
    expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeNull();
  });

  describe('T502 — the view toggle switches the main column to cards', () => {
    it('shows the tree by default and swaps to cards after the toolbar toggle', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('qd-abwab-tree')).toBeTruthy();

      (root.querySelector('[data-testid="abwab-toolbar-view-cards"]') as HTMLElement).click();
      expect(router.navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({ queryParams: expect.objectContaining({ view: 'cards' }) }),
      );

      queryParamMap$.next(convertToParamMap({ view: 'cards' }));
      fixture.detectChanges();
      expect(root.querySelector('qd-abwab-cards')).toBeTruthy();
      expect(root.querySelector('qd-abwab-tree')).toBeNull();
    });
  });

  describe('T508/M31 — the archive-toggle header button swaps the main column to the archive view', () => {
    it('renders the archive view and hides the add-root controls while archive=1', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('qd-abwab-archive-view')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-page-add-root"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-page-add-root-ghost"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-page-archive-toggle"]')?.classList).toContain(
        'abwab-page__header-btn--active',
      );
    });
  });

  describe('T509 — restore wiring', () => {
    it('dispatches restoreDoor with the archived door’s current version', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-archive-restore-3"]') as HTMLElement).click();

      expect(restoreDoor).toHaveBeenCalledWith(3, { version: 1 });
    });
  });

  describe('T506 — reorder wiring', () => {
    it('dispatches reorderDoor with the committed position, the door’s current version, and scope=Global (2) in «كل الأبواب»', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      input.value = '2';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

      // No section selected (query params empty) ⇒ the superset is active ⇒ AbwabOrderScope
      // 'global' ⇒ wire value 2 (AbwabReorderScope.Global) — never 'Section' by default.
      expect(reorderDoor).toHaveBeenCalledWith(1, { position: 2, scope: 2, version: 1 });
    });

    it('T405 — dispatches reorderDoor with scope=Section (1) once a section tab is active', () => {
      queryParamMap$.next(convertToParamMap({ section: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-order-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      const input = root.querySelector('[data-testid="abwab-tree-order-input-1"]') as HTMLInputElement;
      input.value = '2';
      input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

      expect(reorderDoor).toHaveBeenCalledWith(1, { position: 2, scope: 1, version: 1 });
    });
  });

  describe('T402/T405 — the order badge itself swaps with the derived scope', () => {
    it('shows globalOrderValue in «كل الأبواب» and orderValue once a section tab is active', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.textContent?.trim()).toBe('9');

      queryParamMap$.next(convertToParamMap({ section: '1' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.textContent?.trim()).toBe('1');
    });
  });

  describe('T507 — search wiring filters the tree via the toolbar input', () => {
    it('hides a non-matching door and auto-expands the matching ancestor', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-toolbar-search"]')!;
      input.value = 'الرسول';
      input.dispatchEvent(new Event('input'));

      expect(router.navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({ queryParams: expect.objectContaining({ q: 'الرسول' }) }),
      );

      queryParamMap$.next(convertToParamMap({ q: 'الرسول' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-row-1"]')).toBeNull();
    });
  });

  describe('T503/T504 — bulk mode and bulk archive', () => {
    it('toggling bulk mode from the side panel shows the checkboxes and the bulk bar', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-tree-checkbox-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-bar"]')).toBeTruthy();
    });

    it('bulk-archive confirm shows the union live-subtree count and dispatches bulkArchiveDoors on confirm', async () => {
      const bulkArchiveDoors = vi.fn().mockReturnValue(of(ok([])));
      getTestBed().resetTestingModule();
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          {
            provide: AbwabApi,
            useValue: { getTree: vi.fn().mockReturnValue(of(ok(TREE))), archiveDoor, bulkArchiveDoors },
          },
          { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } } },
        ],
      }).compileComponents();
      router = TestBed.inject(Router);
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-tree-checkbox-1"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-tree-checkbox-2"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-side-panel-bulk-archive"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-page-bulk-archive-confirm"]')?.textContent).toContain(
        ABWAB_LABELS.archiveConfirm(2),
      );
      (root.querySelector('[data-testid="abwab-page-bulk-archive-confirm-yes"]') as HTMLElement).click();

      expect(bulkArchiveDoors).toHaveBeenCalledWith({
        doors: [
          { doorId: 1, version: 1 },
          { doorId: 2, version: 1 },
        ],
      });
    });
  });

  describe('M22 (Select, A-*) — the side panel stays disabled/unselected while the archive view is active', () => {
    it('shows the no-selection hint and never renders an active door while archive=1', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-side-panel-empty"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeNull();
    });

    // The path the test above cannot reach: entering the archive view *with a live door
    // already selected*. `buildAbwabQueryParams` drops `door` on that transition (§4.4), so
    // the store must follow the URL — otherwise the archive view keeps offering
    // edit/move/archive on a live door that is nowhere on screen.
    it('clears a live selection when the archive toggle drops door from the URL', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeTruthy();

      (root.querySelector('[data-testid="abwab-page-archive-toggle"]') as HTMLElement).click();
      queryParamMap$.next(convertToParamMap({ archive: '1' })); // what the navigate above produces
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-side-panel-empty"]')).toBeTruthy();
      expect(root.querySelector<HTMLButtonElement>('[data-testid="abwab-side-panel-op-archive"]')?.disabled).toBe(
        true,
      );
    });

    it('clears a selection when a section switch drops door from the URL', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeTruthy();

      queryParamMap$.next(convertToParamMap({ section: '1' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeNull();
    });
  });

  describe('overlay state is page-scoped, not application-scoped', () => {
    // AbwabPageOverlaysController is provided by the page, not `providedIn: 'root'` (same
    // rule as words' *-detail.controller.ts). Root scope would survive leaving /abwab, and
    // every dialog renders outside the loading/error guard — so a left-open modal would
    // paint again on re-entry, before any data loads.
    it('does not carry a left-open modal into a freshly created page', () => {
      const first = render();
      const firstRoot = first.nativeElement as HTMLElement;
      (firstRoot.querySelector('[data-testid="abwab-page-add-root"]') as HTMLElement).click();
      first.detectChanges();
      expect(firstRoot.querySelector('[data-testid="abwab-door-modal"]')).toBeTruthy();

      first.destroy();

      const second = render();
      expect((second.nativeElement as HTMLElement).querySelector('[data-testid="abwab-door-modal"]')).toBeNull();
    });
  });

  describe('the contract row actions (abwab-tree-concept.html:114) — ＋ and ⋯', () => {
    it('＋ selects the row and opens the create modal scoped to it as parent', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-add-child-1"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-door-modal-context"]')?.textContent).toContain('العلم بالله');
    });

    it('⋯ opens the same row menu right-click does, without needing right-click', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-more-2"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      const menu = root.querySelector('[data-testid="abwab-page-context-menu"]');
      expect(menu).toBeTruthy();
      expect(Array.from(menu!.querySelectorAll('[role="menuitem"]'))).toHaveLength(5);

      (root.querySelector('[data-testid="abwab-page-ctx-edit"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-door-modal-name"]')).toHaveProperty('value', 'الرسول');
    });
  });

  describe('Search filters the archive tree while archive=1 (M4/M31)', () => {
    it('hides a non-matching archived door and keeps a matching one', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-toolbar-search"]')!;
      input.value = 'مؤرشف';
      input.dispatchEvent(new Event('input'));

      queryParamMap$.next(convertToParamMap({ archive: '1', q: 'مؤرشف' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-archive-row-3"]')).toBeTruthy();

      queryParamMap$.next(convertToParamMap({ archive: '1', q: 'لا يوجد شيء بهذا الاسم' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-archive-row-3"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-page-archive-empty"]')).toBeTruthy();
    });
  });

  describe('T504/T505 — bulk move wiring (distinct from bulk archive)', () => {
    it('opens the move picker for the whole bulk selection and dispatches bulkMoveDoors on confirm', async () => {
      const bulkMoveDoors = vi.fn().mockReturnValue(of(ok([])));
      getTestBed().resetTestingModule();
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          {
            provide: AbwabApi,
            useValue: { getTree: vi.fn().mockReturnValue(of(ok(TREE))), archiveDoor, bulkMoveDoors },
          },
          { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } } },
        ],
      }).compileComponents();
      router = TestBed.inject(Router);
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-tree-checkbox-1"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-tree-checkbox-2"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-side-panel-bulk-move"]') as HTMLElement).click();
      fixture.detectChanges();

      // Two doors reads as the Arabic dual «بابين», not «2 أبواب» — the title is counted
      // through ABWAB_LABELS.movePickerTitleBulk, so assert what a reader actually sees.
      expect(root.querySelector('[data-testid="abwab-move-picker"]')?.textContent).toContain(
        ABWAB_LABELS.movePickerTitleBulk(2),
      );
      (root.querySelector('[data-testid="abwab-move-picker-section-none"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

      expect(bulkMoveDoors).toHaveBeenCalledWith({
        doors: [
          { doorId: 1, version: 1 },
          { doorId: 2, version: 1 },
        ],
        targetParentId: null,
        targetSectionId: null,
      });
    });
  });

  describe('T505 — move picker wiring', () => {
    it('opens the move picker for the selected door and dispatches moveDoor on confirm', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-side-panel-op-move"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-move-picker"]')).toBeTruthy();
      (root.querySelector('[data-testid="abwab-move-picker-section-none"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

      expect(moveDoor).toHaveBeenCalledWith(1, { targetParentId: null, targetSectionId: null, version: 1 });
    });
  });

  describe('T510 — sections modal wiring', () => {
    it('opens the sections modal from the header button and lists the live sections', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-page-manage-sections"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-sections-modal-row-1"]')?.textContent).toContain(
        'اللغة العربية',
      );
    });
  });

  describe('T511 — the row context menu', () => {
    it('right-click opens a menu with exactly edit/add-child/move/relations/archive and nothing else', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-row-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('contextmenu', { bubbles: true, cancelable: true }),
      );
      fixture.detectChanges();

      const menu = root.querySelector('[data-testid="abwab-page-context-menu"]');
      expect(menu).toBeTruthy();
      const items = Array.from(menu!.querySelectorAll('[role="menuitem"]'));
      expect(items).toHaveLength(5);
      expect(menu!.textContent).not.toContain('الحماية');
    });

    it('the edit item opens the door modal prefilled for the right-clicked door', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-tree-row-2"]') as HTMLElement).dispatchEvent(
        new MouseEvent('contextmenu', { bubbles: true, cancelable: true }),
      );
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-page-ctx-edit"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-door-modal-name"]')).toHaveProperty('value', 'الرسول');
    });
  });
  // Its own TestBed: the reveal's five states need a nested door, a second section and a
  // section-less door, and bolting those onto the shared TREE would rewrite the tab-label and
  // stat expectations every other test in this file makes.
  describe('audit item 10 — reveal-in-tree from the relations modal', () => {
    const REVEAL_TREE: AbwabTreeDto = {
      doors: [
        door({ id: 1, name: 'جذر', sectionId: 1, orderValue: 1 }),
        door({ id: 2, name: 'ابن', sectionId: 1, parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'حفيد', sectionId: 1, parentId: 2, orderValue: 1 }),
        door({ id: 4, name: 'باب قسم آخر', sectionId: 2, orderValue: 1 }),
        door({ id: 5, name: 'باب بلا قسم', orderValue: 2 }),
        door({ id: 6, name: 'باب مؤرشف', sectionId: 1, isArchived: true, orderValue: 3 }),
      ],
      sections: [
        { id: 1, name: 'قسم أول', orderValue: 1, version: 1, doorsInScopeCount: 3 },
        { id: 2, name: 'قسم ثانٍ', orderValue: 2, version: 1, doorsInScopeCount: 1 },
      ],
      version: 'v1',
    };

    const params$ = new BehaviorSubject(convertToParamMap({}));
    let revealRouter: Router;

    beforeEach(async () => {
      getTestBed().resetTestingModule();
      params$.next(convertToParamMap({}));
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(of(ok(REVEAL_TREE))) } },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: params$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      }).compileComponents();
      revealRouter = TestBed.inject(Router);
      vi.spyOn(revealRouter, 'navigate').mockResolvedValue(true);
    });

    function renderReveal(params: Record<string, string> = {}) {
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      params$.next(convertToParamMap(params));
      fixture.detectChanges();
      return fixture;
    }

    function reveal(fixture: { componentInstance: unknown }, doorId: number): void {
      (fixture.componentInstance as { onRevealRequested: (id: number) => void }).onRevealRequested(doorId);
    }

    function lastPatch(): Record<string, string | null> {
      const calls = (revealRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls;
      const extras = calls[calls.length - 1][1] as { queryParams: Record<string, string | null> };
      return extras.queryParams;
    }

    it('same scope: patches door only', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 3);

      expect(lastPatch()).toEqual({ door: '3' });
    });

    it('«كل الأبواب»: patches door only, because the superset already contains every target', () => {
      const fixture = renderReveal({});
      reveal(fixture, 4);

      expect(lastPatch()).toEqual({ door: '4' });
    });

    it('other section: one patch carrying the target’s own section beside the explicit door', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 4);

      // The explicit `door` has to survive the scope-invalidation clear `section` triggers —
      // that override is why this is one navigation instead of two.
      expect(lastPatch()).toEqual({ section: '2', door: '4', card: null, modal: null });
    });

    it('section-less target while a section tab is open: clears section rather than keeping it', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 5);

      expect(lastPatch()).toEqual({ section: null, door: '5', card: null, modal: null });
    });

    it('cards view: the patch switches back to the tree, because the item is reveal-in-tree', () => {
      const fixture = renderReveal({ section: '1', view: 'cards' });
      reveal(fixture, 3);

      expect(lastPatch()).toEqual({ view: 'tree', door: '3' });
    });

    it('active search: the patch clears q, so the target cannot land pruned', () => {
      const fixture = renderReveal({ section: '1', q: 'جذر' });
      reveal(fixture, 3);

      expect(lastPatch()).toEqual({ q: null, door: '3' });
    });

    it('archived or unknown target: no navigation, and the announcer says why', () => {
      const fixture = renderReveal({ section: '1' });
      const before = (revealRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls.length;

      reveal(fixture, 6);
      reveal(fixture, 999);
      fixture.detectChanges();

      expect((revealRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls).toHaveLength(before);
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-announcer"]')?.textContent?.trim(),
      ).toBe(ABWAB_LABELS.revealUnavailable);
    });

    it('opens the target’s ancestor chain and marks the row once the param emission lands', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 3);
      // The reveal waits for the URL to come back, exactly as the cross-section and cards
      // cases require — nothing is marked before that.
      fixture.detectChanges();
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('.abwab-tree__row--revealed'),
      ).toBeNull();

      params$.next(convertToParamMap({ section: '1', door: '3' }));
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      // Both ancestors opened, so the depth-2 target is on screen at all.
      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')?.classList.contains('abwab-tree__row--revealed')).toBe(true);
    });

    // Found in the browser, not by a spec: collapse inside the hold window and reveal the
    // same door again, and the naive version re-marks a row that is no longer on screen —
    // setting revealTargetId to the value it already holds is a no-op write, so the seed
    // never recomputes and the collapsed chain stays collapsed.
    it('re-opens the chain when the same door is revealed twice with a collapse in between', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 3);
      params$.next(convertToParamMap({ section: '1', door: '3' }));
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')).toBeNull();

      reveal(fixture, 3);
      params$.next(convertToParamMap({ section: '1', door: '3' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')?.classList.contains('abwab-tree__row--revealed')).toBe(true);
    });

    it('leaves the revealed chain collapsible — the expand is a seed, not a lock', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 3);
      params$.next(convertToParamMap({ section: '1', door: '3' }));
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')).toBeNull();
    });
  });

  describe('audit item 11 — opening a restorable overlay writes modal=<kind>', () => {
    const params$ = new BehaviorSubject(convertToParamMap({}));
    let modalRouter: Router;

    beforeEach(async () => {
      getTestBed().resetTestingModule();
      params$.next(convertToParamMap({}));
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(of(ok(TREE))) } },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: params$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      }).compileComponents();
      modalRouter = TestBed.inject(Router);
      vi.spyOn(modalRouter, 'navigate').mockResolvedValue(true);
    });

    function renderAt(params: Record<string, string> = {}) {
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      params$.next(convertToParamMap(params));
      fixture.detectChanges();
      return fixture;
    }

    function lastPatch(): Record<string, string | null> {
      const calls = (modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls;
      const extras = calls[calls.length - 1][1] as { queryParams: Record<string, string | null> };
      return extras.queryParams;
    }

    function click(root: HTMLElement, testId: string): void {
      (root.querySelector(`[data-testid="${testId}"]`) as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
    }

    it('the door-independent openers write the kind alone, with no door', () => {
      const root = renderAt().nativeElement as HTMLElement;

      click(root, 'abwab-page-add-root');
      expect(lastPatch()).toEqual({ modal: 'create' });

      click(root, 'abwab-page-manage-sections');
      expect(lastPatch()).toEqual({ modal: 'sections' });
    });

    it.each([
      ['abwab-side-panel-op-add-child', 'child'],
      ['abwab-side-panel-op-edit', 'edit'],
      ['abwab-side-panel-op-move', 'move'],
      ['abwab-side-panel-op-relations', 'relations'],
    ])('the side panel op %s folds door and modal into one patch', (testId, kind) => {
      const fixture = renderAt({ door: '1' });
      const root = fixture.nativeElement as HTMLElement;
      click(root, 'abwab-tree-row-1');
      fixture.detectChanges();

      click(root, testId);

      expect(lastPatch()).toEqual({ door: '1', modal: kind });
    });

    it.each([
      ['abwab-page-ctx-edit', 'edit'],
      ['abwab-page-ctx-add-child', 'child'],
      ['abwab-page-ctx-move', 'move'],
      ['abwab-page-ctx-relations', 'relations'],
    ])('the context-menu action %s selects and opens in one patch', (testId, kind) => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-more-2"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      click(root, testId);

      expect(lastPatch()).toEqual({ door: '2', modal: kind });
    });

    it('the tree’s ＋ restates the door beside the child kind', () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-tree-add-child-2');

      expect(lastPatch()).toEqual({ door: '2', modal: 'child' });
    });

    it('the context-menu archive action stays out of the URL — a confirm is re-initiated, never restored', () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="abwab-tree-more-2"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      fixture.detectChanges();

      click(root, 'abwab-page-ctx-archive');
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-page-archive-confirm"]')).toBeTruthy();
      expect(lastPatch()).toEqual({ door: '2' });
    });

    function lastCallExtras(): { queryParams: Record<string, string | null>; replaceUrl?: boolean } {
      const calls = (modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls;
      return calls[calls.length - 1][1] as { queryParams: Record<string, string | null>; replaceUrl?: boolean };
    }

    it('opening pushes, so the closed state it came from stays reachable by Back', () => {
      const root = renderAt().nativeElement as HTMLElement;

      click(root, 'abwab-page-manage-sections');

      expect(lastCallExtras().replaceUrl).toBe(false);
    });

    it('Escape on an open modal retains it as <kind>-closed, by replace', () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      click(root, 'abwab-page-manage-sections');
      fixture.detectChanges();

      (root.querySelector('[data-testid="abwab-sections-modal-close"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: 'sections-closed' }, replaceUrl: true });
      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeNull();
    });

    it('a saved door discards the key instead of retaining it — the form’s work is committed', async () => {
      const createDoor = vi.fn().mockReturnValue(of(ok(door({ id: 9, name: 'باب جديد' }))));
      getTestBed().resetTestingModule();
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          {
            provide: AbwabApi,
            useValue: { getTree: vi.fn().mockReturnValue(of(ok(TREE))), createDoor },
          },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: params$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      }).compileComponents();
      modalRouter = TestBed.inject(Router);
      vi.spyOn(modalRouter, 'navigate').mockResolvedValue(true);

      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-page-add-root');
      fixture.detectChanges();
      const nameInput = root.querySelector('[data-testid="abwab-door-modal-name"]') as HTMLInputElement;
      nameInput.value = 'باب جديد';
      nameInput.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-door-modal-save"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(createDoor).toHaveBeenCalled();
      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: null }, replaceUrl: true });
    });

    it('restore pushes the kind back open; discard replaces the key away', () => {
      const fixture = renderAt({ modal: 'sections-closed' });
      const page = fixture.componentInstance as unknown as {
        onModalRestoreRequested: () => void;
        onModalDiscardRequested: () => void;
      };

      page.onModalRestoreRequested();
      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: 'sections' }, replaceUrl: false });

      page.onModalDiscardRequested();
      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: null }, replaceUrl: true });
    });

    it('restore does nothing when the parsed state is already open or absent', () => {
      const fixture = renderAt({ modal: 'sections' });
      const page = fixture.componentInstance as unknown as { onModalRestoreRequested: () => void };
      const before = (modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls.length;

      page.onModalRestoreRequested();

      expect((modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls).toHaveLength(before);
    });

    it('closing a bulk-opened overlay writes nothing — the URL never held it', () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      click(root, 'abwab-side-panel-bulk-toggle');
      fixture.detectChanges();
      click(root, 'abwab-tree-checkbox-2');
      fixture.detectChanges();
      click(root, 'abwab-side-panel-bulk-move');
      fixture.detectChanges();
      const before = (modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls.length;

      (root.querySelector('[data-testid="abwab-move-picker-cancel"]') as HTMLElement).click();
      fixture.detectChanges();

      expect((modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls).toHaveLength(before);
      expect(root.querySelector('[data-testid="abwab-move-picker"]')).toBeNull();
    });

    it('the bulk overlays never write the key — their subject is bulkSet, which is not URL state', () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      click(root, 'abwab-side-panel-bulk-toggle');
      fixture.detectChanges();
      click(root, 'abwab-tree-checkbox-2');
      fixture.detectChanges();
      const before = (modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls.length;

      click(root, 'abwab-side-panel-bulk-move');
      fixture.detectChanges();
      click(root, 'abwab-side-panel-bulk-relations');
      fixture.detectChanges();

      expect((modalRouter.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls).toHaveLength(before);
      expect(root.querySelector('[data-testid="abwab-move-picker"]')).toBeTruthy();
    });
  });
});
