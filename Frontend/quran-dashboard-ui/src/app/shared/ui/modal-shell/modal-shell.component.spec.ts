import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { QdModalShellComponent, QdModalShellDismissReason, QdModalShellVariant } from './modal-shell.component';
import { ScrollLockService } from '../modal-scroll-lock/scroll-lock.service';

@Component({
  standalone: true,
  imports: [QdModalShellComponent],
  template: `
    <button type="button" data-testid="opener" (click)="open.set(true)">SYNTH_OPEN</button>
    <qd-modal-shell
      [open]="open()"
      [variant]="variant()"
      titleText="SYNTH_TITLE"
      [dialogRole]="dialogRole()"
      [showClose]="showClose()"
      [hasFooter]="hasFooter()"
      [dismissOnBackdrop]="dismissOnBackdrop()"
      [dismissOnEscape]="dismissOnEscape()"
      [returnFocus]="returnFocus()"
      (dismissed)="reasons.push($event)"
    >
      <p data-testid="projected-body">SYNTH_BODY</p>
      <button qdModalShellFooter type="button" data-testid="projected-footer">SYNTH_SUBMIT</button>
    </qd-modal-shell>
  `,
})
class HostComponent {
  readonly open = signal(true);
  readonly variant = signal<QdModalShellVariant>('form');
  readonly dialogRole = signal<'dialog' | 'alertdialog'>('dialog');
  readonly showClose = signal(true);
  readonly hasFooter = signal(true);
  readonly dismissOnBackdrop = signal(true);
  readonly dismissOnEscape = signal(true);
  readonly returnFocus = signal(true);
  readonly reasons: QdModalShellDismissReason[] = [];
}

@Component({
  standalone: true,
  imports: [QdModalShellComponent],
  template: `
    <qd-modal-shell [open]="true" titleText="SYNTH_OUTER" testIdPrefix="outer" [trapFocus]="outerTrap()">
      <button type="button" data-testid="outer-field">SYNTH_OUTER_FIELD</button>
      @if (innerOpen()) {
        <qd-modal-shell [open]="true" titleText="SYNTH_INNER" testIdPrefix="inner">
          <button type="button" data-testid="inner-field">SYNTH_INNER_FIELD</button>
        </qd-modal-shell>
      }
    </qd-modal-shell>
  `,
})
class NestedShellsHostComponent {
  readonly innerOpen = signal(false);
  readonly outerTrap = signal(true);
}

@Component({
  standalone: true,
  imports: [QdModalShellComponent],
  template: `
    <qd-modal-shell [open]="true" titleText="SYNTH_ONE" testIdPrefix="shell-one" />
    <qd-modal-shell [open]="true" titleText="SYNTH_TWO" testIdPrefix="shell-two" />
  `,
})
class TwoShellsHostComponent {}

const flush = () => new Promise((resolve) => setTimeout(resolve));

describe('QdModalShellComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let host: HostComponent;

  const query = <T extends HTMLElement>(testId: string): T | null =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);

  const dialog = () => query('qd-modal-shell')!;

  beforeEach(async () => {
    document.body.style.overflow = '';
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders nothing until it is opened', () => {
    host.open.set(false);
    fixture.detectChanges();

    expect(query('qd-modal-shell')).toBeNull();
    expect(query('qd-modal-shell-backdrop')).toBeNull();
  });

  it('is a modal dialog labelled by its own title, and can be an alertdialog instead', () => {
    expect(dialog().getAttribute('role')).toBe('dialog');
    expect(dialog().getAttribute('aria-modal')).toBe('true');

    const labelledBy = dialog().getAttribute('aria-labelledby')!;
    expect(fixture.nativeElement.querySelector(`#${labelledBy}`)?.textContent).toContain('SYNTH_TITLE');

    host.dialogRole.set('alertdialog');
    fixture.detectChanges();
    expect(dialog().getAttribute('role')).toBe('alertdialog');
  });

  // D48: four named widths and no fifth. The class is what the stylesheet keys the width off, so a
  // consumer that invents a geometry has nowhere to put it.
  it.each([
    ['confirm', 'qd-modal-shell--confirm'],
    ['form', 'qd-modal-shell--form'],
    ['wide', 'qd-modal-shell--wide'],
    ['overlay', 'qd-modal-shell--overlay'],
  ] as const)('resolves the %s variant to exactly one named width class', (variant, expected) => {
    host.variant.set(variant);
    fixture.detectChanges();

    const widthClasses = Array.from(dialog().classList).filter((name) =>
      name.startsWith('qd-modal-shell--'),
    );
    expect(widthClasses).toEqual([expected]);
    expect(dialog().getAttribute('data-qd-modal-variant')).toBe(variant);
  });

  // D49: the shell owns padding and the body is the only region that scrolls, so header and footer
  // must be siblings of the body rather than content inside it.
  it('projects the body into the single scrolling region and keeps header and footer outside it', () => {
    const body = query('qd-modal-shell-body')!;
    expect(body.querySelector('[data-testid="projected-body"]')).toBeTruthy();
    expect(body.querySelector('[data-testid="projected-footer"]')).toBeNull();

    expect(dialog().querySelectorAll('.qd-modal-shell__body')).toHaveLength(1);
    expect(query('qd-modal-shell-footer')?.querySelector('[data-testid="projected-footer"]')).toBeTruthy();
    expect(query('qd-modal-shell-title')!.closest('.qd-modal-shell__body')).toBeNull();
  });

  it('drops the footer region entirely when the consumer declares none', () => {
    host.hasFooter.set(false);
    fixture.detectChanges();

    expect(query('qd-modal-shell-footer')).toBeNull();
  });

  it.each([
    ['escape', 'escape'],
    ['backdrop', 'backdrop'],
    ['close', 'close'],
  ] as const)('reports the %s dismissal route with its own reason', (route, expected) => {
    if (route === 'escape') {
      dialog().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    } else if (route === 'backdrop') {
      query('qd-modal-shell-backdrop')!.click();
    } else {
      query('qd-modal-shell-close')!.click();
    }

    expect(host.reasons).toEqual([expected]);
  });

  it('does not dismiss when the click landed inside the dialog rather than on the backdrop', () => {
    dialog().click();
    query('projected-body')!.click();

    expect(host.reasons).toEqual([]);
  });

  // A drag-select that starts on a text node inside the body and is released over the backdrop
  // reports its click on the backdrop. Dismissing there discards the draft the user was selecting.
  it('ignores a backdrop click whose press started inside the dialog', () => {
    const backdrop = query('qd-modal-shell-backdrop')!;
    query('projected-body')!.dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));
    backdrop.click();

    expect(host.reasons).toEqual([]);
  });

  it('dismisses when the press and the release both land on the backdrop', () => {
    const backdrop = query('qd-modal-shell-backdrop')!;
    backdrop.dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));
    backdrop.click();

    expect(host.reasons).toEqual(['backdrop']);
  });

  // The topmost surface owns Escape. A shell that declines to dismiss must still consume the key,
  // or an ancestor drawer or the document-level navbar listener closes instead of it.
  it.each([
    ['dismisses', true, ['escape'] as const],
    ['declines', false, [] as const],
  ])('consumes Escape while it is topmost even when it %s', (_case, dismissOnEscape, expected) => {
    host.dismissOnEscape.set(dismissOnEscape);
    fixture.detectChanges();
    const ancestorSaw: string[] = [];
    fixture.nativeElement.addEventListener('keydown', () => ancestorSaw.push('escape'));

    const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
    dialog().dispatchEvent(event);

    expect(host.reasons).toEqual([...expected]);
    expect(ancestorSaw).toEqual([]);
    expect(event.defaultPrevented).toBe(true);
  });

  // A dirty authoring form has to be able to refuse both casual dismissal routes without losing the
  // close button.
  it('suppresses the escape and backdrop routes when the consumer opts out, and keeps close working', () => {
    host.dismissOnEscape.set(false);
    host.dismissOnBackdrop.set(false);
    fixture.detectChanges();

    dialog().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    query('qd-modal-shell-backdrop')!.click();
    expect(host.reasons).toEqual([]);

    query('qd-modal-shell-close')!.click();
    expect(host.reasons).toEqual(['close']);
  });

  it('omits the close control when the consumer owns dismissal itself', () => {
    host.showClose.set(false);
    fixture.detectChanges();

    expect(query('qd-modal-shell-close')).toBeNull();
  });

  describe('page and focus custody', () => {
    it('holds the reference-counted scroll lock while open and hands it back on close', () => {
      const lock = TestBed.inject(ScrollLockService);
      expect(lock.holderCount()).toBe(1);

      host.open.set(false);
      fixture.detectChanges();
      expect(lock.holderCount()).toBe(0);
    });

    it('releases its hold when the shell is destroyed while still open', () => {
      const lock = TestBed.inject(ScrollLockService);
      expect(lock.isLocked()).toBe(true);

      fixture.destroy();
      expect(lock.isLocked()).toBe(false);
    });

    it('returns focus to the control that opened it', async () => {
      host.open.set(false);
      fixture.detectChanges();

      const opener = query<HTMLButtonElement>('opener')!;
      opener.focus();
      opener.click();
      fixture.detectChanges();
      await flush();

      query('qd-modal-shell-close')!.focus();
      expect(dialog().contains(document.activeElement)).toBe(true);

      host.open.set(false);
      fixture.detectChanges();
      await flush();

      expect(document.activeElement).toBe(opener);
    });

    // One owner only. The CDK trap used to restore synchronously on destroy and the shell again on a
    // macrotask, so a consumer that placed focus deliberately on close lost it a tick later.
    it('does not take focus back after the consumer places it on close', async () => {
      const elsewhere = document.createElement('button');
      document.body.appendChild(elsewhere);
      host.open.set(false);
      fixture.detectChanges();

      const opener = query<HTMLButtonElement>('opener')!;
      opener.focus();
      opener.click();
      fixture.detectChanges();
      await flush();
      query('qd-modal-shell-close')!.focus();

      host.open.set(false);
      fixture.detectChanges();
      elsewhere.focus();
      await flush();

      expect(document.activeElement).toBe(elsewhere);
      elsewhere.remove();
    });

    it('leaves focus placement alone when the consumer claims it', async () => {
      const opener = document.createElement('button');
      document.body.appendChild(opener);
      host.open.set(false);
      fixture.detectChanges();
      host.returnFocus.set(false);
      opener.focus();

      host.open.set(true);
      fixture.detectChanges();
      await flush();
      query('qd-modal-shell-close')!.focus();

      host.open.set(false);
      fixture.detectChanges();
      await flush();

      expect(document.activeElement).not.toBe(opener);
      opener.remove();
    });

    it('returns focus to the opener when it is destroyed while still open', async () => {
      const opener = document.createElement('button');
      document.body.appendChild(opener);
      host.open.set(false);
      fixture.detectChanges();
      opener.focus();

      host.open.set(true);
      fixture.detectChanges();
      await flush();
      query('qd-modal-shell-close')!.focus();

      fixture.destroy();

      expect(document.activeElement).toBe(opener);
      opener.remove();
    });
  });

  // FR-05: two shells on screen means two CDK traps. Only the topmost may be enabled, or Tab is
  // caught by the wrong one; a suspended trap drops its anchors out of the tab order.
  describe('nested shells', () => {
    let nested: ReturnType<typeof TestBed.createComponent<NestedShellsHostComponent>>;

    const enabledAnchors = () =>
      Array.from(nested.nativeElement.querySelectorAll('.cdk-focus-trap-anchor[tabindex="0"]'));

    const trapped = (testId: string) => {
      const element = nested.nativeElement.querySelector(`[data-testid="${testId}"]`);
      return enabledAnchors().some(
        (anchor) =>
          (anchor as HTMLElement).nextElementSibling === element ||
          (anchor as HTMLElement).previousElementSibling === element,
      );
    };

    const renderNested = async () => {
      await TestBed.resetTestingModule();
      await TestBed.configureTestingModule({ imports: [NestedShellsHostComponent] }).compileComponents();
      nested = TestBed.createComponent(NestedShellsHostComponent);
      nested.detectChanges();
      await nested.whenStable();
    };

    it('hands the only enabled trap to the shell that opened last', async () => {
      await renderNested();
      expect(trapped('outer')).toBe(true);

      nested.componentInstance.innerOpen.set(true);
      nested.detectChanges();

      expect(trapped('inner')).toBe(true);
      expect(trapped('outer')).toBe(false);
      expect(enabledAnchors()).toHaveLength(2);

      nested.componentInstance.innerOpen.set(false);
      nested.detectChanges();

      expect(trapped('outer')).toBe(true);
      expect(enabledAnchors()).toHaveLength(2);
      nested.destroy();
    });

    it('lets a consumer suspend its own trap while it hosts a nested decision', async () => {
      await renderNested();
      nested.componentInstance.outerTrap.set(false);
      nested.detectChanges();

      expect(trapped('outer')).toBe(false);
      expect(enabledAnchors()).toHaveLength(0);
      nested.destroy();
    });
  });

  // D31: an inline shell and an overlay shell coexist; a shared literal id would make one dialog's
  // accessible name point at the other's heading.
  it('gives two simultaneous shells distinct title ids', async () => {
    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [TwoShellsHostComponent] }).compileComponents();
    const two = TestBed.createComponent(TwoShellsHostComponent);
    two.detectChanges();
    const root = two.nativeElement as HTMLElement;

    const first = root.querySelector('[data-testid="shell-one"]')!.getAttribute('aria-labelledby');
    const second = root.querySelector('[data-testid="shell-two"]')!.getAttribute('aria-labelledby');

    expect(first).toBeTruthy();
    expect(first).not.toBe(second);
    expect(root.querySelector(`#${first}`)?.textContent).toContain('SYNTH_ONE');
    expect(root.querySelector(`#${second}`)?.textContent).toContain('SYNTH_TWO');

    two.destroy();
  });
});
