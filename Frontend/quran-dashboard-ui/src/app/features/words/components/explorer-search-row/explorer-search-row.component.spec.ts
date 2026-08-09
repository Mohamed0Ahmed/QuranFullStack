import { Component } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { ExplorerSearchRowComponent } from './explorer-search-row.component';

@Component({
  selector: 'qd-test-projection-host',
  standalone: true,
  imports: [ExplorerSearchRowComponent],
  template: `
    <qd-explorer-search-row
      searchValue=""
      searchLabel="ابحث في الكلمات"
      searchPlaceholder="اكتب كلمة…"
      searchTestid="row-search-input"
    >
      <span data-testid="projected">حقل مرتبط</span>
    </qd-explorer-search-row>
  `,
})
class ProjectionHostComponent {}

describe('ExplorerSearchRowComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ExplorerSearchRowComponent, ProjectionHostComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function render(inputs: Partial<{
    searchValue: string;
    searchLabel: string;
    searchPlaceholder: string;
    searchTestid: string;
    disabled: boolean;
  }> = {}) {
    const fixture = TestBed.createComponent(ExplorerSearchRowComponent);
    fixture.componentRef.setInput('searchValue', inputs.searchValue ?? '');
    fixture.componentRef.setInput('searchLabel', inputs.searchLabel ?? 'ابحث في الجذور');
    fixture.componentRef.setInput('searchPlaceholder', inputs.searchPlaceholder ?? 'اكتب جذرًا…');
    fixture.componentRef.setInput('searchTestid', inputs.searchTestid ?? 'row-search-input');
    if (inputs.disabled !== undefined) fixture.componentRef.setInput('disabled', inputs.disabled);
    fixture.detectChanges();
    return fixture;
  }

  it('renders the main input with the passed testid, placeholder, value, and sr-only label', () => {
    const fixture = render({
      searchValue: 'قرأ',
      searchLabel: 'ابحث في الجذور',
      searchPlaceholder: 'اكتب جذرًا…',
      searchTestid: 'roots-search-input',
    });
    const root = fixture.nativeElement as HTMLElement;

    const input = root.querySelector<HTMLInputElement>('[data-testid="roots-search-input"]');
    expect(input).toBeTruthy();
    expect(input!.type).toBe('search');
    expect(input!.getAttribute('placeholder')).toBe('اكتب جذرًا…');
    expect(input!.value).toBe('قرأ');
    expect(input!.classList.contains('qd-control')).toBe(true);

    const label = root.querySelector('.qd-sr-only');
    expect(label?.textContent).toContain('ابحث في الجذور');
  });

  it('emits searchChange with the raw typed value on input', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    let emitted: string | undefined;
    fixture.componentInstance.searchChange.subscribe((value) => (emitted = value));

    const input = root.querySelector<HTMLInputElement>('[data-testid="row-search-input"]')!;
    input.value = 'كتاب';
    input.dispatchEvent(new Event('input'));

    expect(emitted).toBe('كتاب');
  });

  it('sets role="search" on the host element', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.getAttribute('role')).toBe('search');
  });

  it('disables the main input when disabled is true', () => {
    const fixture = render({ disabled: true });
    const root = fixture.nativeElement as HTMLElement;

    const input = root.querySelector<HTMLInputElement>('[data-testid="row-search-input"]')!;
    expect(input.disabled).toBe(true);
  });

  it('projects association-field content beside the main input', () => {
    const hostFixture = TestBed.createComponent(ProjectionHostComponent);
    hostFixture.detectChanges();
    const root = hostFixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="row-search-input"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="projected"]')).toBeTruthy();
  });
});
