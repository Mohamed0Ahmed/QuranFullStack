import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdStateComponent, QdStateVariant } from './state.component';

describe('QdStateComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdStateComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(
    variant: QdStateVariant,
    message: string,
    actionLabel: string | null = null,
    reserve = false,
  ) {
    const fixture = TestBed.createComponent(QdStateComponent);
    fixture.componentRef.setInput('variant', variant);
    fixture.componentRef.setInput('message', message);
    fixture.componentRef.setInput('actionLabel', actionLabel);
    fixture.componentRef.setInput('reserve', reserve);
    fixture.detectChanges();
    return fixture;
  }

  it.each([
    ['empty', 'qd-empty-state', 'status', 'لا توجد نتائج'],
    ['loading', 'qd-loading-state', 'status', 'جارٍ التحميل…'],
    ['error', 'qd-error-state', 'alert', 'تعذّر تحميل البيانات'],
  ] as const)('renders the %s variant with the %s backing class and %s role', (variant, cssClass, role, message) => {
    const fixture = render(variant, message);
    const root = fixture.nativeElement as HTMLElement;

    const el = root.querySelector(`.${cssClass}`);
    expect(el).toBeTruthy();
    expect(el?.getAttribute('role')).toBe(role);
    expect(el?.textContent?.trim()).toBe(message);
  });

  it('renders loading as non-interactive with aria-busy and a polite live region', () => {
    const fixture = render('loading', 'جارٍ التحميل…');
    const el = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-loading"]');

    expect(el?.getAttribute('aria-busy')).toBe('true');
    expect(el?.getAttribute('aria-live')).toBe('polite');
    expect(el?.querySelector('button, a, input')).toBeNull();
  });

  it('renders error without an action label as non-interactive (calm, no interactive controls)', () => {
    const fixture = render('error', 'تعذّر تحميل البيانات');
    const el = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');

    expect(el?.querySelector('button, a, input')).toBeNull();
    // The danger-on-tint background comes from the shared .qd-error-state rule
    // (extended in _components.scss); the component itself sets no inline color.
    expect(el?.getAttribute('style')).toBeNull();
  });

  it('offers the single recovery action when an error supplies an action label', () => {
    const fixture = render('error', 'تعذّر تحميل البيانات', 'إعادة المحاولة');
    const el = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');
    const buttons = el?.querySelectorAll('button');

    expect(buttons).toHaveLength(1);
    expect(buttons?.[0].textContent?.trim()).toBe('إعادة المحاولة');
    expect(el?.getAttribute('role')).toBe('alert');
  });

  it('emits action when the recovery control is activated', () => {
    const fixture = render('error', 'تعذّر تحميل البيانات', 'إعادة المحاولة');
    const emitted: number[] = [];
    fixture.componentInstance.action.subscribe(() => emitted.push(1));

    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('[data-testid="qd-state-action"]')
      ?.click();

    expect(emitted).toHaveLength(1);
  });

  it.each(['loading', 'empty'] as const)('never renders an action for the %s variant', (variant) => {
    const fixture = render(variant, 'رسالة', 'إعادة المحاولة');
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="qd-state-action"]')).toBeNull();
  });

  it('renders only one variant at a time', () => {
    const fixture = render('empty', 'لا توجد نتائج');
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.qd-empty-state, .qd-loading-state, .qd-error-state')).toHaveLength(1);
  });

  it('leaves `reserve` off by default, so the seven existing call-sites stay untouched', () => {
    const fixture = render('empty', 'لا توجد نتائج');
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.qd-state--reserve')).toBeNull();
  });

  it('keeps the box mounted with `reserve` on even when the message is still empty', () => {
    const fixture = render('empty', '', null, true);
    const root = fixture.nativeElement as HTMLElement;

    const box = root.querySelector('[data-testid="qd-state-empty"]');
    expect(box).toBeTruthy();
    expect(box?.classList.contains('qd-state--reserve')).toBe(true);
    expect(box?.querySelector('.qd-state__message')?.textContent).toBe('');
  });

  it('fades the message in under `reserve` only once it is non-empty', () => {
    const empty = render('empty', '', null, true);
    const emptyMessage = (empty.nativeElement as HTMLElement).querySelector('.qd-state__message');
    expect(emptyMessage?.classList.contains('qd-state__message--visible')).toBe(false);

    const filled = render('empty', 'لا توجد نتائج', null, true);
    const filledMessage = (filled.nativeElement as HTMLElement).querySelector('.qd-state__message');
    expect(filledMessage?.classList.contains('qd-state__message--visible')).toBe(true);
  });

  // A reserved error region must EXIST while empty (so text insertion into the live region is
  // announced) yet stay visually quiet: the danger tint belongs to a message, not to the box.
  describe('reserved-empty error region (the genuinely-reserved live-region contract)', () => {
    it('keeps role="alert" in the DOM while empty and marks the box quiet', () => {
      const fixture = render('error', '', null, true);
      const box = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');

      expect(box).toBeTruthy();
      expect(box?.getAttribute('role')).toBe('alert');
      expect(box?.classList.contains('qd-state--reserve')).toBe(true);
      expect(box?.classList.contains('qd-state--reserve-empty')).toBe(true);
      expect(box?.querySelector('.qd-state__message')?.classList.contains('qd-state__message--visible')).toBe(false);
    });

    it('drops the quiet class on the SAME element once the message lands, so the danger box appears', () => {
      const fixture = render('error', '', null, true);
      const before = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');

      fixture.componentRef.setInput('message', 'تعذر تنفيذ العملية.');
      fixture.detectChanges();
      const after = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');

      expect(after).toBe(before);
      expect(after?.classList.contains('qd-state--reserve-empty')).toBe(false);
      expect(after?.classList.contains('qd-state--reserve')).toBe(true);
      expect(after?.querySelector('.qd-state__message')?.classList.contains('qd-state__message--visible')).toBe(true);
      expect(after?.textContent?.trim()).toBe('تعذر تنفيذ العملية.');
    });

    it('never marks a non-reserved error quiet, even with an empty message', () => {
      const fixture = render('error', '');
      const box = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');

      expect(box?.classList.contains('qd-state--reserve-empty')).toBe(false);
    });
  });
});
