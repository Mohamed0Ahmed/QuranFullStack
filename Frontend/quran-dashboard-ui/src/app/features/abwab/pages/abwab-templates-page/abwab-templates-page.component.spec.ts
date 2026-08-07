import { describe, expect, it, beforeEach, vi } from 'vitest';
import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';

import { AbwabTemplatesPageComponent } from './abwab-templates-page.component';
import { AbwabTemplatesApi } from '../../data-access/abwab-templates.api';
import { AbwabApi } from '../../data-access/abwab.api';
import { AbwabTemplateDto } from '../../../../core/api/generated/models/abwab-template-dto';
import { AbwabTemplateSummaryDto } from '../../../../core/api/generated/models/abwab-template-summary-dto';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { ABWAB_WRITE_PERMISSIONS } from '../../state/abwab-permissions.controller';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { WriteAuthFailureCoordinator } from '../../../../core/auth/write-auth-failure.coordinator';
import { PERMISSION_CODES, PermissionCode } from '../../../../core/auth/permission-code';

const SUMMARY: AbwabTemplateSummaryDto = { id: 1, name: 'قالب الأبواب', nodeCount: 2 };

const TEMPLATE: AbwabTemplateDto = {
  id: 1,
  name: 'قالب الأبواب',
  nodes: [
    { id: 10, parentNodeId: null, name: 'قالب الأبواب', description: null, representativeAyahText: null, aliases: [], orderValue: 1 },
    { id: 11, parentNodeId: 10, name: 'العلم بالله', description: null, representativeAyahText: null, aliases: [], orderValue: 1 },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

function headerless<T>(envelope: ApiResponse<T>) {
  return of(new HttpResponse({ body: envelope }));
}

// cancelTemplateDelete() is protected and the error signal it clears is only ever painted inside the
// open dialog, so the clearing assertion has to read the signal directly.
interface TemplateDeleteInternals {
  cancelTemplateDelete(): void;
  templateDeleteError(): string | null;
  templateDeleteBusy(): boolean;
}

function internals(fixture: ComponentFixture<AbwabTemplatesPageComponent>): TemplateDeleteInternals {
  return fixture.componentInstance as unknown as TemplateDeleteInternals;
}

function element(fixture: ComponentFixture<AbwabTemplatesPageComponent>, testId: string): HTMLElement | null {
  return (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`);
}

function click(fixture: ComponentFixture<AbwabTemplatesPageComponent>, testId: string): void {
  (element(fixture, testId) as HTMLElement).click();
  fixture.detectChanges();
}

describe('AbwabTemplatesPageComponent', () => {
  let getTemplates: ReturnType<typeof vi.fn>;
  let deleteTemplate: ReturnType<typeof vi.fn>;

  async function render(permissions: readonly PermissionCode[] = PERMISSION_CODES): Promise<ComponentFixture<AbwabTemplatesPageComponent>> {
    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [AbwabTemplatesPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: AbwabTemplatesApi,
          useValue: {
            getTemplates,
            getTemplate: () => headerless(ok(TEMPLATE)),
            deleteTemplate,
          },
        },
        {
          provide: CurrentUserStore,
          useValue: {
            can: (permission: PermissionCode) => permissions.includes(permission),
            canAny: (codes: readonly PermissionCode[]) => codes.some((permission) => permissions.includes(permission)),
            authStateKnown: () => true,
            isAuthenticated: () => true,
            loadState: () => 'ready',
          },
        },
        { provide: WriteAuthFailureCoordinator, useValue: { handle: async () => null } },
        { provide: AbwabApi, useValue: { getTree: () => of(new HttpResponse({ body: ok(null) })) } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AbwabTemplatesPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    getTemplates = vi.fn().mockReturnValue(headerless(ok([SUMMARY])));
    deleteTemplate = vi.fn().mockReturnValue(of(ok(null)));
  });

  describe('templates-list load failure', () => {
    it('offers a retry that re-issues the list read', async () => {
      getTemplates = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
      const fixture = await render();

      const errorState = element(fixture, 'abwab-templates-page-list-error');
      expect(errorState?.textContent).toContain(ABWAB_LABELS.templatesLoadError);

      const retry = errorState?.querySelector('[data-testid="qd-state-action"]') as HTMLButtonElement;
      expect(retry.textContent?.trim()).toBe(ABWAB_LABELS.retryButton);

      expect(getTemplates).toHaveBeenCalledTimes(1);
      retry.click();
      fixture.detectChanges();
      expect(getTemplates).toHaveBeenCalledTimes(2);
    });
  });

  it('keeps template list and tree public while hiding every template and template-node write path', async () => {
    const fixture = await render([]);

    click(fixture, 'abwab-templates-page-item-1');

    expect(element(fixture, 'abwab-templates-page-add')).toBeNull();
    expect(element(fixture, 'abwab-templates-page-copy')).toBeNull();
    expect(element(fixture, 'abwab-templates-page-edit-template')).toBeNull();
    expect(element(fixture, 'abwab-templates-page-delete-template')).toBeNull();
    expect(element(fixture, 'abwab-template-tree-row-10')).toBeTruthy();
    expect(element(fixture, 'abwab-template-tree-add-child-11')).toBeNull();
    expect(element(fixture, 'abwab-template-tree-more-11')).toBeNull();
    expect(element(fixture, 'abwab-template-tree-order-11')?.tagName).toBe('SPAN');

    const chevron = element(fixture, 'abwab-template-tree-chevron-10') as HTMLButtonElement;
    chevron.focus();
    expect(document.activeElement).toBe(chevron);
    chevron.dispatchEvent(new KeyboardEvent('keydown', { key: 'ContextMenu', bubbles: true }));
    chevron.dispatchEvent(new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true }));
    fixture.detectChanges();
    expect(element(fixture, 'abwab-templates-page-context-menu')).toBeNull();

    const internals = fixture.componentInstance as unknown as {
      startNamingTemplate(): void;
      onAddChildRequested(nodeId: number): void;
      onEditRequested(nodeId: number): void;
      onOrderCommitted(event: { nodeId: number; position: number }): void;
      openCopyModal(): void;
    };
    internals.startNamingTemplate();
    internals.onAddChildRequested(10);
    internals.onEditRequested(11);
    internals.onOrderCommitted({ nodeId: 11, position: 2 });
    internals.openCopyModal();
    fixture.detectChanges();

    expect(element(fixture, 'abwab-templates-page-new-name')).toBeNull();
    expect(element(fixture, 'abwab-template-node-modal')).toBeNull();
    expect(element(fixture, 'abwab-template-copy-modal')).toBeNull();
  });

  it.each([
    { name: 'edit-only', permission: ABWAB_WRITE_PERMISSIONS.editTemplateNode, actionTestId: 'abwab-templates-page-ctx-edit' },
    { name: 'delete-only', permission: ABWAB_WRITE_PERMISSIONS.deleteTemplateNode, actionTestId: 'abwab-templates-page-ctx-delete-node' },
  ])('opens the child context menu from a focused row for the $name persona', async ({ permission, actionTestId }) => {
    const fixture = await render([permission]);
    click(fixture, 'abwab-templates-page-item-1');

    const row = element(fixture, 'abwab-template-tree-row-11') as HTMLElement;
    row.getBoundingClientRect = () => ({ left: 120, right: 480, bottom: 64 }) as DOMRect;
    expect(row.getAttribute('tabindex')).toBe('0');
    row.focus();
    expect(document.activeElement).toBe(row);
    row.dispatchEvent(new KeyboardEvent('keydown', { key: 'ContextMenu', bubbles: true }));
    fixture.detectChanges();

    expect(element(fixture, 'abwab-templates-page-context-menu')).not.toBeNull();
    expect(element(fixture, actionTestId)).not.toBeNull();

    row.focus();
    expect(document.activeElement).toBe(row);
    row.dispatchEvent(new KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true }));
    fixture.detectChanges();
    expect(element(fixture, 'abwab-templates-page-context-menu')).not.toBeNull();
  });

  describe('template-delete confirm', () => {
    async function openFailedDelete(): Promise<ComponentFixture<AbwabTemplatesPageComponent>> {
      deleteTemplate = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
      const fixture = await render();
      click(fixture, 'abwab-templates-page-item-1');
      click(fixture, 'abwab-templates-page-delete-template');
      click(fixture, 'abwab-templates-page-delete-template-confirm-confirm');
      return fixture;
    }

    it('drops the failure message when the dialog is dismissed', async () => {
      const fixture = await openFailedDelete();
      expect(element(fixture, 'abwab-templates-page-delete-template-confirm-error')?.textContent).toContain(
        ABWAB_LABELS.writeTransportFallback,
      );

      click(fixture, 'abwab-templates-page-delete-template-confirm-cancel');

      expect(element(fixture, 'abwab-templates-page-delete-template-confirm')).toBeNull();
      expect(internals(fixture).templateDeleteError()).toBeNull();
    });

    it('refuses to dismiss while the delete is still in flight', async () => {
      const pending = new Subject<ApiResponse<unknown>>();
      deleteTemplate = vi.fn().mockReturnValue(pending);
      const fixture = await render();
      click(fixture, 'abwab-templates-page-item-1');
      click(fixture, 'abwab-templates-page-delete-template');
      click(fixture, 'abwab-templates-page-delete-template-confirm-confirm');
      expect(internals(fixture).templateDeleteBusy()).toBe(true);

      internals(fixture).cancelTemplateDelete();
      fixture.detectChanges();

      expect(element(fixture, 'abwab-templates-page-delete-template-confirm')).not.toBeNull();
    });
  });

  // Every one of these overlays is opened from the row context menu, which closeContextMenu()
  // destroys before the overlay mounts — so cdkTrapFocusAutoCapture's restore target is already
  // detached and focus would land on <body>. The doors page solves this with a stable header
  // control (abwab-page.component.ts headerFallbackFocus); this is the same fallback.
  describe('focus never drops to <body> when a context-menu overlay closes', () => {
    function flushFocus(): Promise<void> {
      return new Promise((resolve) => setTimeout(resolve, 0));
    }

    function backLink(fixture: ComponentFixture<AbwabTemplatesPageComponent>): HTMLElement | null {
      return element(fixture, 'abwab-templates-page-back');
    }

    async function openContextMenu(): Promise<ComponentFixture<AbwabTemplatesPageComponent>> {
      const fixture = await render();
      click(fixture, 'abwab-templates-page-item-1');
      click(fixture, 'abwab-template-tree-more-11');
      return fixture;
    }

    it('returns focus to the header when the node modal it opened closes', async () => {
      const fixture = await openContextMenu();
      click(fixture, 'abwab-templates-page-ctx-edit');
      expect(element(fixture, 'abwab-template-node-modal')).not.toBeNull();

      click(fixture, 'abwab-template-node-modal-cancel');
      await flushFocus();

      expect(document.activeElement).toBe(backLink(fixture));
    });

    it('returns focus to the header when the node-delete confirm it opened is cancelled', async () => {
      const fixture = await openContextMenu();
      click(fixture, 'abwab-templates-page-ctx-delete-node');
      expect(element(fixture, 'abwab-templates-page-delete-node-confirm')).not.toBeNull();

      click(fixture, 'abwab-templates-page-delete-node-confirm-cancel');
      await flushFocus();

      expect(document.activeElement).toBe(backLink(fixture));
    });

    it('leaves focus alone when the node modal was opened from a control that survives', async () => {
      const fixture = await render();
      click(fixture, 'abwab-templates-page-item-1');
      click(fixture, 'abwab-templates-page-edit-template');

      click(fixture, 'abwab-template-node-modal-cancel');
      await flushFocus();

      expect(document.activeElement).not.toBe(backLink(fixture));
    });
  });
});
