import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { CONTEXT_MENU_LABELS } from './context-menu.labels';
import { QdContextMenuComponent } from './context-menu.component';

@Component({
  standalone: true,
  imports: [QdContextMenuComponent],
  template: `
    <button type="button" data-testid="invoker" (click)="open.set(true)">SYNTH_INVOKER</button>
    @if (open()) {
      <qd-context-menu
        [position]="{ x: 40, y: 60 }"
        menuTestId="synth-menu"
        backdropTestId="synth-backdrop"
        (dismissed)="open.set(false)"
      >
        <button type="button" role="menuitem" data-testid="item-a">SYNTH_A</button>
        <button type="button" role="menuitem" data-testid="item-b">SYNTH_B</button>
        <button type="button" role="menuitem" data-testid="item-c">SYNTH_C</button>
      </qd-context-menu>
    }
  `,
})
class HostComponent {
  readonly open = signal(false);
}

describe('QdContextMenuComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;

  const query = <T extends HTMLElement>(testId: string): T | null =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);

  const invoker = () => query<HTMLButtonElement>('invoker')!;

  async function openFromInvoker(): Promise<void> {
    invoker().focus();
    invoker().click();
    fixture.detectChanges();
    await new Promise((resolve) => setTimeout(resolve));
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
  });

  // The host is appended to document.body by hand, so nothing else takes it back out: destroy
  // first (Angular teardown needs the host still attached), then remove it.
  afterEach(() => {
    fixture?.destroy();
    (fixture?.nativeElement as HTMLElement | undefined)?.remove();
  });

  // F-70: `role="menu"` with no accessible name announces as an unnamed menu.
  it('names the menu in Arabic by default, and lets a consumer override the name', () => {
    fixture.componentInstance.open.set(true);
    fixture.detectChanges();

    expect(query('synth-menu')?.getAttribute('aria-label')).toBe(CONTEXT_MENU_LABELS.menuAriaLabel);
    expect(CONTEXT_MENU_LABELS.menuAriaLabel).toBe('قائمة العمليات');
  });

  // F-70: the projected items sit at the very end of the page DOM, so without this the keyboard
  // open path leaves focus on the tree row and the items are a full page of tabbing away.
  it('moves focus to the first item when it opens', async () => {
    await openFromInvoker();

    expect(document.activeElement).toBe(query('item-a'));
  });

  it('traverses the items with ArrowDown/ArrowUp, wrapping at both ends', async () => {
    await openFromInvoker();
    const menu = query('synth-menu')!;

    menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    expect(document.activeElement).toBe(query('item-b'));

    menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    expect(document.activeElement).toBe(query('item-a'));

    menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    expect(document.activeElement).toBe(query('item-c'));
  });

  it('returns focus to the element that opened it when it closes', async () => {
    await openFromInvoker();
    query<HTMLButtonElement>('item-c')!.focus();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(query('synth-menu')).toBeNull();
    expect(document.activeElement).toBe(invoker());
  });

  // A menu item that opens a modal moves focus itself; the return must not fight it.
  it('leaves focus alone when something else has already claimed it', async () => {
    await openFromInvoker();
    const elsewhere = document.createElement('button');
    document.body.appendChild(elsewhere);
    elsewhere.focus();

    fixture.componentInstance.open.set(false);
    fixture.detectChanges();

    expect(document.activeElement).toBe(elsewhere);
    elsewhere.remove();
  });
});
