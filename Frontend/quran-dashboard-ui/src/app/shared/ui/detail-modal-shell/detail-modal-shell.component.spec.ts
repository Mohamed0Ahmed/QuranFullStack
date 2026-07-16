import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { DetailModalShellComponent } from './detail-modal-shell.component';
import { ScrollLockService } from '../modal-scroll-lock/scroll-lock.service';

@Component({
  standalone: true,
  imports: [DetailModalShellComponent],
  template: `
    <qd-detail-modal-shell
      [visibility]="visibility()"
      [titleText]="title()"
      [depth]="depth()"
      backLabel="SYNTH_BACK"
      closeLabel="SYNTH_CLOSE"
      restoreLabel="SYNTH_RESTORE"
      [restoreAriaLabel]="'SYNTH_RESTORE ' + title()"
      [statusMessage]="status()"
      (backRequested)="events.push('back')"
      (closeRequested)="events.push('close')"
      (restoreRequested)="events.push('restore')"
    >
      <p data-testid="projected-detail">SYNTH_DETAIL_CONTENT</p>
    </qd-detail-modal-shell>
  `,
})
class ShellHostComponent {
  readonly visibility = signal<'open' | 'closed'>('open');
  readonly title = signal('SYNTH_TITLE');
  readonly depth = signal(1);
  readonly status = signal('');
  readonly events: string[] = [];
}

describe('DetailModalShellComponent', () => {
  let host: ShellHostComponent;
  let root: HTMLElement;

  function detect(): void {
    TestBed.inject(ScrollLockService);
    fixtureRef.detectChanges();
  }

  let fixtureRef: ReturnType<typeof TestBed.createComponent<ShellHostComponent>>;

  beforeEach(() => {
    document.body.style.overflow = '';
    fixtureRef = TestBed.createComponent(ShellHostComponent);
    host = fixtureRef.componentInstance;
    root = fixtureRef.nativeElement as HTMLElement;
    detect();
  });

  afterEach(() => {
    fixtureRef.destroy();
    document.body.style.overflow = '';
  });

  it('renders an RTL dialog with aria-modal and a labelling heading', () => {
    const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;

    expect(dialog.getAttribute('role')).toBe('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.getAttribute('dir')).toBe('rtl');

    const labelledBy = dialog.getAttribute('aria-labelledby')!;
    const heading = document.getElementById(labelledBy)!;
    expect(heading.textContent).toContain('SYNTH_TITLE');
    expect(root.querySelector('[data-testid="projected-detail"]')?.textContent).toContain('SYNTH_DETAIL_CONTENT');
  });

  it('shows Back only above depth one and emits back/close from the header actions', () => {
    expect(root.querySelector('[data-testid="detail-modal-back"]')).toBeNull();

    host.depth.set(2);
    detect();
    const back = root.querySelector('[data-testid="detail-modal-back"]') as HTMLButtonElement;
    expect(back.textContent).toContain('SYNTH_BACK');

    back.click();
    (root.querySelector('[data-testid="detail-modal-close"]') as HTMLButtonElement).click();
    expect(host.events).toEqual(['back', 'close']);
  });

  it('emits close on Escape and on backdrop click, but not on clicks inside the dialog', () => {
    const dialog = root.querySelector('[data-testid="detail-modal-shell"]') as HTMLElement;
    dialog.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(host.events).toEqual(['close']);

    (root.querySelector('[data-testid="projected-detail"]') as HTMLElement).click();
    expect(host.events).toEqual(['close']);

    (root.querySelector('[data-testid="detail-modal-backdrop"]') as HTMLElement).click();
    expect(host.events).toEqual(['close', 'close']);
  });

  it('renders the restore control with the retained-title accessible name when closed', () => {
    host.visibility.set('closed');
    detect();

    expect(root.querySelector('[data-testid="detail-modal-shell"]')).toBeNull();
    const restore = root.querySelector('[data-testid="detail-modal-restore"]') as HTMLButtonElement;
    expect(restore.textContent).toContain('SYNTH_RESTORE');
    expect(restore.getAttribute('aria-label')).toBe('SYNTH_RESTORE SYNTH_TITLE');

    restore.click();
    expect(host.events).toEqual(['restore']);
  });

  it('holds the reference-counted scroll lock only while open', () => {
    expect(document.body.style.overflow).toBe('hidden');

    host.visibility.set('closed');
    detect();
    expect(document.body.style.overflow).toBe('');

    host.visibility.set('open');
    detect();
    expect(document.body.style.overflow).toBe('hidden');

    fixtureRef.destroy();
    expect(document.body.style.overflow).toBe('');
  });

  it('announces the title and an optional status through polite live regions', () => {
    const live = root.querySelector('[data-testid="detail-modal-live-title"]')!;
    expect(live.getAttribute('aria-live')).toBe('polite');
    expect(live.textContent).toContain('SYNTH_TITLE');

    host.status.set('SYNTH_CAP_STATUS');
    detect();
    expect(root.querySelector('[data-testid="detail-modal-live-status"]')?.textContent).toContain('SYNTH_CAP_STATUS');
  });

  it('traps focus inside the open dialog', () => {
    const dialog = root.querySelector('[data-testid="detail-modal-shell"]') as HTMLElement;
    expect(dialog.hasAttribute('cdktrapfocus')).toBe(true);
    // CDK renders the trap anchors as tabindex sentinels around the dialog.
    const anchors = root.querySelectorAll('.cdk-focus-trap-anchor');
    expect(anchors.length).toBeGreaterThanOrEqual(2);
  });
});
