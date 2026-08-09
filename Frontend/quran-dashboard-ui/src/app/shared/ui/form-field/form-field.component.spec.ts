import { Component, signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdControlDirective } from './control.directive';
import { QdFormFieldComponent } from './form-field.component';

@Component({
  selector: 'qd-test-form-field-host',
  standalone: true,
  imports: [QdFormFieldComponent, QdControlDirective],
  template: `
    <qd-form-field
      label="اسم الباب"
      [helper]="helper()"
      [error]="error()"
      [required]="required()"
      data-testid="field-a"
    >
      <input qdControl type="text" data-testid="control-a" />
    </qd-form-field>

    <qd-form-field label="القسم" helper="اختر القسم" data-testid="field-b">
      <select qdControl data-testid="control-b"><option>أ</option></select>
    </qd-form-field>

    <input qdControl type="search" data-testid="orphan" [invalid]="orphanInvalid()" />
  `,
})
class FormFieldHostComponent {
  readonly helper = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly required = signal(false);
  readonly orphanInvalid = signal<boolean | undefined>(undefined);
}

describe('QdFormFieldComponent + QdControlDirective', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [FormFieldHostComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render() {
    const fixture = TestBed.createComponent(FormFieldHostComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    return {
      fixture,
      host: fixture.componentInstance,
      el: (testId: string) => root.querySelector(`[data-testid="${testId}"]`) as HTMLElement,
      root,
    };
  }

  it('labels the projected control through a generated id the call-site never writes', () => {
    const { el } = render();
    const control = el('control-a');
    const label = el('field-a').querySelector('label') as HTMLLabelElement;

    expect(control.id).toBeTruthy();
    expect(label.getAttribute('for')).toBe(control.id);
    expect(label.textContent?.trim()).toContain('اسم الباب');
  });

  it('gives two fields on one page distinct control, helper and error ids', () => {
    const { fixture, host, el } = render();
    host.helper.set('حروف عربية فقط');
    host.error.set('الاسم مطلوب');
    fixture.detectChanges();

    const first = el('control-a');
    const second = el('control-b');

    expect(first.id).not.toBe(second.id);
    expect(first.getAttribute('aria-describedby')).not.toBe(
      second.getAttribute('aria-describedby'),
    );
  });

  it('describes the control by helper and error together, in that order', () => {
    const { fixture, host, el } = render();
    host.helper.set('حروف عربية فقط');
    host.error.set('الاسم مطلوب');
    fixture.detectChanges();

    const field = el('field-a');
    const helperId = field.querySelector('.qd-field__helper')?.id;
    const errorId = field.querySelector('.qd-field__error')?.id;

    expect(el('control-a').getAttribute('aria-describedby')).toBe(`${helperId} ${errorId}`);
  });

  it('marks the control invalid only while an error is present, and never by colour alone', () => {
    const { fixture, host, el } = render();

    expect(el('control-a').getAttribute('aria-invalid')).toBeNull();
    expect(el('field-a').querySelector('.qd-field__error')).toBeNull();

    host.error.set('الاسم مطلوب');
    fixture.detectChanges();

    expect(el('control-a').getAttribute('aria-invalid')).toBe('true');
    expect(el('field-a').querySelector('.qd-field__error')?.textContent?.trim()).toBe(
      'الاسم مطلوب',
    );

    host.error.set(null);
    fixture.detectChanges();

    expect(el('control-a').getAttribute('aria-invalid')).toBeNull();
    expect(el('field-a').querySelector('.qd-field__error')).toBeNull();
  });

  it('states "required" in text as well as with the asterisk glyph', () => {
    const { fixture, host, el } = render();
    host.required.set(true);
    fixture.detectChanges();

    const label = el('field-a').querySelector('label') as HTMLLabelElement;
    expect(label.querySelector('.qd-field__required')?.getAttribute('aria-hidden')).toBe('true');
    expect(label.querySelector('.qd-sr-only')?.textContent?.trim()).toBe('مطلوب');
  });

  it('leaves a control outside a field unowned: no borrowed id or description', () => {
    const { fixture, host, el } = render();
    const orphan = el('orphan');

    expect(orphan.classList.contains('qd-control')).toBe(true);
    expect(orphan.getAttribute('id')).toBeNull();
    expect(orphan.getAttribute('aria-describedby')).toBeNull();
    expect(orphan.getAttribute('aria-invalid')).toBeNull();

    host.orphanInvalid.set(true);
    fixture.detectChanges();

    expect(orphan.getAttribute('aria-invalid')).toBe('true');
  });
});
