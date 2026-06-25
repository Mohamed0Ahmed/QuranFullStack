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

    expect(COMING_SOON_HUB_SECTIONS.length).toBe(2);
    expect(COMING_SOON_HUB_SECTIONS.map((s) => s.labelAr)).not.toContain('الجذور');
    expect(COMING_SOON_HUB_SECTIONS.map((s) => s.labelAr)).not.toContain('الصيغ المعجمية');
  });

  it('links the Roots Explorer card to the roots route (FR-047)', async () => {
    const root = await createComponent();

    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS.map((s) => s.labelAr)).toContain('الجذور');
    const rootsCard = root.querySelector('[data-testid="words-hub-card--الجذور"]');
    expect(rootsCard).toBeTruthy();
  });

  it('links the Lemmas Explorer card to the lemmas route', async () => {
    const root = await createComponent();

    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS.map((s) => s.labelAr)).toContain('الصيغ المعجمية');
    const lemmasCard = root.querySelector('[data-testid="words-hub-card--الصيغ المعجمية"]');
    expect(lemmasCard).toBeTruthy();
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

    expect(ACTIVE_HUB_SECTION.labelAr).toBe('الكلمات الفريدة');
    expect(COMING_SOON_BADGE).toBe('قريبًا');
  });
});
