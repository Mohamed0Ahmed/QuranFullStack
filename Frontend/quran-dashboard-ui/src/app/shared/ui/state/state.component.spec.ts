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

  function render(variant: QdStateVariant, message: string, actionLabel: string | null = null) {
    const fixture = TestBed.createComponent(QdStateComponent);
    fixture.componentRef.setInput('variant', variant);
    fixture.componentRef.setInput('message', message);
    fixture.componentRef.setInput('actionLabel', actionLabel);
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
});
