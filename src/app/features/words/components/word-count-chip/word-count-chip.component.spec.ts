import { describe, expect, it, beforeEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { WordCountChipComponent } from './word-count-chip.component';

describe('WordCountChipComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [WordCountChipComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  function setup(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(WordCountChipComponent);
    fixture.componentRef.setInput('label', 'الآيات');
    fixture.componentRef.setInput('count', 12);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as { setInput: (name: string, value: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('shows the label by default', () => {
    const fixture = setup();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.word-count-chip__label')?.textContent).toContain('الآيات');
    expect(root.querySelector('.word-count-chip__count')?.textContent).toContain('12');
  });

  it('hides the visible label when showLabel is false but keeps aria-label', () => {
    const fixture = setup({ showLabel: false });
    const root = fixture.nativeElement as HTMLElement;
    const button = root.querySelector('[data-testid="word-count-chip"]') as HTMLButtonElement;

    expect(root.querySelector('.word-count-chip__label')).toBeNull();
    expect(root.querySelector('.word-count-chip__count')?.textContent).toContain('12');
    expect(button.getAttribute('aria-label')).toBe('الآيات: 12');
  });
});
