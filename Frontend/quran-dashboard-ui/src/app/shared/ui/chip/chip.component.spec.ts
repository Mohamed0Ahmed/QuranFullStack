import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdChipComponent } from './chip.component';

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

  describe('removable (plan-slice-b.md T412 — the alias-chip remove affordance)', () => {
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
});
