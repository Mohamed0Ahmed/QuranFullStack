import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { Component, input } from '@angular/core';

import { QdChipComponent } from './chip.component';

const PROJECTED_LABEL = 'باب العلم بالله';

/** A real host, because the label wrapper is chosen by an `@if` and the only way to catch a
 * broken projection slot is to project something and look for it. */
@Component({
  standalone: true,
  imports: [QdChipComponent],
  template: `<qd-chip
    [removable]="removable()"
    [labelClickable]="labelClickable()"
    [labelAriaLabel]="labelAriaLabel()"
    [removeAriaLabel]="removeAriaLabel()"
    [as]="elementType()"
    [href]="href()"
    (labelClick)="labelClicks.push(1)"
    (remove)="removes.push(1)"
    >{{ projected }}</qd-chip
  >`,
})
class ChipHostComponent {
  readonly removable = input(false);
  readonly labelClickable = input(false);
  readonly labelAriaLabel = input<string | null>(null);
  readonly removeAriaLabel = input<string | null>(null);
  readonly elementType = input<'button' | 'a'>('button');
  readonly href = input<string | null>(null);
  readonly projected = PROJECTED_LABEL;
  readonly labelClicks: number[] = [];
  readonly removes: number[] = [];
}

describe('QdChipComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdChipComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(QdChipComponent);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as { setInput: (name: string, value: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders a button by default', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('button[data-testid="qd-chip"]')).toBeTruthy();
    expect(root.querySelector('a[data-testid="qd-chip"]')).toBeNull();
  });

  it('renders an anchor when as="a"', () => {
    const fixture = render({ as: 'a', href: '/words/roots' });
    const root = fixture.nativeElement as HTMLElement;

    const anchor = root.querySelector('a[data-testid="qd-chip"]') as HTMLAnchorElement;
    expect(anchor).toBeTruthy();
    expect(anchor.getAttribute('href')).toBe('/words/roots');
  });

  it('marks the selected state with qd-is-selected and drives color purely through that class (no inline gold fill)', () => {
    const selected = render({ selected: true });
    const selectedChip = (selected.nativeElement as HTMLElement).querySelector(
      '[data-testid="qd-chip"]',
    ) as HTMLElement;
    expect(selectedChip.classList.contains('qd-is-selected')).toBe(true);

    const unselected = render({ selected: false });
    const unselectedChip = (unselected.nativeElement as HTMLElement).querySelector(
      '[data-testid="qd-chip"]',
    ) as HTMLElement;
    expect(unselectedChip.classList.contains('qd-is-selected')).toBe(false);

    // The component never writes an inline background/color style — the selected
    // visual (tint + accent-text, no solid gold fill) comes entirely from the
    // reviewed `.qd-chip.qd-is-selected` rule in _components.scss, so there is no
    // per-instance way to smuggle in a raw `--qd-accent` fill.
    expect(selectedChip.getAttribute('style')).toBeNull();
  });

  it('renders the trailing count only when provided', () => {
    const withCount = render({ count: 12 });
    expect(
      (withCount.nativeElement as HTMLElement).querySelector('[data-testid="qd-chip-count"]')?.textContent?.trim(),
    ).toBe('12');

    const withoutCount = render();
    expect((withoutCount.nativeElement as HTMLElement).querySelector('[data-testid="qd-chip-count"]')).toBeNull();
  });

  it('disables the button variant and blocks clicks', () => {
    const fixture = render({ disabled: true });
    const emitted: void[] = [];
    fixture.componentInstance.chipClick.subscribe(() => emitted.push(undefined));

    const button = (fixture.nativeElement as HTMLElement).querySelector('button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    button.click();

    expect(emitted).toHaveLength(0);
  });

  it('makes the anchor variant non-interactive when disabled', () => {
    const fixture = render({ as: 'a', href: '/words/roots', disabled: true });
    const emitted: void[] = [];
    fixture.componentInstance.chipClick.subscribe(() => emitted.push(undefined));

    const anchor = (fixture.nativeElement as HTMLElement).querySelector('a') as HTMLAnchorElement;
    expect(anchor.getAttribute('href')).toBeNull();
    expect(anchor.getAttribute('aria-disabled')).toBe('true');
    expect(anchor.getAttribute('tabindex')).toBe('-1');

    anchor.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
    expect(emitted).toHaveLength(0);
  });

  it('emits chipClick when an enabled chip is clicked', () => {
    const fixture = render();
    const emitted: void[] = [];
    fixture.componentInstance.chipClick.subscribe(() => emitted.push(undefined));

    (fixture.nativeElement as HTMLElement).querySelector('button')?.dispatchEvent(new Event('click'));

    expect(emitted).toHaveLength(1);
  });

  describe('removable — the alias-chip remove affordance', () => {
    it('renders a static, non-button wrapper carrying a nested remove button', () => {
      const fixture = render({ removable: true, removeAriaLabel: 'إزالة التوحيد' });
      const root = fixture.nativeElement as HTMLElement;

      const wrapper = root.querySelector('[data-testid="qd-chip"]') as HTMLElement;
      expect(wrapper.tagName).toBe('SPAN');

      const removeButton = root.querySelector('[data-testid="qd-chip-remove"]') as HTMLButtonElement;
      expect(removeButton).toBeTruthy();
      expect(removeButton.tagName).toBe('BUTTON');
      expect(removeButton.getAttribute('aria-label')).toBe('إزالة التوحيد');
    });

    it('emits remove (not chipClick) when the nested button is clicked, and does not bubble to a parent click handler', () => {
      const fixture = render({ removable: true, removeAriaLabel: 'إزالة' });
      const removed: void[] = [];
      const clicked: void[] = [];
      fixture.componentInstance.remove.subscribe(() => removed.push(undefined));
      fixture.componentInstance.chipClick.subscribe(() => clicked.push(undefined));

      const removeButton = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="qd-chip-remove"]',
      ) as HTMLButtonElement;
      removeButton.dispatchEvent(new MouseEvent('click', { bubbles: true }));

      expect(removed).toHaveLength(1);
      expect(clicked).toHaveLength(0);
    });

    it('never writes an inline style on the remove button (tint/hairline only, no solid fill)', () => {
      const fixture = render({ removable: true, removeAriaLabel: 'إزالة' });
      const removeButton = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="qd-chip-remove"]',
      ) as HTMLButtonElement;

      expect(removeButton.getAttribute('style')).toBeNull();
    });
  });

  describe('labelClickable — the label as its own control (Slice D)', () => {
    // Projection is the thing to guard: the label wrapper is chosen by an @if, and two
    // `<ng-content>` slots sharing one selector would leave the opted-in chip's label empty
    // while every element-type assertion still passed.
    function renderWithLabel(overrides: Record<string, unknown>) {
      const fixture = TestBed.createComponent(ChipHostComponent);
      for (const [key, value] of Object.entries(overrides)) {
        (fixture.componentRef as { setInput: (name: string, value: unknown) => void }).setInput(key, value);
      }
      fixture.detectChanges();
      return fixture;
    }


    it('leaves the label a plain span by default, projected content intact', () => {
      const fixture = renderWithLabel({ removable: true, removeAriaLabel: 'إزالة' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="qd-chip-label"]')).toBeNull();
      const label = root.querySelector('.qd-chip__label');
      expect(label?.tagName).toBe('SPAN');
      expect(label?.textContent?.trim()).toBe(PROJECTED_LABEL);
    });

    it('renders the label as a button when opted in, and emits labelClick', () => {
      const fixture = renderWithLabel({
        removable: true,
        removeAriaLabel: 'إزالة',
        labelClickable: true,
        labelAriaLabel: 'إظهار في الشجرة',
      });
      const label = (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="qd-chip-label"]',
      ) as HTMLButtonElement;
      expect(label.tagName).toBe('BUTTON');
      expect(label.getAttribute('aria-label')).toBe('إظهار في الشجرة');
      expect(label.textContent?.trim()).toBe(PROJECTED_LABEL);

      label.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      expect(fixture.componentInstance.labelClicks).toHaveLength(1);
    });

    it('keeps the two controls independent — removing does not emit labelClick and vice versa', () => {
      const fixture = renderWithLabel({
        removable: true,
        removeAriaLabel: 'إزالة',
        labelClickable: true,
      });
      const { labelClicks, removes } = fixture.componentInstance;
      const root = fixture.nativeElement as HTMLElement;
      (root.querySelector('[data-testid="qd-chip-remove"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      expect(removes).toHaveLength(1);
      expect(labelClicks).toHaveLength(0);

      (root.querySelector('[data-testid="qd-chip-label"]') as HTMLElement).dispatchEvent(
        new MouseEvent('click', { bubbles: true }),
      );
      expect(labelClicks).toHaveLength(1);
      expect(removes).toHaveLength(1);
    });

    // A nested button inside the button/anchor branches would be invalid HTML, so the opt-in
    // is ignored there rather than trusted.
    it('ignores the opt-in on the non-removable branches', () => {
      const asButton = renderWithLabel({ labelClickable: true });
      expect((asButton.nativeElement as HTMLElement).querySelector('[data-testid="qd-chip-label"]')).toBeNull();

      const asAnchor = renderWithLabel({ labelClickable: true, elementType: 'a', href: '/x' });
      expect((asAnchor.nativeElement as HTMLElement).querySelector('[data-testid="qd-chip-label"]')).toBeNull();
    });
  });

  // F17: the Angular chip owns the *interactive* variants only. Lifecycle, membership and count
  // badges carry no interaction and are semantic classes, so they must not appear here as variants.
  describe('variant — the interactive chip families', () => {
    it.each([
      ['filter', 'qd-chip--filter'],
      ['taxonomy', 'qd-chip--taxonomy'],
      ['alias', 'qd-chip--alias'],
    ] as const)('resolves the %s variant to exactly one named chip class', (variant, expected) => {
      const fixture = render({ variant });
      const chip = fixture.nativeElement.querySelector('[data-testid="qd-chip"]') as HTMLElement;

      const variantClasses = Array.from(chip.classList).filter(
        (name) => name.startsWith('qd-chip--') && name !== 'qd-chip--pill',
      );
      expect(variantClasses).toEqual([expected]);
    });

    it('adds no variant class by default, so existing call-sites are untouched', () => {
      const fixture = render();
      const chip = fixture.nativeElement.querySelector('[data-testid="qd-chip"]') as HTMLElement;

      expect(Array.from(chip.classList)).toEqual(['qd-chip', 'qd-chip--pill']);
    });
  });
});
