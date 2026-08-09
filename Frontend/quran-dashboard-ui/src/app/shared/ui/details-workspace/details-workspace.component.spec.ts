import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import {
  QdDetailsWorkspaceComponent,
  QdDetailsWorkspaceLayout,
} from './details-workspace.component';

@Component({
  standalone: true,
  imports: [QdDetailsWorkspaceComponent],
  template: `
    <qd-details-workspace
      [identity]="identity()"
      [layout]="layout()"
      [hasTabs]="hasTabs()"
      [hasFooter]="hasFooter()"
      noSelectionMessage="SYNTH_NO_SELECTION"
    >
      <dl qdDetailsMetadata data-testid="projected-metadata">
        <dt>SYNTH_LABEL</dt>
        <dd>SYNTH_VALUE</dd>
      </dl>
      <button qdDetailsActions type="button" data-testid="projected-action">SYNTH_ACTION</button>
      <div qdDetailsTabs data-testid="projected-tabs">SYNTH_TABS</div>
      <p qdDetailsStatus data-testid="projected-status">SYNTH_STATUS</p>
      <p data-testid="projected-body">SYNTH_BODY</p>
      <div qdDetailsFooter data-testid="projected-footer">SYNTH_FOOTER</div>
    </qd-details-workspace>
  `,
})
class HostComponent {
  readonly identity = signal('SYNTH_IDENTITY');
  readonly layout = signal<QdDetailsWorkspaceLayout>('selection');
  readonly hasTabs = signal(true);
  readonly hasFooter = signal(true);
}

@Component({
  standalone: true,
  imports: [QdDetailsWorkspaceComponent],
  template: `
    <qd-details-workspace identity="SYNTH_INLINE" testIdPrefix="inline" />
    <qd-details-workspace identity="SYNTH_OVERLAY" testIdPrefix="overlay" />
  `,
})
class TwoWorkspacesHostComponent {}

describe('QdDetailsWorkspaceComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let host: HostComponent;

  const query = (testId: string): HTMLElement =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('labels the panel by its own identity heading', () => {
    const shell = query('qd-details');
    const labelledBy = shell.getAttribute('aria-labelledby')!;

    expect(fixture.nativeElement.querySelector(`#${labelledBy}`)?.textContent).toContain('SYNTH_IDENTITY');
  });

  it('places every named zone in its own region and only the body content inside the scroller', () => {
    expect(query('qd-details-body').querySelector('[data-testid="projected-body"]')).toBeTruthy();

    for (const outside of ['projected-metadata', 'projected-action', 'projected-tabs', 'projected-status', 'projected-footer']) {
      expect(query('qd-details-body').querySelector(`[data-testid="${outside}"]`)).toBeNull();
    }

    expect(query('qd-details').querySelectorAll('.qd-details__body')).toHaveLength(1);
  });

  // D41/F12: the status slot is permanently mounted and polite, so a later message lands in a region
  // that already existed instead of pushing the body down.
  it('keeps one permanently mounted polite status slot outside the body', () => {
    const status = query('qd-details-status');

    expect(status.getAttribute('role')).toBe('status');
    expect(status.getAttribute('aria-live')).toBe('polite');
    expect(status.closest('.qd-details__body')).toBeNull();
  });

  it('drops the tab and footer regions when the consumer declares none', () => {
    host.hasTabs.set(false);
    host.hasFooter.set(false);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="qd-details-tabs"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="qd-details-footer"]')).toBeNull();
  });

  // The Wide panel must stay mounted with a designed prompt rather than collapsing the split into a
  // blank column.
  it('renders the designed no-selection prompt instead of the body, keeping the shell mounted', () => {
    host.layout.set('no-selection');
    fixture.detectChanges();

    expect(query('qd-details-no-selection').textContent).toContain('SYNTH_NO_SELECTION');
    expect(fixture.nativeElement.querySelector('[data-testid="projected-body"]')).toBeNull();
    expect(query('qd-details')).toBeTruthy();
    expect(query('qd-details-status')).toBeTruthy();
  });

  // D31: an inline details panel and the global overlay's details body coexist on one page.
  it('namespaces identity, status, tab and panel ids per instance', async () => {
    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [TwoWorkspacesHostComponent] }).compileComponents();
    const two = TestBed.createComponent(TwoWorkspacesHostComponent);
    two.detectChanges();
    const root = two.nativeElement as HTMLElement;

    const inline = root.querySelector('[data-testid="inline"]')!.getAttribute('aria-labelledby');
    const overlay = root.querySelector('[data-testid="overlay"]')!.getAttribute('aria-labelledby');
    expect(inline).not.toBe(overlay);

    const [first, second] = two.debugElement
      .queryAll(By.directive(QdDetailsWorkspaceComponent))
      .map((node) => node.componentInstance as QdDetailsWorkspaceComponent);
    expect(first.tabId('words')).not.toBe(second.tabId('words'));
    expect(first.panelId('words')).not.toBe(second.panelId('words'));
    expect(first.statusId).not.toBe(second.statusId);

    two.destroy();
  });
});
