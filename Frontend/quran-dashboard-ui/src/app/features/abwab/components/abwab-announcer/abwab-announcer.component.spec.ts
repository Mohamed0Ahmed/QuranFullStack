import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { AbwabAnnouncerComponent } from './abwab-announcer.component';

function render(message: string | null) {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({ imports: [AbwabAnnouncerComponent] });
  const fixture = TestBed.createComponent(AbwabAnnouncerComponent);
  fixture.componentRef.setInput('message', message);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

// M32: messages land in one aria-live="polite" region.
describe('AbwabAnnouncerComponent', () => {
  it('renders a single role="status" aria-live="polite" region', () => {
    const root = render('تم حفظ الباب');
    const regions = root.querySelectorAll('[data-testid="abwab-announcer"]');

    expect(regions).toHaveLength(1);
    expect(regions[0].getAttribute('role')).toBe('status');
    expect(regions[0].getAttribute('aria-live')).toBe('polite');
    expect(regions[0].textContent?.trim()).toBe('تم حفظ الباب');
  });

  it('keeps the region mounted with empty text when there is no message, rather than removing it', () => {
    const root = render(null);
    const region = root.querySelector('[data-testid="abwab-announcer"]');

    expect(region).toBeTruthy();
    expect(region?.textContent?.trim()).toBe('');
  });
});
