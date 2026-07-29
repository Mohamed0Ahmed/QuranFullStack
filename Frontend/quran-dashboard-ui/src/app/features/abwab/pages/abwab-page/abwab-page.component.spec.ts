import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { AbwabPageComponent } from './abwab-page.component';
import { AbwabApi } from '../../data-access/abwab.api';
import { AbwabTreeDoorDto } from '../../../../core/api/generated/models/abwab-tree-door-dto';
import { AbwabTreeDto } from '../../../../core/api/generated/models/abwab-tree-dto';
import { ApiResponse } from '../../../../core/data-access/api-response.model';

function door(overrides: Partial<AbwabTreeDoorDto> & { id: number; name: string }): AbwabTreeDoorDto {
  return {
    aliases: [],
    description: null,
    directChildCount: 0,
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
    door({ id: 1, name: 'العلم بالله', sectionId: 1, orderValue: 1 }),
    door({ id: 2, name: 'الرسول', sectionId: 1, orderValue: 2 }),
    door({ id: 3, name: 'باب مؤرشف', isArchived: true, orderValue: 3 }),
  ],
  sections: [{ id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 2 }],
  version: 'v1',
};

describe('AbwabPageComponent', () => {
  let router: Router;
  let archiveDoor: ReturnType<typeof vi.fn>;
  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();
    queryParamMap$.next(convertToParamMap({}));
    archiveDoor = vi.fn().mockReturnValue(of(ok(null)));

    await TestBed.configureTestingModule({
      imports: [AbwabPageComponent],
      providers: [
        provideRouter([]),
        { provide: AbwabApi, useValue: { getTree: vi.fn().mockReturnValue(of(ok(TREE))), archiveDoor } },
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

  describe('M31 (partial — cards/search deferred to phase 5) — archived doors are unreachable from the live tree and tabs', () => {
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
});
