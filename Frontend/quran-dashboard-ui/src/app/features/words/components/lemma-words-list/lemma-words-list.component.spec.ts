import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { LemmaWordsListComponent } from './lemma-words-list.component';
import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { LemmaWordItemDto } from '../../models/lemmas.models';

function wordItem(uniqueWordId: number): LemmaWordItemDto {
  return {
    uniqueWordId,
    displayText: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
  };
}

async function setup(): Promise<void> {
  await TestBed.configureTestingModule({
    imports: [LemmaWordsListComponent],
    providers: [provideRouter([]), provideLocationMocks()],
  }).compileComponents();
}

describe('LemmaWordsListComponent', () => {
  it.each([
    { wordView: 'simple' as const, uniqueWordId: 1003 },
    { wordView: 'tashkeel' as const, uniqueWordId: 2003 },
  ])('renders an overlay entity link mirroring the old $wordView deep link', async ({ wordView, uniqueWordId }) => {
    await setup();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(uniqueWordId)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('kind', wordView);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="lemma-word-link"]') as HTMLAnchorElement;
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain(`qdDetail=v1~unique~${wordView}~${uniqueWordId}~ayahs~1`);
    expect(link.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(link.getAttribute('target')).toBeNull();
    expect(link.getAttribute('rel')).toBeNull();
    expect(link.getAttribute('role')).toBe('listitem');
    expect(link.closest('[role="list"]')?.classList.contains('qd-result-list--linked')).toBe(true);
  });

  it('renders without a nested tablist', async () => {
    await setup();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(1)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('kind', 'simple');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="tablist"]')).toBeNull();
  });

  it('renders scoped counts, in-app overlay anchors, and pagination changes', async () => {
    await setup();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 1,
      totalCount: 2,
      items: [wordItem(1003)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('kind', 'simple');
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="lemma-word-link"]') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toContain('qdDetail=v1~unique~simple~1003~ayahs~1');
    expect(link.getAttribute('href')).not.toContain('كلمة');
    expect(link.getAttribute('target')).toBeNull();
    expect(link.getAttribute('rel')).toBeNull();
    expect(fixture.nativeElement.querySelector('.qd-badge')?.textContent?.trim()).toBe('2');

    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((value) => emitted.push(value));

    const nextButton = fixture.nativeElement.querySelector('[data-testid="qd-pagination-next"]') as HTMLButtonElement;
    nextButton?.click();

    expect(emitted).toEqual([2]);
  });

  it('intercepts an unmodified click in-app and leaves a modifier click to the browser', async () => {
    await setup();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(1003)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('kind', 'simple');
    fixture.detectChanges();

    const startSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'startStack')
      .mockReturnValue(undefined);
    const link = fixture.nativeElement.querySelector('[data-testid="lemma-word-link"]') as HTMLAnchorElement;

    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);
    expect(plainClick.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith({ kind: 'unique', mode: 'simple', id: 1003, view: 'ayahs', ayahPage: 1 });

    const modifiedClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ctrlKey: true });
    link.dispatchEvent(modifiedClick);
    expect(modifiedClick.defaultPrevented).toBe(false);
    expect(startSpy).toHaveBeenCalledTimes(1);
  });

  // N3 row 4: the pagination bar unmounted while loading and the panel body sprang up under it.
  it('keeps the pagination slot rendered while loading and once loaded', async () => {
    await setup();

    const fixture = TestBed.createComponent(LemmaWordsListComponent);
    fixture.componentRef.setInput('page', { page: 1, pageSize: 100, totalCount: 1, items: [wordItem(1003)] });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('kind', 'simple');
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;

    const slot = () => host.querySelector('[data-testid="lemma-words-pagination-slot"]');
    expect(slot()).toBeTruthy();
    // While loading the slot is deliberately empty — it reserves the row, it does not render the bar.
    expect(slot()!.querySelector('qd-pagination')).toBeNull();

    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();
    expect(slot()).toBeTruthy();
    expect(slot()!.querySelector('qd-pagination')).toBeTruthy();
  });
});
