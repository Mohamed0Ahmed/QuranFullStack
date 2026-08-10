import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';

import { PLACEHOLDER_MESSAGE, PlaceholderPageComponent } from './placeholder-page.component';

function render(titleAr: string): HTMLElement {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [PlaceholderPageComponent],
    providers: [{ provide: ActivatedRoute, useValue: { data: of({ titleAr }) } }],
  });

  const fixture = TestBed.createComponent(PlaceholderPageComponent);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('PlaceholderPageComponent', () => {
  it('renders the route title supplied by the route data', () => {
    const root = render('المتشابهات');

    const heading = root.querySelector<HTMLHeadingElement>('h1[data-testid="placeholder-title"]');
    expect(heading?.textContent?.trim()).toBe('المتشابهات');
  });

  it('aligns the title and the message on one capped-reading axis with no second gutter owner', () => {
    const root = render('التفاسير');

    const shells = root.querySelectorAll('.qd-page-shell');
    expect(shells).toHaveLength(1);
    expect(shells[0].classList).toContain('qd-page-shell--capped-reading');
    expect(root.querySelectorAll('.qd-container, .qd-page-frame, .qd-explorer-frame')).toHaveLength(
      0,
    );

    // Title and message share the single shell, so they cannot drift onto two axes.
    expect(shells[0].querySelector('[data-testid="placeholder-title"]')).not.toBeNull();
    expect(shells[0].querySelector('[data-testid="placeholder-message"]')).not.toBeNull();
  });

  it('states exactly one message through the F12 empty owner and offers no action', () => {
    const root = render('الصوتيات');

    const state = root.querySelector('[data-testid="placeholder-message"]');
    expect(state?.getAttribute('role')).toBe('status');
    expect(state?.textContent?.trim()).toBe(PLACEHOLDER_MESSAGE);
    expect(root.querySelectorAll('[data-testid="placeholder-message"]')).toHaveLength(1);
    expect(root.querySelectorAll('button, a')).toHaveLength(0);
    expect(root.querySelector('qd-state')).toBeNull();
  });
});
