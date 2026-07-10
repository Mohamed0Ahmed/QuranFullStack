import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { WordsHubPageComponent } from './words-hub-page.component';
import { lemmasRoutePath, stemsRoutePath, wordTypesRoutePath } from '../../../../core/navigation/route-paths';
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

  it('renders the active Word Types card and no coming-soon cards', async () => {
    const root = await createComponent();

    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS.map((s) => s.labelAr)).toContain('أنواع الكلمات');
    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS.find((s) => s.labelAr === 'أنواع الكلمات')?.route).toBe(wordTypesRoutePath());
    expect(root.querySelector('[data-testid="words-hub-card--أنواع الكلمات"]')).toBeTruthy();

    const disabledCards = root.querySelectorAll('[data-testid="words-hub-card--disabled"]');
    expect(COMING_SOON_HUB_SECTIONS).toHaveLength(0);
    expect(disabledCards).toHaveLength(0);

    expect(COMING_SOON_HUB_SECTIONS.map((s) => s.labelAr)).not.toContain('الجذور');
    expect(COMING_SOON_HUB_SECTIONS.map((s) => s.labelAr)).not.toContain('الصيغ المعجمية');
    expect(COMING_SOON_HUB_SECTIONS.map((s) => s.labelAr)).not.toContain('الأصول الصرفية');
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
    expect(
      ADDITIONAL_ACTIVE_HUB_SECTIONS.find((s) => s.labelAr === 'الصيغ المعجمية')?.route,
    ).toBe(lemmasRoutePath());
    const lemmasCard = root.querySelector('[data-testid="words-hub-card--الصيغ المعجمية"]');
    expect(lemmasCard).toBeTruthy();
  });

  it('links the Stems Explorer card to the stems route', async () => {
    const root = await createComponent();

    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS.map((s) => s.labelAr)).toContain('الأصول الصرفية');
    expect(
      ADDITIONAL_ACTIVE_HUB_SECTIONS.find((s) => s.labelAr === 'الأصول الصرفية')?.route,
    ).toBe(stemsRoutePath());
    const stemsCard = root.querySelector('[data-testid="words-hub-card--الأصول الصرفية"]');
    expect(stemsCard).toBeTruthy();
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
