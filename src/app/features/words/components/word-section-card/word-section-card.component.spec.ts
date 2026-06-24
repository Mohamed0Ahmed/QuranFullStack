import { describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { WordSectionCardComponent } from './word-section-card.component';

const COMING_SOON_BADGE = 'قريبًا';

interface CardInputs {
  labelAr: string;
  descriptionAr: string;
  route?: string | null;
  disabled?: boolean;
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
    fixture.componentRef.setInput('labelAr', inputs.labelAr);
    fixture.componentRef.setInput('descriptionAr', inputs.descriptionAr);
    if (inputs.route !== undefined) {
      fixture.componentRef.setInput('route', inputs.route);
    }
    if (inputs.disabled !== undefined) {
      fixture.componentRef.setInput('disabled', inputs.disabled);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the active card as a navigable router link when not disabled', () => {
    const root = setup({
      labelAr: 'الكلمات الفريدة',
      descriptionAr: 'استعراض الكلمات القرآنية الفريدة',
      route: '/dashboard/words/unique',
      disabled: false,
    });

    const link = root.querySelector('a[data-testid="word-section-card"]');
    expect(link).toBeTruthy();

    expect(root.querySelector('[data-testid="word-section-coming-soon"]')).toBeNull();
  });

  it('renders the disabled card as a non-navigable element without a router link', () => {
    const root = setup({
      labelAr: 'الجذور',
      descriptionAr: 'استكشاف جذور الكلمات',
      disabled: true,
    });

    expect(root.querySelector('a')).toBeNull();
    expect(root.querySelector('[data-testid="word-section-card"]')).toBeTruthy();
  });

  it('shows the coming-soon badge only on disabled cards', () => {
    const root = setup({
      labelAr: 'الصيغة المعجمية',
      descriptionAr: 'استكشاف الصيغ المعجمية',
      disabled: true,
    });

    const badge = root.querySelector('[data-testid="word-section-coming-soon"]');
    expect(badge?.textContent).toContain(COMING_SOON_BADGE);
  });

  it('marks the disabled card with aria-disabled and keeps it keyboard-safe', () => {
    const root = setup({
      labelAr: 'الأصل الصرفي',
      descriptionAr: 'استكشاف الأصول الصرفية',
      disabled: true,
    });

    const card = root.querySelector('[data-testid="word-section-card"]') as HTMLElement;
    expect(card.getAttribute('aria-disabled')).toBe('true');

    expect(root.querySelector<HTMLAnchorElement>('a[href]')).toBeNull();
  });
});
