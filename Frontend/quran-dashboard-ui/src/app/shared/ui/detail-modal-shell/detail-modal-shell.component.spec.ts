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
      [kindLabel]="kindLabel()"
      [countText]="countText()"
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
      @if (showOpener()) {
        <button type="button" data-testid="synth-opener">SYNTH_OPEN_NESTED</button>
      }
    </qd-detail-modal-shell>
  `,
})
class ShellHostComponent {
  readonly visibility = signal<'open' | 'closed'>('open');
  readonly title = signal('SYNTH_TITLE');
  readonly kindLabel = signal('');
  readonly countText = signal('');
  readonly depth = signal(1);
  readonly status = signal('');
  /** Stands in for the projected link that pushed the next frame. */
  readonly showOpener = signal(true);
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

  it('renders the kind chip only when a label is supplied', () => {
    expect(root.querySelector('[data-testid="detail-modal-kind"]')).toBeNull();

    host.kindLabel.set('SYNTH_KIND');
    detect();
    expect(root.querySelector('[data-testid="detail-modal-kind"]')?.textContent).toContain('SYNTH_KIND');
  });

  it('keeps the count box mounted while empty and only fades its text in on arrival', () => {
    const countBox = () => root.querySelector('[data-testid="detail-modal-count"]');

    // The box must never appear/disappear: it holds its reserved width from the
    // first render so the arriving count cannot shift the header.
    expect(countBox()).not.toBeNull();
    expect(countBox()!.textContent?.trim()).toBe('');
    expect(countBox()!.classList.contains('detail-modal-shell__count--visible')).toBe(false);

    host.countText.set('SYNTH_COUNT: 42');
    detect();
    expect(countBox()).not.toBeNull();
    expect(countBox()!.textContent).toContain('SYNTH_COUNT: 42');
    expect(countBox()!.classList.contains('detail-modal-shell__count--visible')).toBe(true);
  });

  it('describes the dialog by the count without placing it in either live region', () => {
    host.countText.set('SYNTH_COUNT: 42');
    host.status.set('SYNTH_CAP_STATUS');
    detect();

    const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
    const count = root.querySelector('[data-testid="detail-modal-count"]')!;
    expect(dialog.getAttribute('aria-describedby')).toBe(count.id);
    expect(count.id).not.toBe(dialog.getAttribute('aria-labelledby'));

    // Inlining the count into the heading or a live region would double-announce
    // it on load, so it must live outside both.
    expect(count.closest('[aria-live]')).toBeNull();
    expect(count.closest('h2')).toBeNull();
    expect(root.querySelector('[data-testid="detail-modal-live-title"]')!.textContent).not.toContain('SYNTH_COUNT');
    expect(root.querySelector('[data-testid="detail-modal-live-status"]')!.textContent).not.toContain('SYNTH_COUNT');
  });

  // Phase 7 (D48/D49): the overlay is a thin adapter over the one F14 shell, so its geometry is
  // the named `overlay` width rather than a fifth local drawer box, and the shell's body remains
  // the single scroll region with the header outside it.
  it('resolves to the named overlay width on the shared shell rather than a local geometry', () => {
    const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
    const body = root.querySelector('.qd-modal-shell__body');
    const header = root.querySelector('.qd-modal-shell__header');

    expect(dialog.getAttribute('data-qd-modal-variant')).toBe('overlay');
    expect(dialog.classList.contains('qd-modal-shell--overlay')).toBe(true);
    expect(Array.from(dialog.classList).filter((name) => name.startsWith('qd-modal-shell--'))).toEqual([
      'qd-modal-shell--overlay',
    ]);
    expect(header).not.toBeNull();
    expect(body).not.toBeNull();
    expect(body!.contains(header)).toBe(false);
    expect(root.querySelectorAll('.qd-modal-shell__body').length).toBe(1);
    expect(root.querySelector('[data-testid="detail-modal-back"], [data-testid="detail-modal-close"]')!
      .closest('.qd-modal-shell__body')).toBeNull();
  });

  // The last Back destroys the Back button itself, so the dialog must choose a
  // new focus target or focus falls out of the still-open dialog onto the body.
  describe('focus after the final Back (depth 2 → 1)', () => {
    const settleFocus = () => new Promise((resolve) => setTimeout(resolve, 0));

    async function pushThenPopToDepthOne(): Promise<HTMLButtonElement> {
      // Let the trap's auto-capture finish before we stage our own focus.
      await settleFocus();
      const opener = root.querySelector('[data-testid="synth-opener"]') as HTMLButtonElement;
      opener.focus();
      expect(document.activeElement).toBe(opener);

      host.depth.set(2);
      detect();
      (root.querySelector('[data-testid="detail-modal-back"]') as HTMLButtonElement).focus();
      return opener;
    }

    it('returns focus to the invoking link when it survived the pop', async () => {
      const opener = await pushThenPopToDepthOne();

      host.depth.set(1);
      detect();
      await settleFocus();

      const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
      expect(document.activeElement).toBe(opener);
      expect(dialog.contains(document.activeElement)).toBe(true);
    });

    it('falls back to Close, still inside the dialog, when the invoking link is gone', async () => {
      await pushThenPopToDepthOne();

      // The parent frame re-rendered without the link that opened frame two.
      host.showOpener.set(false);
      host.depth.set(1);
      detect();
      await settleFocus();

      const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
      expect(document.activeElement).toBe(root.querySelector('[data-testid="detail-modal-close"]'));
      expect(dialog.contains(document.activeElement)).toBe(true);
    });
  });

  describe('focus after a push (depth 1 → 2)', () => {
    const settleFocus = () => new Promise((resolve) => setTimeout(resolve, 0));

    it('moves focus to Back when the activated link no longer exists in the new frame', async () => {
      await settleFocus();
      const opener = root.querySelector('[data-testid="synth-opener"]') as HTMLButtonElement;
      opener.focus();

      // The pushed frame replaces the content that held the link the user activated.
      host.showOpener.set(false);
      host.depth.set(2);
      detect();
      await settleFocus();

      const dialog = root.querySelector('[data-testid="detail-modal-shell"]')!;
      expect(document.activeElement).toBe(root.querySelector('[data-testid="detail-modal-back"]'));
      expect(dialog.contains(document.activeElement)).toBe(true);
      expect(document.activeElement).not.toBe(document.body);
    });

    it('leaves focus alone when it survived inside the dialog', async () => {
      await settleFocus();
      const close = root.querySelector('[data-testid="detail-modal-close"]') as HTMLButtonElement;
      close.focus();

      host.depth.set(2);
      detect();
      await settleFocus();

      expect(document.activeElement).toBe(close);
    });
  });

  it('traps focus inside the open dialog', () => {
    // CDK renders the trap anchors as tabindex sentinels around the dialog; the shell enables the
    // trap only while this overlay is the topmost open shell.
    const anchors = root.querySelectorAll('.cdk-focus-trap-anchor[tabindex="0"]');
    expect(anchors.length).toBeGreaterThanOrEqual(2);
  });
});
