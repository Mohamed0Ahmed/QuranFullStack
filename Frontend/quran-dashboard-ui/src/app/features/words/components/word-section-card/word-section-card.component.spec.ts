import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { WordSectionCardComponent } from './word-section-card.component';

interface CardInputs {
  ordinal: string;
  eyebrow: string;
  title: string;
  description: string;
  route: string;
}

describe('WordSectionCardComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
      teardown: { destroyAfterEach: true },
    });
  });

  function setup(inputs: CardInputs): HTMLElement {
    const fixture = TestBed.createComponent(WordSectionCardComponent);
    fixture.componentRef.setInput('ordinal', inputs.ordinal);
    fixture.componentRef.setInput('eyebrow', inputs.eyebrow);
    fixture.componentRef.setInput('title', inputs.title);
    fixture.componentRef.setInput('description', inputs.description);
    fixture.componentRef.setInput('route', inputs.route);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const ROOTS: CardInputs = {
    ordinal: '٠٢',
    eyebrow: 'أوسع تجميع صرفي',
    title: 'الجذور',
    description: 'النواة التي تخرج منها الأسماء والأفعال جميعًا.',
    route: '/dashboard/words/roots',
  };

  it('renders an active navigable router link carrying the ordinal, eyebrow, title, and description', () => {
    const root = setup(ROOTS);

    const link = root.querySelector('a[data-testid="word-section-card"]') as HTMLAnchorElement;
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain('/dashboard/words/roots');

    expect(root.querySelector('.word-section-card__ordinal')?.textContent?.trim()).toBe(ROOTS.ordinal);
    expect(root.querySelector('.word-section-card__eyebrow')?.textContent?.trim()).toBe(ROOTS.eyebrow);
    expect(root.querySelector('.qd-card-title')?.textContent?.trim()).toBe(ROOTS.title);
    expect(root.querySelector('.word-section-card__desc')?.textContent?.trim()).toBe(ROOTS.description);
  });

  it('marks the decorative ordinal aria-hidden and exposes no coming-soon branch', () => {
    const root = setup(ROOTS);

    expect(root.querySelector('.word-section-card__ordinal')?.getAttribute('aria-hidden')).toBe('true');
    expect(root.querySelector('[data-testid="word-section-coming-soon"]')).toBeNull();
    // Always an active link — never a disabled group.
    expect(root.querySelector('[aria-disabled="true"]')).toBeNull();
  });
});
