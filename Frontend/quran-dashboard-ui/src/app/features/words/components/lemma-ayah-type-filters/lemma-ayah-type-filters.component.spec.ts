import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { LemmaAyahTypeFiltersComponent } from './lemma-ayah-type-filters.component';

const nounType = {
  code: 'N',
  arabicLabel: 'اسم',
  occurrencesCount: 10,
};

const verbType = {
  code: 'V',
  arabicLabel: 'فعل',
  occurrencesCount: 1,
};

const zeroType = {
  code: 'P',
  arabicLabel: 'حرف',
  occurrencesCount: 0,
};

describe('LemmaAyahTypeFiltersComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [LemmaAyahTypeFiltersComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders all filter selected by default and emits typeCode changes', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType, verbType]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-all"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.textContent).toContain('عرض الكل');
    expect(root.textContent).toContain('اسم');
    expect(root.textContent).toContain('10 مرة');

    let emitted: string | null | undefined;
    fixture.componentInstance.typeCodeChange.subscribe((value) => (emitted = value));

    (root.querySelector('[data-testid="lemma-ayah-type-filter-N"]') as HTMLButtonElement).click();
    expect(emitted).toBe('N');
  });

  it('hides عرض الكل and selects the only type when a single type is available', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-all"]')).toBeNull();
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-N"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.textContent).not.toContain('عرض الكل');
  });

  it('does not emit when the only type chip is clicked', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const emitted: (string | null)[] = [];
    fixture.componentInstance.typeCodeChange.subscribe((value) => emitted.push(value));

    (root.querySelector('[data-testid="lemma-ayah-type-filter-N"]') as HTMLButtonElement).click();

    expect(emitted).toEqual([]);
  });

  it('does not emit when the active chip of a multi-type set is clicked', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType, verbType]);
    fixture.componentRef.setInput('selectedTypeCode', 'N');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const emitted: (string | null)[] = [];
    fixture.componentInstance.typeCodeChange.subscribe((value) => emitted.push(value));

    (root.querySelector('[data-testid="lemma-ayah-type-filter-N"]') as HTMLButtonElement).click();

    expect(emitted).toEqual([]);
  });

  it('does not emit when the active عرض الكل chip is clicked', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType, verbType]);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const emitted: (string | null)[] = [];
    fixture.componentInstance.typeCodeChange.subscribe((value) => emitted.push(value));

    (root.querySelector('[data-testid="lemma-ayah-type-filter-all"]') as HTMLButtonElement).click();

    expect(emitted).toEqual([]);
  });

  it('still emits when a non-active chip is clicked', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType, verbType]);
    fixture.componentRef.setInput('selectedTypeCode', 'N');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const emitted: (string | null)[] = [];
    fixture.componentInstance.typeCodeChange.subscribe((value) => emitted.push(value));

    (root.querySelector('[data-testid="lemma-ayah-type-filter-V"]') as HTMLButtonElement).click();
    (root.querySelector('[data-testid="lemma-ayah-type-filter-all"]') as HTMLButtonElement).click();

    expect(emitted).toEqual(['V', null]);
  });

  it('keeps a zero-count type visible, disabled, and linked to the visible reason', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', [nounType, zeroType]);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector('[data-testid="lemma-ayah-type-filter-P"]') as HTMLButtonElement;
    const reasonId = trigger.getAttribute('aria-describedby');
    const emitted: (string | null)[] = [];
    fixture.componentInstance.typeCodeChange.subscribe((value) => emitted.push(value));

    expect(trigger).toBeTruthy();
    expect(trigger.disabled).toBe(true);
    expect(trigger.getAttribute('aria-disabled')).toBe('true');
    expect(trigger.classList.contains('qd-action')).toBe(true);
    expect(reasonId).toBeTruthy();
    const reason = root.querySelector(`#${reasonId}`) as HTMLElement;
    expect(reason).toBeTruthy();
    expect(reason.outerHTML).toContain('لا كلمات مرتبطة بهذا النوع، لذا لا تفاصيل لعرضها.');
    trigger.click();
    expect(emitted).toEqual([]);
  });

  it('shows skeleton chips while loading', () => {
    const fixture = TestBed.createComponent(LemmaAyahTypeFiltersComponent);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[role="status"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="lemma-ayah-type-filter-loading-chip"]')).toHaveLength(4);
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-all"]')).toBeNull();
  });
});
