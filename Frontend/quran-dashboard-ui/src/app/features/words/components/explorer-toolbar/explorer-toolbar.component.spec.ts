import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { ExplorerToolbarComponent } from './explorer-toolbar.component';

@Component({
  standalone: true,
  imports: [ExplorerToolbarComponent],
  template: `
    <qd-explorer-toolbar ariaLabel="مرشحات الجذور">
      <span qdExplorerToolbarPrimary data-testid="primary">بحث</span>
      <span qdExplorerToolbarResult data-testid="result">١٦٤٢</span>
      <span qdExplorerToolbarSecondary data-testid="secondary">ترتيب</span>
      <span qdExplorerToolbarApplied data-testid="applied">مرشح مطبق</span>
    </qd-explorer-toolbar>
  `,
})
class ToolbarHostComponent {}

describe('ExplorerToolbarComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ToolbarHostComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  it('projects the feature-owned controls into stable semantic zones', () => {
    const fixture = TestBed.createComponent(ToolbarHostComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const toolbar = root.querySelector('qd-explorer-toolbar') as HTMLElement;

    expect(toolbar.classList.contains('qd-toolbar')).toBe(true);
    expect(toolbar.classList.contains('qd-toolbar--explorer')).toBe(true);
    expect(toolbar.getAttribute('aria-label')).toBe('مرشحات الجذور');
    expect(root.querySelector('.qd-toolbar__filters [data-testid="primary"]')).toBeTruthy();
    expect(root.querySelector('.qd-toolbar__result [data-testid="result"]')).toBeTruthy();
    expect(root.querySelector('.qd-toolbar__actions [data-testid="secondary"]')).toBeTruthy();
    expect(root.querySelector('.qd-toolbar__applied [data-testid="applied"]')).toBeTruthy();
  });

  it('keeps the applied-summary zone mounted when it has no content', () => {
    const fixture = TestBed.createComponent(ExplorerToolbarComponent);
    fixture.componentRef.setInput('ariaLabel', 'مرشحات الصيغ');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.qd-toolbar__applied')).toBeTruthy();
  });
});
