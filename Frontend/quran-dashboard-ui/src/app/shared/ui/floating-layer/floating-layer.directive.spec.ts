import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { QdFloatingLayerDirective, QdFloatingLayerDismissReason, QdFloatingLayerVariant } from './floating-layer.directive';

@Component({
  standalone: true,
  imports: [QdFloatingLayerDirective],
  template: `
    <button #triggerRef type="button" data-testid="trigger" (click)="open.set(true)">SYNTH_TRIGGER</button>
    <button type="button" data-testid="outside">SYNTH_OUTSIDE</button>
    @if (open()) {
      <div
        [qdFloatingLayer]="variant()"
        role="menu"
        [anchorElement]="triggerRef"
        (dismissed)="reasons.push($event)"
      >
        @for (item of items(); track item) {
          <button
            type="button"
            role="menuitem"
            tabindex="-1"
            [attr.data-testid]="'item-' + item"
            [attr.aria-disabled]="item === 'disabled' ? 'true' : null"
          >
            {{ item }}
          </button>
        }
      </div>
    }
  `,
})
class HostComponent {
  readonly open = signal(false);
  readonly variant = signal<QdFloatingLayerVariant>('action-menu');
  readonly items = signal(['alpha', 'beta', 'disabled', 'beacon', 'gamma']);
  readonly reasons: QdFloatingLayerDismissReason[] = [];
}

@Component({
  standalone: true,
  imports: [QdFloatingLayerDirective],
  template: `
    <button #pickerTrigger type="button" data-testid="picker-trigger">SYNTH_PICKER_TRIGGER</button>
    @if (open()) {
      <div
        [qdFloatingLayer]="variant()"
        role="listbox"
        [anchorElement]="pickerTrigger"
        (dismissed)="reasons.push($event)"
      >
        @if (variant() === 'searchable-picker') {
          <input type="search" role="combobox" aria-expanded="true" data-testid="picker-search" />
        }
        @for (item of items(); track item) {
          <div
            role="option"
            [attr.data-testid]="'option-' + item"
            [attr.aria-selected]="item === selected() ? 'true' : 'false'"
          >
            {{ item }}
          </div>
        }
      </div>
    }
  `,
})
class PickerHostComponent {
  readonly open = signal(false);
  readonly variant = signal<QdFloatingLayerVariant>('searchable-picker');
  readonly items = signal(['alef', 'baa', 'taa']);
  readonly selected = signal<string | null>(null);
  readonly reasons: QdFloatingLayerDismissReason[] = [];
}

describe('QdFloatingLayerDirective', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<HostComponent>>;
  let host: HostComponent;

  const query = <T extends HTMLElement>(testId: string): T =>
    fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);

  const layer = () => fixture.nativeElement.querySelector('.qd-floating-layer') as HTMLElement;

  const press = (key: string, options: KeyboardEventInit = {}) => {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...options });
    const active = document.activeElement;
    const from = active instanceof HTMLElement && layer().contains(active) ? active : layer();
    from.dispatchEvent(event);
    fixture.detectChanges();
    return event;
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    query('trigger').focus();
    host.open.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('takes keyboard entry on its first enabled item', () => {
    expect(document.activeElement).toBe(query('item-alpha'));
  });

  // D33: one key script for every menu and picker. The disabled item is the interesting part —
  // arrow navigation must step over it rather than park focus on something that cannot be chosen.
  it('walks enabled items with ArrowDown/ArrowUp, wrapping and skipping disabled items', () => {
    press('ArrowDown');
    expect(document.activeElement).toBe(query('item-beta'));

    press('ArrowDown');
    expect(document.activeElement).toBe(query('item-beacon'));

    press('ArrowDown');
    expect(document.activeElement).toBe(query('item-gamma'));

    press('ArrowDown');
    expect(document.activeElement).toBe(query('item-alpha'));

    press('ArrowUp');
    expect(document.activeElement).toBe(query('item-gamma'));
  });

  it('jumps to the first and last enabled item with Home and End', () => {
    press('End');
    expect(document.activeElement).toBe(query('item-gamma'));

    press('Home');
    expect(document.activeElement).toBe(query('item-alpha'));
  });

  // Type-ahead accumulates inside its window: pressing b then e must reach the "be…" item further
  // down the list, not bounce between the two items that merely start with b.
  it('moves forward to the item matching the accumulated typed prefix', () => {
    press('b');
    expect(document.activeElement).toBe(query('item-beta'));

    press('e');
    expect(document.activeElement).toBe(query('item-beacon'));
  });

  // APG: Space extends a type-ahead string that is already being typed, and otherwise belongs to the
  // item under focus. Swallowing it would turn "activate this menu item" into "jump somewhere else".
  it('leaves Space to the focused item when no type-ahead string is in progress', () => {
    const event = press(' ');

    expect(event.defaultPrevented).toBe(false);
    expect(document.activeElement).toBe(query('item-alpha'));
  });

  it('extends an in-progress type-ahead with Space instead of matching every item', () => {
    host.items.set(['a one', 'a two']);
    fixture.detectChanges();

    press('a');
    expect(document.activeElement).toBe(query('item-a one'));

    const spaced = press(' ');
    expect(spaced.defaultPrevented).toBe(true);

    press('o');
    expect(document.activeElement).toBe(query('item-a one'));
  });

  it('closes on Escape and returns focus to the anchor', () => {
    const event = press('Escape');

    expect(event.defaultPrevented).toBe(true);
    expect(host.reasons).toEqual(['escape']);
    expect(document.activeElement).toBe(query('trigger'));
  });

  // Tab must not be swallowed: the layer closes and the browser continues the tab sequence from the
  // trigger, which is where a keyboard user expects to be.
  it('closes on Tab without preventing the default move, returning focus to the anchor first', () => {
    const event = press('Tab');

    expect(event.defaultPrevented).toBe(false);
    expect(host.reasons).toEqual(['tab']);
    expect(document.activeElement).toBe(query('trigger'));
  });

  it('closes on a pointer press outside the layer and its anchor, and not on one inside either', () => {
    query('item-alpha').dispatchEvent(new Event('pointerdown', { bubbles: true }));
    query('trigger').dispatchEvent(new Event('pointerdown', { bubbles: true }));
    expect(host.reasons).toEqual([]);

    query('outside').dispatchEvent(new Event('pointerdown', { bubbles: true }));
    expect(host.reasons).toEqual(['outside']);
  });

  it('positions itself as a fixed layer so it never joins document flow', () => {
    expect(layer().style.position).toBe('fixed');
    expect(layer().dataset['qdFloatingBlockSide']).toBeTruthy();
    expect(layer().style.maxBlockSize).toMatch(/px$/);
  });

  // F15 §16: a searchable picker keeps DOM focus in its field and moves an `aria-activedescendant`
  // cursor; an action menu moves real focus. Running both models at once leaves the field unusable.
  describe('option models', () => {
    let picker: ReturnType<typeof TestBed.createComponent<PickerHostComponent>>;
    let pickerHost: PickerHostComponent;

    const pick = <T extends HTMLElement>(testId: string): T =>
      picker.nativeElement.querySelector(`[data-testid="${testId}"]`);

    const pickerLayer = () => picker.nativeElement.querySelector('.qd-floating-layer') as HTMLElement;

    const pressOn = (from: HTMLElement, key: string) => {
      const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
      from.dispatchEvent(event);
      picker.detectChanges();
      return event;
    };

    const openPicker = async (variant: QdFloatingLayerVariant) => {
      await TestBed.resetTestingModule();
      await TestBed.configureTestingModule({ imports: [PickerHostComponent] }).compileComponents();
      picker = TestBed.createComponent(PickerHostComponent);
      pickerHost = picker.componentInstance;
      pickerHost.variant.set(variant);
      picker.detectChanges();
      pick('picker-trigger').focus();
      pickerHost.open.set(true);
      picker.detectChanges();
      await picker.whenStable();
    };

    it('takes keyboard entry on the search field, not on the first option', async () => {
      await openPicker('searchable-picker');

      const field = pick('picker-search');
      expect(document.activeElement).toBe(field);
      expect(field.getAttribute('aria-activedescendant')).toBe(pick('option-alef').id);
    });

    it('moves the option cursor with the arrows while focus stays in the field', async () => {
      await openPicker('searchable-picker');
      const field = pick('picker-search');

      pressOn(field, 'ArrowDown');

      expect(document.activeElement).toBe(field);
      expect(field.getAttribute('aria-activedescendant')).toBe(pick('option-baa').id);
      expect(pick('option-baa').id).not.toBe('');
    });

    it('leaves printable keys and Home/End to the field so the caret and the query still work', async () => {
      await openPicker('searchable-picker');
      const field = pick('picker-search');
      const cursorBefore = field.getAttribute('aria-activedescendant');

      const typed = pressOn(field, 'b');
      const home = pressOn(field, 'Home');
      const end = pressOn(field, 'End');

      expect([typed.defaultPrevented, home.defaultPrevented, end.defaultPrevented]).toEqual([
        false,
        false,
        false,
      ]);
      expect(document.activeElement).toBe(field);
      expect(field.getAttribute('aria-activedescendant')).toBe(cursorBefore);
    });

    it('drives a select listbox by activedescendant without moving focus onto an option', async () => {
      await openPicker('select-listbox');

      expect(document.activeElement).toBe(pickerLayer());
      expect(pickerLayer().getAttribute('aria-activedescendant')).toBe(pick('option-alef').id);

      pressOn(pickerLayer(), 'End');

      expect(document.activeElement).toBe(pickerLayer());
      expect(pickerLayer().getAttribute('aria-activedescendant')).toBe(pick('option-taa').id);
    });

    it('drops the activedescendant cursor when the layer closes', async () => {
      await openPicker('select-listbox');
      expect(pickerLayer().hasAttribute('aria-activedescendant')).toBe(true);

      pressOn(pickerLayer(), 'Escape');

      expect(pickerHost.reasons).toEqual(['escape']);
      expect(pickerLayer().hasAttribute('aria-activedescendant')).toBe(false);
    });

    it('never puts an activedescendant cursor on an action menu, which roves real focus', () => {
      expect(document.activeElement).toBe(query('item-alpha'));
      expect(layer().hasAttribute('aria-activedescendant')).toBe(false);

      press('ArrowDown');

      expect(document.activeElement).toBe(query('item-beta'));
      expect(layer().hasAttribute('aria-activedescendant')).toBe(false);
    });
  });

  describe('non-navigable variants', () => {
    it.each(['disclosure-popover', 'tooltip'] as const)(
      'leaves arrow keys alone for the %s variant but still closes on Escape',
      async (variant) => {
        host.open.set(false);
        fixture.detectChanges();
        host.variant.set(variant);
        query('trigger').focus();
        host.open.set(true);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(document.activeElement).toBe(query('trigger'));

        const arrow = press('ArrowDown');
        expect(arrow.defaultPrevented).toBe(false);

        press('Escape');
        expect(host.reasons).toEqual(['escape']);
      },
    );
  });
});
