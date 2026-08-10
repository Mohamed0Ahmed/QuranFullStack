import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ConfirmDialogComponent } from './confirm-dialog.component';

@Component({
  standalone: true,
  imports: [ConfirmDialogComponent],
  template: `
    <qd-confirm-dialog
      [open]="open()"
      titleText="SYNTH_TITLE"
      confirmLabel="SYNTH_CONFIRM"
      cancelLabel="SYNTH_CANCEL"
      [tone]="tone()"
      [busy]="busy()"
      [confirmDisabled]="confirmDisabled()"
      (confirmed)="events.push('confirmed')"
      (cancelled)="events.push('cancelled')"
    >
      <p data-testid="projected-body">SYNTH_BODY</p>
    </qd-confirm-dialog>
  `,
})
class HostComponent {
  readonly open = signal(true);
  readonly tone = signal<'default' | 'danger'>('default');
  readonly busy = signal(false);
  readonly confirmDisabled = signal(false);
  readonly events: string[] = [];
}

describe('ConfirmDialogComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let host: HostComponent;

  const query = <T extends HTMLElement>(testId: string): T | null =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);

  const dialog = () => query('qd-confirm-dialog')!;
  const confirmButton = () => query<HTMLButtonElement>('qd-confirm-dialog-confirm')!;
  const cancelButton = () => query<HTMLButtonElement>('qd-confirm-dialog-cancel')!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders nothing until it is opened', () => {
    host.open.set(false);
    fixture.detectChanges();

    expect(query('qd-confirm-dialog')).toBeNull();
  });

  it('renders the labels and the projected body', () => {
    expect(dialog().textContent).toContain('SYNTH_TITLE');
    expect(confirmButton().textContent?.trim()).toBe('SYNTH_CONFIRM');
    expect(cancelButton().textContent?.trim()).toBe('SYNTH_CANCEL');
    expect(query('projected-body')?.textContent).toBe('SYNTH_BODY');
  });

  // The roles are the whole reason this primitive exists — a hand-rolled confirm that gets them
  // wrong looks identical on screen.
  it('is an alertdialog labelled by its own title', () => {
    expect(dialog().getAttribute('role')).toBe('alertdialog');
    expect(dialog().getAttribute('aria-modal')).toBe('true');

    const labelledBy = dialog().getAttribute('aria-labelledby');
    expect(labelledBy).toBeTruthy();
    expect(fixture.nativeElement.querySelector(`#${labelledBy}`)?.textContent).toContain('SYNTH_TITLE');
  });

  // Cancel, not confirm: the dialog interrupts, so a reflexive Enter must produce the safe answer.
  it('puts initial focus on cancel', async () => {
    await new Promise((resolve) => setTimeout(resolve));

    expect(document.activeElement).toBe(cancelButton());
  });

  it('emits confirmed and cancelled from their own buttons', () => {
    confirmButton().click();
    cancelButton().click();

    expect(host.events).toEqual(['confirmed', 'cancelled']);
  });

  it('cancels on Escape and on a backdrop click', () => {
    dialog().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    query('qd-confirm-dialog-backdrop')!.click();

    expect(host.events).toEqual(['cancelled', 'cancelled']);
  });

  it('does not cancel when the click started inside the dialog', () => {
    dialog().click();

    expect(host.events).toEqual([]);
  });

  // Both buttons, not just confirm: a decision in flight should not be cancellable into an
  // inconsistent state either, and a second confirm must not fire the write twice.
  it('disables both buttons while busy, and emits nothing', () => {
    host.busy.set(true);
    fixture.detectChanges();

    expect(confirmButton().disabled).toBe(true);
    expect(cancelButton().disabled).toBe(true);

    confirmButton().click();
    cancelButton().click();
    expect(host.events).toEqual([]);
  });

  // Disabled alone reads as "not available"; aria-busy is what says "working on it" — the same signal
  // qd-state and the skeletons use, so a consumer does not invent a second vocabulary for waiting.
  it('carries the house busy affordance on confirm, and only while busy', () => {
    expect(confirmButton().getAttribute('aria-busy')).toBeNull();

    host.busy.set(true);
    fixture.detectChanges();

    expect(confirmButton().getAttribute('aria-busy')).toBe('true');
  });

  it('disables confirm alone when the decision is incomplete', () => {
    host.confirmDisabled.set(true);
    fixture.detectChanges();

    expect(confirmButton().disabled).toBe(true);
    expect(cancelButton().disabled).toBe(false);

    confirmButton().click();
    expect(host.events).toEqual([]);
  });

  // Phase 7: the destructive treatment is the shared F05 danger variant, not a local override.
  it('marks the confirm button destructive only for the danger tone', () => {
    expect(confirmButton().classList).toContain('qd-action--primary');
    expect(confirmButton().classList).not.toContain('qd-action--danger');

    host.tone.set('danger');
    fixture.detectChanges();

    expect(confirmButton().classList).toContain('qd-action--danger');
    expect(confirmButton().classList).not.toContain('qd-action--primary');
  });

  // Phase 3 moved the framing onto QdModalShellComponent. The confirm keeps its alert-dialog
  // semantics and its own copy, and gains the shell's named 30rem geometry instead of a local one.
  it('renders through the named confirm shell rather than a local modal box', () => {
    expect(dialog().getAttribute('data-qd-modal-variant')).toBe('confirm');
    expect(dialog().classList).toContain('qd-modal-shell--confirm');
    expect(dialog().querySelector('[data-testid="qd-confirm-dialog-close"]')).toBeNull();
  });

  it('keeps the projected decision copy inside the shell body and the two actions in its footer', () => {
    const body = query('qd-confirm-dialog-body')!;
    const footer = query('qd-confirm-dialog-footer')!;

    expect(body.querySelector('[data-testid="projected-body"]')).toBeTruthy();
    expect(footer.contains(confirmButton())).toBe(true);
    expect(footer.contains(cancelButton())).toBe(true);
    expect(body.contains(confirmButton())).toBe(false);
  });

  describe('testIdPrefix', () => {
    const SUFFIXES = ['', '-backdrop', '-confirm', '-cancel'] as const;

    it.each(SUFFIXES)('falls back to the default prefix for "%s" when the input is omitted', (suffix) => {
      expect(query(`qd-confirm-dialog${suffix}`)).toBeTruthy();
    });

    it.each(SUFFIXES)('renames "%s" onto a custom prefix', async (suffix) => {
      await TestBed.resetTestingModule();
      await TestBed.configureTestingModule({ imports: [PrefixedHostComponent] }).compileComponents();
      const prefixed = TestBed.createComponent(PrefixedHostComponent);
      prefixed.detectChanges();
      const root = prefixed.nativeElement as HTMLElement;

      expect(root.querySelector(`[data-testid="SYNTH_PREFIX${suffix}"]`)).toBeTruthy();
      expect(root.querySelector(`[data-testid="qd-confirm-dialog${suffix}"]`)).toBeNull();
    });
  });
});

@Component({
  standalone: true,
  imports: [ConfirmDialogComponent],
  template: `
    <qd-confirm-dialog
      [open]="true"
      testIdPrefix="SYNTH_PREFIX"
      titleText="SYNTH_TITLE"
      confirmLabel="SYNTH_CONFIRM"
      cancelLabel="SYNTH_CANCEL"
    />
  `,
})
class PrefixedHostComponent {}
