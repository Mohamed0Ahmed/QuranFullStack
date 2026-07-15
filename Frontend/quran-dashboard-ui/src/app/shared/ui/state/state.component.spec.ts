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

  function render(variant: QdStateVariant, message: string) {
    const fixture = TestBed.createComponent(QdStateComponent);
    fixture.componentRef.setInput('variant', variant);
    fixture.componentRef.setInput('message', message);
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

  it('renders error as non-interactive (calm, no interactive controls)', () => {
    const fixture = render('error', 'تعذّر تحميل البيانات');
    const el = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');

    expect(el?.querySelector('button, a, input')).toBeNull();
    // The danger-on-tint background comes from the shared .qd-error-state rule
    // (extended in _components.scss); the component itself sets no inline color.
    expect(el?.getAttribute('style')).toBeNull();
  });

  it('renders only one variant at a time', () => {
    const fixture = render('empty', 'لا توجد نتائج');
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.qd-empty-state, .qd-loading-state, .qd-error-state')).toHaveLength(1);
  });
});
