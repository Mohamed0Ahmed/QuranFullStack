import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { WordsHubPageComponent } from './words-hub-page.component';
import {
  ACTIVE_HUB_SECTION,
  ADDITIONAL_ACTIVE_HUB_SECTIONS,
  COMING_SOON_BADGE,
  COMING_SOON_HUB_SECTIONS,
} from '../../models/unique-words.labels';

describe('WordsHubPageComponent', () => {
  // The hub page renders WordSectionCardComponent which uses RouterLink, so the
  // router must be available even though this page makes no API calls.
  //
  // Assertion strategy: the experimental @angular/build:unit-test runner caches
  // component definitions across sibling specs sharing a test fork, which makes
  // INTERPOLATED text ({{ ... }}) render intermittently empty depending on fork
  // composition (the unique-words-page spec documents the same limitation).
  // Element presence, counts, and data-testid hooks render reliably, so these
  // tests assert on structure and verify the exact spec-locked Arabic wording
  // against the label constants directly — never via interpolated text.
  beforeEach(async () => {
    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      providers: [provideRouter([])],
      teardown: { destroyAfterEach: true },
    }).compileComponents();
  });

  async function createComponent(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(WordsHubPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders exactly one active section card', async () => {
    const root = await createComponent();

    expect(root.querySelectorAll('[data-testid="words-hub-card--active"]')).toHaveLength(1);
  });

  it('renders the disabled coming-soon section cards', async () => {
    const root = await createComponent();

    const disabledCards = root.querySelectorAll('[data-testid="words-hub-card--disabled"]');
    expect(disabledCards).toHaveLength(COMING_SOON_HUB_SECTIONS.length);
    // Feature 015 (FR-047) promoted الجذور from coming-soon to active, so the
    // coming-soon set shrank from four to three.
    expect(COMING_SOON_HUB_SECTIONS.length).toBe(3);
    expect(COMING_SOON_HUB_SECTIONS.map((s) => s.labelAr)).not.toContain('الجذور');
  });

  it('links the Roots Explorer card to the roots route (FR-047)', async () => {
    const root = await createComponent();

    // الجذور is now an additional ACTIVE card linking to /dashboard/words/roots.
    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS.map((s) => s.labelAr)).toContain('الجذور');
    const rootsCard = root.querySelector('[data-testid="words-hub-card--الجذور"]');
    expect(rootsCard).toBeTruthy();
  });

  it('marks every disabled card with a coming-soon badge element', async () => {
    const root = await createComponent();

    const badges = root.querySelectorAll('[data-testid="word-section-coming-soon"]');
    expect(badges).toHaveLength(COMING_SOON_HUB_SECTIONS.length);
  });

  it('renders the hub title and subtitle regions', async () => {
    const root = await createComponent();

    expect(root.querySelector('[data-testid="words-hub-title"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="words-hub-subtitle"]')).toBeTruthy();
  });

  it('uses the spec-locked Arabic hub labels', () => {
    // Guards the locked decisions in spec.md (FR-002 / FR-003) and the routing
    // contract: the active section and coming-soon badge wording must not drift.
    expect(ACTIVE_HUB_SECTION.labelAr).toBe('الكلمات الفريدة');
    expect(COMING_SOON_BADGE).toBe('قريبًا');
  });
});
