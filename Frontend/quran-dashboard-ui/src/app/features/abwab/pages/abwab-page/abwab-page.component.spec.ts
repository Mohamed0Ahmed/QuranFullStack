import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';

import { AbwabPageOverlaysController } from '../../state/abwab-page-overlays.controller';

import { AbwabPageComponent } from './abwab-page.component';
import { AbwabApi } from '../../data-access/abwab.api';
import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../../core/auth/write-auth-failure.coordinator';
import { PermissionCode } from '../../../../core/auth/permission-code';

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
    sectionId: 1,
    sectionRetired: false,
    version: 1,
    ...overrides,
  };
}

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

// getTree observes the whole response now, so the facade can read the ETag header beside the
// envelope. Headerless here: no test below sends or stores a validator.
function treeResponse(tree: AbwabTreeDto) {
  return of(new HttpResponse({ body: ok(tree) }));
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

function allowedAccessProviders() {
  return [
    {
      provide: CurrentUserStore,
      useValue: {
        can: () => true,
        canAny: () => true,
        authStateKnown: () => true,
        isAuthenticated: () => true,
        loadState: () => 'ready',
      },
    },
    { provide: WriteAuthFailureCoordinator, useValue: { handle: async () => null } },
  ];
}

function controlledAccessProviders(granted: ReturnType<typeof signal<ReadonlySet<PermissionCode>>>, authenticated = true) {
  return [
    {
      provide: CurrentUserStore,
      useValue: {
        can: (permission: PermissionCode) => granted().has(permission),
        canAny: (permissions: readonly PermissionCode[]) => permissions.some((permission) => granted().has(permission)),
        authStateKnown: () => true,
        isAuthenticated: () => authenticated,
        loadState: () => 'ready',
      },
    },
  ];
}

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
    restoreDoor = vi.fn().mockReturnValue(of(ok(TREE.doors[0])));
    reorderDoor = vi.fn().mockReturnValue(of(ok(TREE.doors[0])));
    moveDoor = vi.fn().mockReturnValue(of(ok(TREE.doors[0])));

    await TestBed.configureTestingModule({
      imports: [AbwabPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: AbwabApi,
          useValue: {
            getTree: vi.fn().mockReturnValue(treeResponse(TREE)),
            archiveDoor,
            restoreDoor,
            reorderDoor,
            moveDoor,
          },
        },
        ...allowedAccessProviders(),
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

  function click(root: HTMLElement, testId: string): void {
    (root.querySelector(`[data-testid="${testId}"]`) as HTMLElement).click();
  }

  function button(root: HTMLElement, testId: string): HTMLButtonElement {
    return root.querySelector(`[data-testid="${testId}"]`) as HTMLButtonElement;
  }

  function selectRow(fixture: ReturnType<typeof render>, id: number): void {
    ((fixture.nativeElement as HTMLElement).querySelector(`[data-testid="abwab-tree-row-${id}"]`) as HTMLElement)
      .dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
  }

  /** Focus moves are queued through `setTimeout(…, 0)` (the page's `focusQueued`) so they land
   * after the render that removed the old target. */
  function flushFocus(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, 0));
  }

  function navigateCallCount(): number {
    return (router.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls.length;
  }

  it('composes the toolbar, tree, side panel and announcer', () => {
    const root = render().nativeElement as HTMLElement;

    expect(root.querySelector('qd-abwab-toolbar')).toBeTruthy();
    expect(root.querySelector('qd-abwab-tree')).toBeTruthy();
    expect(root.querySelector('qd-abwab-side-panel')).toBeTruthy();
    expect(root.querySelector('qd-abwab-announcer')).toBeTruthy();
  });

  it('preserves anonymous reads while stripping only an unauthorized URL-restored write overlay', async () => {
    const granted = signal<ReadonlySet<PermissionCode>>(new Set());
    getTestBed().resetTestingModule();
    queryParamMap$.next(convertToParamMap({ section: '1', q: 'العلم', modal: 'create' }));
    await TestBed.configureTestingModule({
      imports: [AbwabPageComponent],
      providers: [
        provideRouter([]),
        ...controlledAccessProviders(granted, false),
        { provide: WriteAuthFailureCoordinator, useValue: { handle: async () => null } },
        {
          provide: AbwabApi,
          useValue: {
            getTree: vi.fn().mockReturnValue(treeResponse(TREE)),
            getDoorRelations: vi.fn().mockReturnValue(of(ok([]))),
          },
        },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();
    const localRouter = TestBed.inject(Router);
    vi.spyOn(localRouter, 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(AbwabPageComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="abwab-tree-row-1"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-page-templates"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-page-add-root"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-page-manage-sections"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-tree-add-child-1"]')).toBeNull();
    expect(root.querySelector('[data-testid="abwab-tree-order-1"]')?.tagName).toBe('SPAN');
    expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeNull();
    expect(localRouter.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParams: { modal: null }, queryParamsHandling: 'merge', replaceUrl: true }),
    );

    (root.querySelector('[data-testid="abwab-tree-flag-rel-1"]') as HTMLElement).click();
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="abwab-relations-modal"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-relations-modal-add"]')).toBeNull();
  });

  it('refreshes access after a 403, does not retry, and disables the stale archive confirmation', async () => {
    const granted = signal<ReadonlySet<PermissionCode>>(new Set(['abwab.doors.archive']));
    const archiveAttempt = vi.fn().mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 403, error: { isSuccess: false, message: 'ممنوع', data: null } })),
    );
    const refreshAfterForbidden = vi.fn().mockImplementation(async () => {
      granted.set(new Set());
      return { kind: 'forbidden', message: 'ممنوع' };
    });
    getTestBed().resetTestingModule();
    queryParamMap$.next(convertToParamMap({}));
    await TestBed.configureTestingModule({
      imports: [AbwabPageComponent],
      providers: [
        provideRouter([]),
        ...controlledAccessProviders(granted),
        { provide: WriteAuthFailureCoordinator, useValue: { handle: refreshAfterForbidden } },
        {
          provide: AbwabApi,
          useValue: { getTree: vi.fn().mockReturnValue(treeResponse(TREE)), archiveDoor: archiveAttempt },
        },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();
    const localRouter = TestBed.inject(Router);
    vi.spyOn(localRouter, 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(AbwabPageComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    selectRow(fixture, 1);
    click(root, 'abwab-side-panel-op-archive');
    fixture.detectChanges();
    click(root, 'abwab-page-archive-confirm-confirm');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(archiveAttempt).toHaveBeenCalledTimes(1);
    expect(refreshAfterForbidden).toHaveBeenCalledTimes(1);
    expect(root.querySelector('[data-testid="abwab-side-panel-op-archive"]')).toBeNull();
    const confirm = root.querySelector('[data-testid="abwab-page-archive-confirm-confirm"]') as HTMLButtonElement | null;
    expect(confirm === null || confirm.disabled).toBe(true);
  });

  describe('M31 — archived doors are unreachable from the live tree and tabs', () => {
    it('never renders the archived door as a tree row', () => {
      const root = render().nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-tree-row-3"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-tree-row-1"]')).toBeTruthy();
    });

    it('only lists real (non-derived) sections as tabs — an archived door never manufactures a tab', () => {
      const root = render().nativeElement as HTMLElement;
      // The tab's visible label is its leading text node; the count badge (item 19) is a
      // sibling <span> appended after it, so a bare textContent read would include the count.
      const tabLabels = Array.from(root.querySelectorAll('[role="tab"]')).map(
        (el) => el.childNodes[0]?.textContent?.trim(),
      );
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
    (root.querySelector('[data-testid="abwab-page-archive-confirm-confirm"]') as HTMLElement).click();
    fixture.detectChanges();

    expect(archiveDoor).toHaveBeenCalledWith(1, { version: 1 });
    expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeNull();
  });

  describe('the archive confirm holds itself open across the write', () => {
    /** Opens the single-archive confirm from the side-panel op, with `archiveDoor` left pending. */
    function openPendingArchive() {
      const pending = new Subject<ApiResponse<null>>();
      archiveDoor.mockReturnValue(pending);
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      selectRow(fixture, 1);
      click(root, 'abwab-side-panel-op-archive');
      fixture.detectChanges();
      return { fixture, root, pending };
    }

    it('disables both buttons while the write is in flight, then closes on success', () => {
      const { fixture, root, pending } = openPendingArchive();

      click(root, 'abwab-page-archive-confirm-confirm');
      fixture.detectChanges();

      expect(button(root, 'abwab-page-archive-confirm-confirm').disabled).toBe(true);
      expect(button(root, 'abwab-page-archive-confirm-cancel').disabled).toBe(true);
      expect(root.querySelector('[data-testid="abwab-page-archive-confirm"]')).toBeTruthy();

      pending.next(ok(null));
      pending.complete();
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-page-archive-confirm"]')).toBeNull();
    });

    it('a failed write keeps the dialog open and owns the error alone', () => {
      const backendMessage = 'تعذر أرشفة الباب';
      archiveDoor.mockReturnValue(throwError(() => new HttpErrorResponse({
        status: 409,
        error: { isSuccess: false, message: backendMessage, data: null },
      })));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      selectRow(fixture, 1);
      click(root, 'abwab-side-panel-op-archive');
      fixture.detectChanges();
      click(root, 'abwab-page-archive-confirm-confirm');
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-page-archive-confirm"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-page-archive-confirm-error"]')?.textContent).toContain(
        backendMessage,
      );

      // Exactly one visible *error* element, and it is the in-dialog one. The announcer is a
      // role="status" live region, not an error surface, and it deliberately still carries the
      // message: alertdialog content changes are not announced, so suppressing it would leave a
      // screen-reader user with silence on failure.
      expect(root.querySelectorAll('qd-error-state[severity="write"]')).toHaveLength(1);
      expect(root.querySelector('qd-abwab-announcer')?.textContent).toContain(backendMessage);
    });
  });

  describe('focus never drops to <body> when an archive confirm closes', () => {
    it('cancel from the row context menu returns focus to the targeted row', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-tree-more-2');
      fixture.detectChanges();
      click(root, 'abwab-page-ctx-archive');
      fixture.detectChanges();

      click(root, 'abwab-page-archive-confirm-cancel');
      fixture.detectChanges();
      await flushFocus();

      // The ctx menu that opened the dialog is gone in both outcomes, so auto-restore has no
      // target and the page places focus on the tree's roving item itself.
      expect(document.activeElement).toBe(root.querySelector('[data-testid="abwab-tree-row-2"]'));
    });

    it('success moves focus to the roving item once the archived row disappears', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-tree-more-2');
      fixture.detectChanges();
      click(root, 'abwab-page-ctx-archive');
      fixture.detectChanges();
      click(root, 'abwab-page-archive-confirm-confirm');
      fixture.detectChanges();
      await flushFocus();

      expect(document.activeElement).not.toBe(document.body);
      expect(root.querySelector('[data-testid="abwab-tree"]')?.contains(document.activeElement)).toBe(true);
    });

    it('cancel from the side-panel op leaves the still-enabled trigger to the primitive', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      selectRow(fixture, 1);
      click(root, 'abwab-side-panel-op-archive');
      fixture.detectChanges();
      click(root, 'abwab-page-archive-confirm-cancel');
      fixture.detectChanges();
      await flushFocus();

      // Selection survives cancel, so the op button is still there and still enabled, and the
      // page must NOT steal focus away from cdkTrapFocusAutoCapture's restore target. Asserting
      // where focus landed is not available here — auto-capture does not run in jsdom — so this
      // pins the page's own half of the contract: it did not redirect focus into the tree.
      expect(button(root, 'abwab-side-panel-op-archive').disabled).toBe(false);
      expect(root.querySelector('[data-testid="abwab-tree"]')?.contains(document.activeElement)).toBe(false);
    });
  });

  // The restore modal's invoking control is the archive row's restore button, which the
  // refresh removes on success — the mirror of the archive-confirm case above, and the page
  // owes it the same deliberate landing rather than letting focus fall to <body>.
  describe('focus never drops to <body> when the restore modal closes', () => {
    it('success places focus deliberately once the restored row is gone', async () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      fixture.detectChanges();

      click(root, 'abwab-archive-restore-3');
      fixture.detectChanges();
      click(root, 'abwab-door-restore-confirm-confirm');
      fixture.detectChanges();
      await flushFocus();

      expect(restoreDoor).toHaveBeenCalled();
      expect(document.activeElement).not.toBe(document.body);
      expect(root.contains(document.activeElement)).toBe(true);
    });
  });

  it('the two archive confirms cannot be open at once', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    click(root, 'abwab-side-panel-bulk-toggle');
    fixture.detectChanges();
    click(root, 'abwab-tree-checkbox-1');
    fixture.detectChanges();
    click(root, 'abwab-side-panel-bulk-archive');
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="abwab-page-bulk-archive-confirm"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="abwab-page-archive-confirm"]')).toBeNull();
  });

  it('opening and cancelling the sections-delete confirm leaves the URL alone', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    click(root, 'abwab-page-manage-sections');
    fixture.detectChanges();
    const patchesBefore = navigateCallCount();

    click(root, 'abwab-sections-modal-delete-1');
    fixture.detectChanges();
    click(root, 'abwab-sections-modal-delete-confirm-cancel');
    fixture.detectChanges();

    expect(navigateCallCount()).toBe(patchesBefore);
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
    // The button opens the modal rather than writing: a root whose section was retired meanwhile
    // needs a destination, and the backend refuses the write without one.
    it('opens the restore modal, which dispatches restoreDoor with the archived door’s current version', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-archive-restore-3"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(restoreDoor).not.toHaveBeenCalled();
      expect(root.querySelector('[data-testid="abwab-door-restore-modal-name"]')?.textContent).toContain('باب مؤرشف');

      (root.querySelector('[data-testid="abwab-door-restore-confirm-confirm"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(restoreDoor).toHaveBeenCalledWith(3, { version: 1 });
      expect(root.querySelector('[data-testid="abwab-door-restore-modal-name"]')).toBeNull();
    });

    it('closes the restore modal on cancel without writing', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-archive-restore-3"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-door-restore-confirm-cancel"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(restoreDoor).not.toHaveBeenCalled();
      expect(root.querySelector('[data-testid="abwab-door-restore-modal-name"]')).toBeNull();
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

  // Rewritten by ux-slice-l: T507's original decision — search PRUNES the tree — is reversed for
  // the tree only. Hiding rows destroyed the structure the user was reading, and a zero-match
  // query collapsed the whole tree into «لا توجد أبواب بعد», which is a lie about the data. The
  // tree marks matches in place; cards and archive still filter, deliberately, from the same box.
  describe('T507 — search marks matches in the tree via the toolbar input', () => {
    function search(fixture: ReturnType<typeof render>, query: string): HTMLElement {
      const root = fixture.nativeElement as HTMLElement;
      const input = root.querySelector<HTMLInputElement>('[data-testid="abwab-toolbar-search"]')!;
      input.value = query;
      input.dispatchEvent(new Event('input'));
      queryParamMap$.next(convertToParamMap(query === '' ? {} : { q: query }));
      fixture.detectChanges();
      return root;
    }

    it('keeps every row, marks the match, and shows the count', () => {
      const fixture = render();
      const root = search(fixture, 'الرسول');

      expect(router.navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({ queryParams: expect.objectContaining({ q: 'الرسول' }) }),
      );

      const match = root.querySelector('[data-testid="abwab-tree-row-2"]');
      const nonMatch = root.querySelector('[data-testid="abwab-tree-row-1"]');
      expect(match).toBeTruthy();
      // The row that did not match is still there — that is the whole change.
      expect(nonMatch).toBeTruthy();
      expect(match!.classList.contains('abwab-tree__row--match')).toBe(true);
      expect(nonMatch!.classList.contains('abwab-tree__row--match')).toBe(false);

      expect(root.querySelector('[data-testid="abwab-toolbar-search-count"]')?.textContent?.trim()).toBe(
        ABWAB_LABELS.searchMatchCount(1),
      );
    });

    it('a zero-match query leaves the full tree on screen with a zero count, not the empty state', () => {
      const fixture = render();
      const root = search(fixture, 'لا يوجد باب بهذا الاسم');

      expect(root.querySelector('[data-testid="abwab-tree-row-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-page-empty"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-toolbar-search-count"]')?.textContent?.trim()).toBe(
        ABWAB_LABELS.searchMatchCount(0),
      );
    });

    it('clearing the query drops the marks and the count', () => {
      const fixture = render();
      let root = search(fixture, 'الرسول');
      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')!.classList).toContain('abwab-tree__row--match');

      root = search(fixture, '');

      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')!.classList).not.toContain(
        'abwab-tree__row--match',
      );
      expect(root.querySelector('[data-testid="abwab-toolbar-search-count"]')).toBeNull();
    });

    it('cards view still prunes under the same query — the split is per view, not per query', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      queryParamMap$.next(convertToParamMap({ view: 'cards', q: 'الرسول' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-card-2"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-card-1"]')).toBeNull();
    });

    // F-53: the cards branch used to render a bare breadcrumb over a blank grid.
    it('a zero-match cards query states that nothing matched, and keeps the breadcrumb', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;
      queryParamMap$.next(convertToParamMap({ view: 'cards', q: 'لا يوجد باب بهذا الاسم' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-cards-empty"]')?.textContent).toContain(
        ABWAB_LABELS.noSearchMatchesMessage,
      );
      expect(root.querySelector('[data-testid="abwab-cards-crumb"]')).toBeTruthy();
    });

    // F-55: the pruned tree turned a matching root whose children did not match into a card
    // that looked and behaved like a leaf, hiding its real children behind a dead affordance.
    it('leaves a matching root openable in cards, with its real child count, when no descendant matches', async () => {
      const nested: AbwabTreeDto = {
        doors: [
          door({ id: 1, name: 'العلم بالله', orderValue: 1, globalOrderValue: 1 }),
          door({ id: 2, name: 'الرسول', parentId: 1, orderValue: 1 }),
        ],
        sections: TREE.sections,
        version: 'v1',
      };
      getTestBed().resetTestingModule();
      queryParamMap$.next(convertToParamMap({ view: 'cards', q: 'العلم' }));
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          ...allowedAccessProviders(),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(treeResponse(nested)) } },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      }).compileComponents();
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      const card = root.querySelector('[data-testid="abwab-card-1"]')!;
      expect(card.classList.contains('abwab-cards__card--leaf')).toBe(false);
      expect(card.querySelector('.abwab-cards__meta')?.textContent?.trim()).toBe('1');
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
          ...allowedAccessProviders(),
          {
            provide: AbwabApi,
            useValue: { getTree: vi.fn().mockReturnValue(treeResponse(TREE)), archiveDoor, bulkArchiveDoors },
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
      (root.querySelector('[data-testid="abwab-page-bulk-archive-confirm-confirm"]') as HTMLElement).click();

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

    // F-35 (HIGH). A scope change invalidated the single selection but not the bulk set, so bulk
    // archive/move/relations submitted doors that were no longer on screen. The archive toggle
    // already cleared bulk; the section scope did not. Two entry paths reach a section change and
    // both are pinned here, because the defect is the seam between them.
    it('F-35 — a section switch clears the bulk set, not just the single selection', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-tree-checkbox-1"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-tree-checkbox-2"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-count"]')?.textContent?.trim()).toBe('بابان محددان');

      queryParamMap$.next(convertToParamMap({ section: '1' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-count"]')?.textContent?.trim()).toBe('لا أبواب محددة');
      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-bar"]')).toBeTruthy();
    });

    it('F-35 — revealing a door in another section clears the bulk set through the same rule', async () => {
      const TWO_SECTIONS: AbwabTreeDto = {
        doors: [
          door({ id: 1, name: 'العلم بالله', sectionId: 1, orderValue: 1, globalOrderValue: 9 }),
          door({ id: 2, name: 'الرسول', sectionId: 2, orderValue: 1, globalOrderValue: 8 }),
        ],
        sections: [
          { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 1 },
          { id: 2, name: 'الفقه', orderValue: 2, version: 1, doorsInScopeCount: 1 },
        ],
        version: 'v1',
      };
      getTestBed().resetTestingModule();
      await TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          ...allowedAccessProviders(),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(treeResponse(TWO_SECTIONS)), archiveDoor } },
          { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } } },
        ],
      }).compileComponents();
      router = TestBed.inject(Router);
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      queryParamMap$.next(convertToParamMap({ section: '1' }));
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-tree-checkbox-1"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-count"]')?.textContent?.trim()).toBe('باب محدد واحد');

      // Reveal is the entry path a per-slice review could not see: it writes `section` itself when
      // the revealed door lives elsewhere, so the user never touches a section tab.
      (fixture.componentInstance as unknown as { onRevealRequested(doorId: number): void }).onRevealRequested(2);

      const calls = (router.navigate as unknown as { mock: { calls: unknown[][] } }).mock.calls;
      const emitted = (calls[calls.length - 1][1] as { queryParams: Record<string, string | null> }).queryParams;
      expect(emitted['section']).toBe('2');

      // Feed back what navigate actually produced, merged as `queryParamsHandling: 'merge'` does,
      // rather than a hand-written URL that could drift from the handler.
      const merged: Record<string, string> = { section: '1' };
      for (const [key, value] of Object.entries(emitted)) {
        if (value === null) {
          delete merged[key];
        } else {
          merged[key] = value;
        }
      }
      queryParamMap$.next(convertToParamMap(merged));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-side-panel-bulk-count"]')?.textContent?.trim()).toBe('لا أبواب محددة');
    });

    // F-90 (LOW). The in-app archive toggle drops `door` via buildAbwabQueryParams, but a
    // hand-entered or bookmarked `?archive=1&door=<live id>` bypassed that clear: the door=
    // effect selected the door and the side panel offered edit/move/archive/add-child over the
    // archive view. The parse now fails `door` closed to null whenever `archive` is on.
    it('F-90 — a hand-entered archive=1&door=<live id> keeps the side panel unselected and read-only', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1', door: '1' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-side-panel-active-door"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-side-panel-empty"]')).toBeTruthy();
      expect(
        root.querySelector<HTMLButtonElement>('[data-testid="abwab-side-panel-op-archive"]')?.disabled,
      ).toBe(true);
      expect(
        root.querySelector<HTMLButtonElement>('[data-testid="abwab-side-panel-op-edit"]')?.disabled,
      ).toBe(true);
    });
  });

  // F-37 (MEDIUM). `parsePositiveId` validates shape, not existence, so a restored URL carrying
  // a section id that no longer exists left a permanently empty tree, a 0 stat and no active
  // tab. Once the snapshot has landed the page falls back to «كل الأبواب» — mirroring the
  // settle-gated door= effect — and rewrites the URL by replace.
  describe('F-37 — a section id that no longer exists fails closed to «كل الأبواب»', () => {
    it('falls back to the all-doors scope once the snapshot lands and rewrites the URL by replace', () => {
      queryParamMap$.next(convertToParamMap({ section: '999' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-row-1"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-row-2"]')).toBeTruthy();
      expect(
        root.querySelector('[data-testid="abwab-toolbar-tab-all"]')?.getAttribute('aria-selected'),
      ).toBe('true');
      expect(router.navigate).toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: expect.objectContaining({ section: null }),
          replaceUrl: true,
        }),
      );
    });

    // The fallback must also settle AbwabSelectionStore.sectionScope, not just the page signal:
    // if the store were left on the dead id, the replace navigation's own echo would arrive as a
    // 999 → null scope change and wipe any bulk set built after the fallback.
    it('keeps the store scope in sync, so the URL echo of the rewrite cannot wipe a later bulk set', () => {
      queryParamMap$.next(convertToParamMap({ section: '999' }));
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="abwab-side-panel-bulk-toggle"]') as HTMLElement).click();
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-tree-checkbox-1"]') as HTMLElement).click();
      fixture.detectChanges();
      expect(root.querySelector<HTMLInputElement>('[data-testid="abwab-tree-checkbox-1"]')?.checked).toBe(true);

      queryParamMap$.next(convertToParamMap({}));
      fixture.detectChanges();

      expect(root.querySelector<HTMLInputElement>('[data-testid="abwab-tree-checkbox-1"]')?.checked).toBe(true);
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

    // F-54: «لا توجد أبواب مؤرشفة.» under a zero-match query is a lie about the data — the
    // archive is not empty, the query simply matched nothing in it.
    it('says nothing matched, not that the archive is empty, when a query prunes every archived door', () => {
      queryParamMap$.next(convertToParamMap({ archive: '1', q: 'لا يوجد شيء بهذا الاسم' }));
      const root = render().nativeElement as HTMLElement;
      const state = root.querySelector('[data-testid="abwab-page-archive-empty"]');

      expect(state?.textContent).toContain(ABWAB_LABELS.archiveNoSearchMatchesMessage);
      expect(state?.textContent).not.toContain(ABWAB_LABELS.archiveEmptyMessage);
    });

    it('still says the archive is empty when it genuinely holds nothing', () => {
      const noArchive: AbwabTreeDto = { doors: [TREE.doors[0]], sections: TREE.sections, version: 'v1' };
      getTestBed().resetTestingModule();
      queryParamMap$.next(convertToParamMap({ archive: '1' }));
      TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          ...allowedAccessProviders(),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(treeResponse(noArchive)) } },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      });
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-page-archive-empty"]')?.textContent).toContain(
        ABWAB_LABELS.archiveEmptyMessage,
      );
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
          ...allowedAccessProviders(),
          {
            provide: AbwabApi,
            useValue: { getTree: vi.fn().mockReturnValue(treeResponse(TREE)), archiveDoor, bulkMoveDoors },
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
      // Both selected doors live in section 1, so the strip opens with that section active.
      (root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

      expect(bulkMoveDoors).toHaveBeenCalledWith({
        doors: [
          { doorId: 1, version: 1 },
          { doorId: 2, version: 1 },
        ],
        targetParentId: null,
        targetSectionId: 1,
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
      // A single move already knows its section, so the strip opens with it active.
      (root.querySelector('[data-testid="abwab-move-picker-dest-asmain"]') as HTMLElement).click();
      (root.querySelector('[data-testid="abwab-move-picker-confirm"]') as HTMLElement).click();

      expect(moveDoor).toHaveBeenCalledWith(1, { targetParentId: null, targetSectionId: 1, version: 1 });
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
  // Its own TestBed: the reveal's states need a nested door and a second section, and bolting
  // those onto the shared TREE would rewrite the tab-label and stat expectations every other test
  // in this file makes.
  describe('audit item 10 — reveal-in-tree from the relations modal', () => {
    const REVEAL_TREE: AbwabTreeDto = {
      doors: [
        door({ id: 1, name: 'جذر', sectionId: 1, orderValue: 1 }),
        door({ id: 2, name: 'ابن', sectionId: 1, parentId: 1, orderValue: 1 }),
        door({ id: 3, name: 'حفيد', sectionId: 1, parentId: 2, orderValue: 1 }),
        door({ id: 4, name: 'باب قسم آخر', sectionId: 2, orderValue: 1 }),
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
          ...allowedAccessProviders(),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(treeResponse(REVEAL_TREE)) } },
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

    // Tested here because this describe owns the only nested fixture (1 → 2 → 3): search
    // auto-expansion is DERIVED, not seeded — the page passes `searchResult().autoExpandedIds`
    // to the tree's `searchExpandedIds` input, replaced wholesale per query, and the tree unions
    // it with the manual set at render time. The move-picker contract, now on the main tree.
    describe('search derives the match’s ancestors open (supersedes ux-slice-l’s surviving-seed pin)', () => {
      const rowsPresent = (fixture: { nativeElement: unknown }, ids: readonly number[]) =>
        ids.map((id) => !!(fixture.nativeElement as HTMLElement).querySelector(`[data-testid="abwab-tree-row-${id}"]`));

      const toggleRootChevron = (fixture: { nativeElement: unknown }) =>
        (
          (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-tree-chevron-1"]') as HTMLElement
        ).dispatchEvent(new MouseEvent('click', { bubbles: true }));

      it('clearing returns expansion to exactly what the user opened by hand — no more, no less', () => {
        // Collapsed to start: only the root is on screen.
        const fixture = renderReveal({ section: '1' });
        expect(rowsPresent(fixture, [1, 2, 3])).toEqual([true, false, false]);

        // «حفيد» is the grandchild — its ancestors 1 and 2 derive open.
        params$.next(convertToParamMap({ section: '1', q: 'حفيد' }));
        fixture.detectChanges();
        expect(rowsPresent(fixture, [1, 2, 3])).toEqual([true, true, true]);

        // Clearing the query empties the derived set: the user never opened anything by hand.
        params$.next(convertToParamMap({ section: '1' }));
        fixture.detectChanges();
        expect(rowsPresent(fixture, [1, 2, 3])).toEqual([true, false, false]);

        // The "no less" half: a branch opened by hand survives a search-and-clear cycle
        // while the search-derived branch below it still closes.
        toggleRootChevron(fixture);
        fixture.detectChanges();
        params$.next(convertToParamMap({ section: '1', q: 'حفيد' }));
        fixture.detectChanges();
        expect(rowsPresent(fixture, [1, 2, 3])).toEqual([true, true, true]);
        params$.next(convertToParamMap({ section: '1' }));
        fixture.detectChanges();
        expect(rowsPresent(fixture, [1, 2, 3])).toEqual([true, true, false]);

        // Still no force anywhere: the hand-opened branch collapses on its chevron.
        toggleRootChevron(fixture);
        fixture.detectChanges();
        expect(rowsPresent(fixture, [1, 2, 3])).toEqual([true, false, false]);
      });
    });

    it('same scope: patches door only', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 3);

      expect(lastPatch()).toEqual({ door: '3', modal: null });
    });

    it('«كل الأبواب»: patches door only, because the superset already contains every target', () => {
      const fixture = renderReveal({});
      reveal(fixture, 4);

      expect(lastPatch()).toEqual({ door: '4', modal: null });
    });

    it('other section: one patch carrying the target’s own section beside the explicit door', () => {
      const fixture = renderReveal({ section: '1' });
      reveal(fixture, 4);

      // The explicit `door` has to survive the scope-invalidation clear `section` triggers —
      // that override is why this is one navigation instead of two.
      expect(lastPatch()).toEqual({ section: '2', door: '4', card: null, modal: null });
    });

    it('cards view: the patch switches back to the tree, because the item is reveal-in-tree', () => {
      const fixture = renderReveal({ section: '1', view: 'cards' });
      reveal(fixture, 3);

      expect(lastPatch()).toEqual({ view: 'tree', door: '3', modal: null });
    });

    // Slice D cleared `q` here because a filtering tree could leave the reveal's target pruned.
    // ux-slice-l removed the pruning, so the premise is gone and the user's query survives —
    // throwing it away would now be a second, unasked-for action (user decision, 2026-08-02).
    it('active search: the patch carries no q term, so the search survives the reveal', () => {
      const fixture = renderReveal({ section: '1', q: 'جذر' });
      reveal(fixture, 3);

      expect(lastPatch()).toEqual({ door: '3', modal: null });
      expect(lastPatch()).not.toHaveProperty('q');
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
          ...allowedAccessProviders(),
          {
            provide: AbwabApi,
            useValue: {
              getTree: vi.fn().mockReturnValue(treeResponse(TREE)),
              getDoorRelations: vi.fn().mockReturnValue(of(ok([]))),
            },
          },
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
          ...allowedAccessProviders(),
          {
            provide: AbwabApi,
            useValue: { getTree: vi.fn().mockReturnValue(treeResponse(TREE)), createDoor },
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
      // «كل الأبواب» has no tab to derive a section from, so the shell asks for one before it writes.
      const sectionSelect = root.querySelector('[data-testid="abwab-door-modal-section-select"]') as HTMLSelectElement;
      sectionSelect.value = '1';
      sectionSelect.dispatchEvent(new Event('change', { bubbles: true }));
      fixture.detectChanges();
      (root.querySelector('[data-testid="abwab-door-modal-save"]') as HTMLElement).click();
      fixture.detectChanges();

      expect(createDoor).toHaveBeenCalled();
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

    it('a deep link opens the door-dependent kind only once the snapshot binds its subject', () => {
      const fixture = TestBed.createComponent(AbwabPageComponent);
      params$.next(convertToParamMap({ door: '1', modal: 'edit' }));
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-door-modal-name"]')).toHaveProperty(
        'value',
        'العلم بالله',
      );
    });

    it('a door-independent kind deep-links without any door at all', () => {
      const root = renderAt({ modal: 'sections' }).nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeTruthy();
    });

    it.each([
      ['an archived door', '3'],
      ['a door that is not in the snapshot', '999'],
    ])('leaves the key inert for %s — nothing opens and no restore control renders', (_case, doorId) => {
      const fixture = renderAt({ door: doorId, modal: 'edit' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();
    });

    it('an emission carrying -closed closes the open overlay — the Back path', () => {
      const fixture = renderAt({ modal: 'sections' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeTruthy();

      params$.next(convertToParamMap({ modal: 'sections-closed' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeNull();
    });

    it('a URL-driven close discards an unsaved draft without raising the confirm', () => {
      const fixture = renderAt({ door: '1', modal: 'edit' });
      const root = fixture.nativeElement as HTMLElement;
      const nameInput = root.querySelector('[data-testid="abwab-door-modal-name"]') as HTMLInputElement;
      nameInput.value = 'مسودة لم تُحفظ';
      nameInput.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      // Cancelling by gesture raises the dirty confirm; Back does not. The URL is the single
      // source of truth, so a URL that says the overlay is closed closes it — and restoring
      // returns a pristine overlay rather than the draft (README, «Restoring reopens the
      // overlay, not a draft»).
      params$.next(convertToParamMap({ door: '1', modal: 'edit-closed' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-door-modal-discard-confirm"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeTruthy();
    });

    it('an emission that moves door= under an unchanged kind rebinds to the new subject', () => {
      const fixture = renderAt({ door: '1', modal: 'edit' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-door-modal-name"]')).toHaveProperty('value', 'العلم بالله');

      params$.next(convertToParamMap({ door: '2', modal: 'edit' }));
      fixture.detectChanges();

      // The subject is tracked beside the kind, so this reads as a different overlay. Comparing
      // kinds alone would leave the editor bound to door 1 while the URL named door 2.
      expect(root.querySelector('[data-testid="abwab-door-modal-name"]')).toHaveProperty('value', 'الرسول');
    });

    it('a parsed key does not hijack a bulk-opened overlay into single-subject mode', () => {
      const fixture = renderAt({ door: '1' });
      const root = fixture.nativeElement as HTMLElement;
      click(root, 'abwab-side-panel-bulk-toggle');
      fixture.detectChanges();
      click(root, 'abwab-tree-checkbox-1');
      fixture.detectChanges();
      click(root, 'abwab-tree-checkbox-2');
      fixture.detectChanges();
      click(root, 'abwab-side-panel-bulk-move');
      fixture.detectChanges();
      // The picker's heading is `qd-modal-shell`'s now, so the title is read from the shell's
      // published `-title` test id rather than the `h3` the picker used to render itself. The
      // subject this pins — the bulk title survives a single-subject key — is unchanged.
      const titleOf = () => root.querySelector('[data-testid="abwab-move-picker-title"]')?.textContent?.trim();
      const bulkTitle = titleOf();
      expect(bulkTitle).toBe(ABWAB_LABELS.movePickerTitleBulk(2));

      params$.next(convertToParamMap({ door: '1', modal: 'move' }));
      fixture.detectChanges();

      expect(titleOf()).toBe(bulkTitle);
    });

    it('the echo of a gesture’s own patch is a no-op, not a second open', () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      click(root, 'abwab-tree-row-1');
      fixture.detectChanges();
      click(root, 'abwab-side-panel-op-edit');
      fixture.detectChanges();

      const nameInput = root.querySelector('[data-testid="abwab-door-modal-name"]') as HTMLInputElement;
      nameInput.value = 'مسودة';
      nameInput.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      // The emission that lands the gesture's own patch, then an unrelated one: neither may
      // re-run the opener, which would reset the field back to the snapshot's name.
      params$.next(convertToParamMap({ door: '1', modal: 'edit' }));
      fixture.detectChanges();
      params$.next(convertToParamMap({ door: '1', modal: 'edit', q: 'بحث' }));
      fixture.detectChanges();

      expect((root.querySelector('[data-testid="abwab-door-modal-name"]') as HTMLInputElement).value).toBe('مسودة');
    });

    it('switching to another kind closes the first and opens the second', () => {
      const fixture = renderAt({ modal: 'sections' });
      const root = fixture.nativeElement as HTMLElement;

      params$.next(convertToParamMap({ modal: 'create' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-sections-modal"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeTruthy();
    });

    it('renders the restore control only for a retained state, naming the overlay it holds', () => {
      const fixture = renderAt({ door: '1', modal: 'edit-closed' });
      const root = fixture.nativeElement as HTMLElement;

      const restore = root.querySelector('[data-testid="abwab-page-modal-restore"]');
      expect(restore?.textContent?.trim()).toBe(
        ABWAB_LABELS.modalRestoreLabel(ABWAB_LABELS.modalKindNames['edit']),
      );
      expect(
        root.querySelector('[data-testid="abwab-page-modal-discard"]')?.getAttribute('aria-label'),
      ).toBe(ABWAB_LABELS.modalDiscardAriaLabel(ABWAB_LABELS.modalKindNames['edit']));
    });

    it('shows no restore control while the overlay is open, or when the key is absent', () => {
      const fixture = renderAt({ door: '1', modal: 'edit' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();

      params$.next(convertToParamMap({}));
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();
    });

    it('the restore control round-trips: clicking it reopens the overlay through the URL', () => {
      const fixture = renderAt({ door: '1', modal: 'edit-closed' });
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-page-modal-restore');
      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: 'edit' }, replaceUrl: false });

      params$.next(convertToParamMap({ door: '1', modal: 'edit' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();
    });

    it('the X clears the key, and the control goes with it', () => {
      const fixture = renderAt({ door: '1', modal: 'edit-closed' });
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-page-modal-discard');
      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: null }, replaceUrl: true });

      params$.next(convertToParamMap({ door: '1' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();
    });

    it('focus lands on the restore control after a retaining close', async () => {
      const fixture = renderAt();
      const root = fixture.nativeElement as HTMLElement;
      document.body.appendChild(root);
      click(root, 'abwab-page-manage-sections');
      fixture.detectChanges();

      (root.querySelector('[data-testid="abwab-sections-modal-close"]') as HTMLElement).click();
      params$.next(convertToParamMap({ modal: 'sections-closed' }));
      fixture.detectChanges();
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(document.activeElement).toBe(root.querySelector('[data-testid="abwab-page-modal-restore"]'));
      root.remove();
    });

    it('focus moves to the next header control after the discard X removes itself', async () => {
      const fixture = renderAt({ door: '1', modal: 'edit-closed' });
      const root = fixture.nativeElement as HTMLElement;
      document.body.appendChild(root);

      click(root, 'abwab-page-modal-discard');
      params$.next(convertToParamMap({ door: '1' }));
      fixture.detectChanges();
      await new Promise((resolve) => setTimeout(resolve, 0));

      // Without the handoff the activated control is gone and focus falls to `<body>`.
      expect(document.activeElement).toBe(root.querySelector('[data-testid="abwab-page-archive-toggle"]'));
      root.remove();
    });

    it('a reveal onto a dead target still clears the key it closed the modal for', () => {
      const fixture = renderAt({ door: '1', modal: 'relations' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-relations-modal"]')).toBeTruthy();

      // The guard branch navigates nowhere on its own, so if the close did not carry the key
      // the modal would shut with `modal=relations` stranded in the URL and no emission left
      // to reconcile it — the overlay would be unreachable from then on.
      (fixture.componentInstance as unknown as { onRevealRequested: (id: number) => void }).onRevealRequested(3);
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-relations-modal"]')).toBeNull();
      expect(lastCallExtras()).toMatchObject({ queryParams: { modal: null }, replaceUrl: true });

      // And the page is genuinely reusable afterwards: reopening works.
      params$.next(convertToParamMap({ door: '1' }));
      fixture.detectChanges();
      click(root, 'abwab-side-panel-op-relations');
      expect(lastCallExtras()).toMatchObject({ queryParams: { door: '1', modal: 'relations' } });
    });

    // Rewritten by ux-slice-l. The reveal used to DISCARD the key: `door=` was being pointed at
    // the target, and a plain `relations-closed` follows `door=`, so the restore control would
    // have reopened the target's relations while the user expected the source's. The key now
    // carries the diverged subject itself, so the state survives instead of being thrown away.
    it('a reveal retains relations-<sourceId>-closed in its single patch', () => {
      const fixture = renderAt({ door: '1', modal: 'relations' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-relations-modal"]')).toBeTruthy();

      (fixture.componentInstance as unknown as { onRevealRequested: (id: number) => void }).onRevealRequested(2);

      // Still ONE patch, and the id in it is the SOURCE's, not the target's.
      expect(lastPatch()).toEqual({ door: '2', modal: 'relations-1-closed' });
    });

    it('the restore control renders for a carried subject, naming the source door', () => {
      const fixture = renderAt({ door: '2', modal: 'relations-1-closed' });
      const root = fixture.nativeElement as HTMLElement;

      // «استعادة علاقات «العلم بالله»» — door 1, the source, even though door= is 2. The plain
      // form would have named the kind only, leaving the user to guess whose relations wait.
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')?.textContent?.trim()).toBe(
        ABWAB_LABELS.modalRestoreLabel(ABWAB_LABELS.relationsOfDoorKindName('العلم بالله')),
      );
      expect(root.querySelector('[data-testid="abwab-page-modal-discard"]')?.getAttribute('aria-label')).toBe(
        ABWAB_LABELS.modalDiscardAriaLabel(ABWAB_LABELS.relationsOfDoorKindName('العلم بالله')),
      );
    });

    it('restoring writes door=<source> and the bare open key in one patch', () => {
      const fixture = renderAt({ door: '2', modal: 'relations-1-closed' });
      const root = fixture.nativeElement as HTMLElement;

      click(root, 'abwab-page-modal-restore');

      // One push, and the open state carries no id — its subject is `door=` again.
      expect(lastPatch()).toEqual({ door: '1', modal: 'relations' });
    });

    it('a carried subject is pinned: selecting another door does not move it', () => {
      const fixture = renderAt({ door: '2', modal: 'relations-1-closed' });
      const root = fixture.nativeElement as HTMLElement;

      params$.next(convertToParamMap({ door: '3', modal: 'relations-1-closed' }));
      fixture.detectChanges();

      // Unlike a plain `-closed`, which follows `door=`, this one still names door 1.
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')?.textContent?.trim()).toBe(
        ABWAB_LABELS.modalRestoreLabel(ABWAB_LABELS.relationsOfDoorKindName('العلم بالله')),
      );
    });

    it('renders no control when the carried door is archived or absent', () => {
      // Door 3 in this fixture is archived; 99 does not exist. Both leave the key inert —
      // no control, no rewrite — the same outcome a dead `door=` already produces.
      for (const deadId of ['3', '99']) {
        const fixture = renderAt({ door: '1', modal: `relations-${deadId}-closed` });
        expect(
          (fixture.nativeElement as HTMLElement).querySelector('[data-testid="abwab-page-modal-restore"]'),
        ).toBeNull();
      }
    });

    // The single-retained-state rule is a DECISION, not an accident of the key being
    // single-valued — so both orders are pinned here rather than left to archaeology.
    describe('the retained key holds one state, and the next writer wins', () => {
      it('opening another modal overwrites a reveal-retained key, control and all', () => {
        const fixture = renderAt({ door: '2', modal: 'relations-1-closed' });
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeTruthy();

        click(root, 'abwab-page-manage-sections');
        expect(lastPatch()).toMatchObject({ modal: 'sections' });

        params$.next(convertToParamMap({ door: '2', modal: 'sections' }));
        fixture.detectChanges();
        expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();

        // Closing the new modal retains ITS plain key; the relations state does not come back.
        params$.next(convertToParamMap({ door: '2', modal: 'sections-closed' }));
        fixture.detectChanges();
        expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')?.textContent?.trim()).toBe(
          ABWAB_LABELS.modalRestoreLabel(ABWAB_LABELS.modalKindNames['sections']),
        );
      });

      it('opening relations fresh on another door overwrites the carried key', () => {
        const fixture = renderAt({ door: '2', modal: 'relations-1-closed' });
        const root = fixture.nativeElement as HTMLElement;

        click(root, 'abwab-side-panel-op-relations');

        // `door=2` with a bare key — the carried id is gone, and closing this one would retain
        // a plain `relations-closed` whose subject is door 2.
        expect(lastPatch()).toMatchObject({ door: '2', modal: 'relations' });
      });
    });

    // Back after a reveal was designed but pinned by nothing: the previous history entry is
    // `modal=relations&door=<source>`, and the reconcile machinery has to reopen the modal on
    // the source from that emission alone.
    it('Back after a reveal reopens the modal on the source door', () => {
      const fixture = renderAt({ door: '1', modal: 'relations' });
      const root = fixture.nativeElement as HTMLElement;

      (fixture.componentInstance as unknown as { onRevealRequested: (id: number) => void }).onRevealRequested(2);
      params$.next(convertToParamMap({ door: '2', modal: 'relations-1-closed' }));
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-relations-modal"]')).toBeNull();

      // Back — the emission the browser produces for the previous entry.
      params$.next(convertToParamMap({ door: '1', modal: 'relations' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-relations-modal"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-relations-modal"]')?.textContent).toContain('العلم بالله');
    });

    it('switching section clears the key, and the overlay closes with it', () => {
      const fixture = renderAt({ section: '1', door: '1', modal: 'edit' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeTruthy();

      click(root, 'abwab-toolbar-tab-all');
      expect(lastPatch()).toMatchObject({ section: null, door: null, card: null, modal: null });

      params$.next(convertToParamMap({}));
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();
    });

    it('turning the archive view on clears a retained key too', () => {
      const fixture = renderAt({ door: '1', modal: 'edit-closed' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeTruthy();

      click(root, 'abwab-page-archive-toggle');

      expect(lastPatch()).toMatchObject({ archive: '1', door: null, card: null, modal: null });
    });

    it('archive success orphans a retained edit into inertness rather than a broken restore', () => {
      const fixture = renderAt({ door: '1', modal: 'edit-closed' });
      const root = fixture.nativeElement as HTMLElement;
      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeTruthy();

      // The archive-success callback clears `door=`; the key itself is never rewritten
      // (fail-closed parse, no canonicalization), so it simply stops parsing.
      params$.next(convertToParamMap({ modal: 'edit-closed' }));
      fixture.detectChanges();

      expect(root.querySelector('[data-testid="abwab-page-modal-restore"]')).toBeNull();
      expect(root.querySelector('[data-testid="abwab-door-modal"]')).toBeNull();
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

  describe('the tree badge header follows the tree, not the page (slice J)', () => {
    // The page owns the loading and empty branches and mounts <qd-abwab-tree> only in the
    // populated one, so the header cannot appear over a skeleton or an empty state.
    it('is absent while the tree skeleton is showing', () => {
      const pending = new Subject<HttpResponse<ApiResponse<AbwabTreeDto>>>();
      getTestBed().resetTestingModule();
      TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          ...allowedAccessProviders(),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(pending) } },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      });
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('.abwab-page__tree-skeleton')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-header"]')).toBeNull();
    });

    it('is absent in the empty state', () => {
      const empty: AbwabTreeDto = { doors: [], sections: TREE.sections, version: 'v1' };
      getTestBed().resetTestingModule();
      TestBed.configureTestingModule({
        imports: [AbwabPageComponent],
        providers: [
          provideRouter([]),
          ...allowedAccessProviders(),
          { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(treeResponse(empty)) } },
          {
            provide: ActivatedRoute,
            useValue: { queryParamMap: queryParamMap$, snapshot: { queryParamMap: convertToParamMap({}) } },
          },
        ],
      });
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
      const fixture = TestBed.createComponent(AbwabPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-page-empty"]')).toBeTruthy();
      expect(root.querySelector('[data-testid="abwab-tree-header"]')).toBeNull();
    });

    it('is present once rows render', () => {
      const root = render().nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="abwab-tree-header"]')).toBeTruthy();
    });
  });

  it('restoreAncestors keeps one identity while no restore is open', () => {
    const fixture = render();
    const overlays = fixture.debugElement.injector.get(AbwabPageOverlaysController);

    const first = overlays.restoreAncestors();
    // A rebuild hands the tree a whole new node graph; a fresh `[]` here would mark the OnPush
    // restore modal dirty on every one of them.
    queryParamMap$.next(convertToParamMap({ q: 'الرسول' }));
    fixture.detectChanges();

    expect(overlays.restoreAncestors()).toBe(first);
  });

  // Phase 8 / D01–D02: the route declares exactly one named page intent and the shell is the only
  // inline-gutter owner. The rail is the named 18rem Abwab size, never a local inline-size.
  describe('Golden page composition', () => {
    it('renders one named page intent and no second gutter frame', () => {
      const fixture = render();
      const root = fixture.nativeElement as HTMLElement;

      const shells = root.querySelectorAll('.qd-page-shell');
      expect(shells).toHaveLength(1);
      expect(shells[0].classList.contains('qd-page-shell--full-data')).toBe(true);
      expect(root.querySelectorAll('.qd-container, .qd-page-frame, .qd-explorer-frame')).toHaveLength(0);
    });

    it('sizes the side rail from the named 18rem rail token', () => {
      const fixture = render();
      const rail = (fixture.nativeElement as HTMLElement).querySelector('.abwab-page__side')!;

      expect(rail.classList.contains('qd-page-rail')).toBe(true);
      expect(rail.classList.contains('qd-page-rail--m')).toBe(true);
    });

    // F12: abwab reaches the five owners directly. The compatibility adapter has no abwab
    // consumer left, which is what Phase 11 needs before it can delete it.
    it('mounts no qd-state adapter anywhere on the page', () => {
      const fixture = render();
      expect((fixture.nativeElement as HTMLElement).querySelectorAll('qd-state')).toHaveLength(0);
    });
  });
});
