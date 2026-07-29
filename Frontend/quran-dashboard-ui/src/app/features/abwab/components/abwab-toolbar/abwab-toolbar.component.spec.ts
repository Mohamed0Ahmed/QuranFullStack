import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabToolbarComponent } from './abwab-toolbar.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';

const SECTIONS: AbwabTreeSectionDto[] = [
  { id: 1, name: 'اللغة العربية', orderValue: 1, version: 1, doorsInScopeCount: 5 },
  { id: 2, name: 'العلوم الحديثة', orderValue: 2, version: 1, doorsInScopeCount: 3 },
];

function render(overrides: Record<string, unknown> = {}) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabToolbarComponent] });
  const fixture = TestBed.createComponent(AbwabToolbarComponent);
  fixture.componentRef.setInput('sections', SECTIONS);
  fixture.componentRef.setInput('activeSectionId', null);
  for (const [key, value] of Object.entries(overrides)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AbwabToolbarComponent', () => {
  it('renders «كل الأبواب» plus one tab per section, and no «الأبواب الرئيسية» tab', () => {
    const fixture = render();
    const root = fixture.nativeElement as HTMLElement;
    const labels = Array.from(root.querySelectorAll('[role="tab"]')).map((el) => el.textContent?.trim());

    expect(labels).toEqual(['كل الأبواب', 'اللغة العربية', 'العلوم الحديثة']);
    expect(labels).not.toContain('الأبواب الرئيسية');
  });

  it('marks the active section tab as selected', () => {
    const fixture = render({ activeSectionId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    const selected = root.querySelector('[aria-selected="true"]');
    expect(selected?.textContent?.trim()).toBe('العلوم الحديثة');
  });

  it('emits sectionChanged with the section id, or null for «كل الأبواب»', () => {
    const fixture = render();
    const changes: (number | null)[] = [];
    fixture.componentInstance.sectionChanged.subscribe((id: number | null) => changes.push(id));

    const root = fixture.nativeElement as HTMLElement;
    const tabs = Array.from(root.querySelectorAll('[role="tab"]')) as HTMLElement[];
    tabs[1].click();
    tabs[0].click();

    expect(changes).toEqual([1, null]);
  });
});
