import { Component, signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdActionDirective, QdActionSize, QdActionVariant } from './action.directive';

@Component({
  selector: 'qd-test-action-host',
  standalone: true,
  imports: [QdActionDirective],
  template: `
    <button
      type="button"
      class="feature-local-class"
      [qdAction]="variant()"
      [size]="size()"
      [busy]="busy()"
      [disabled]="disabled()"
      (click)="clicks.set(clicks() + 1)"
      data-testid="action"
    >
      حفظ
    </button>

    <button type="button" qdAction data-testid="bare">إلغاء</button>

    <a href="/words/roots" qdAction="tertiary" data-testid="link">الجذور</a>
  `,
})
class ActionHostComponent {
  readonly variant = signal<QdActionVariant>('primary');
  readonly size = signal<QdActionSize>('md');
  readonly busy = signal<boolean | undefined>(undefined);
  readonly disabled = signal(false);
  readonly clicks = signal(0);
}

describe('QdActionDirective', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ActionHostComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render() {
    const fixture = TestBed.createComponent(ActionHostComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    return {
      fixture,
      host: fixture.componentInstance,
      el: (testId: string) => root.querySelector(`[data-testid="${testId}"]`) as HTMLElement,
    };
  }

  it.each([
    ['primary', 'qd-action--primary'],
    ['secondary', 'qd-action--secondary'],
    ['tertiary', 'qd-action--tertiary'],
    ['danger', 'qd-action--danger'],
    ['icon-only', 'qd-action--icon-only'],
    ['toolbar', 'qd-action--toolbar'],
    ['row-action', 'qd-action--row-action'],
  ] as const)('maps the %s variant onto %s', (variant, expected) => {
    const { fixture, host, el } = render();
    host.variant.set(variant);
    fixture.detectChanges();

    const button = el('action');
    expect(button.classList.contains('qd-action')).toBe(true);
    expect(button.classList.contains(expected)).toBe(true);
    expect(
      Array.from(button.classList)
        .filter((name) => name.startsWith('qd-action--'))
        .sort(),
    ).toEqual([expected, 'qd-action--md'].sort());
  });

  it.each([
    ['sm', 'qd-action--sm'],
    ['md', 'qd-action--md'],
    ['lg', 'qd-action--lg'],
  ] as const)('maps the %s size onto the %s geometry class', (size, expected) => {
    const { fixture, host, el } = render();
    host.size.set(size);
    fixture.detectChanges();

    expect(el('action').classList.contains(expected)).toBe(true);
  });

  it('falls back to the secondary variant when no variant value is supplied', () => {
    const { el } = render();

    expect(el('bare').classList.contains('qd-action--secondary')).toBe(true);
  });

  it('keeps the classes the call-site already wrote on the element', () => {
    const { el } = render();

    expect(el('action').classList.contains('feature-local-class')).toBe(true);
  });

  // The busy contract is a geometry contract: the icon slot is reserved from the moment a
  // call-site declares the action busy-capable, so switching to busy cannot resize the control.
  it('reserves the busy icon slot for every busy-capable action, in flight or not', () => {
    const { fixture, host, el } = render();
    host.busy.set(false);
    fixture.detectChanges();

    const button = el('action');
    expect(button.classList.contains('qd-action--busy-slot')).toBe(true);
    expect(button.classList.contains('qd-action--busy')).toBe(false);
    expect(button.getAttribute('aria-busy')).toBeNull();

    host.busy.set(true);
    fixture.detectChanges();

    expect(button.classList.contains('qd-action--busy-slot')).toBe(true);
    expect(button.classList.contains('qd-action--busy')).toBe(true);
    expect(button.getAttribute('aria-busy')).toBe('true');
  });

  it('reserves no icon slot for an action that never declares a busy state', () => {
    const { el } = render();

    expect(el('bare').classList.contains('qd-action--busy-slot')).toBe(false);
  });

  it('leaves native disabled with the call-site: the directive neither sets nor clears it', () => {
    const { fixture, host, el } = render();
    const button = el('action') as HTMLButtonElement;
    host.busy.set(true);
    fixture.detectChanges();

    expect(button.disabled).toBe(false);

    host.disabled.set(true);
    fixture.detectChanges();
    button.click();

    expect(button.disabled).toBe(true);
    expect(host.clicks()).toBe(0);
  });

  it('leaves a link a link: no button semantics are grafted onto an anchor', () => {
    const { el } = render();
    const link = el('link') as HTMLAnchorElement;

    expect(link.tagName).toBe('A');
    expect(link.getAttribute('href')).toBe('/words/roots');
    expect(link.getAttribute('type')).toBeNull();
    expect(link.getAttribute('role')).toBeNull();
    expect(link.classList.contains('qd-action--tertiary')).toBe(true);
  });
});
