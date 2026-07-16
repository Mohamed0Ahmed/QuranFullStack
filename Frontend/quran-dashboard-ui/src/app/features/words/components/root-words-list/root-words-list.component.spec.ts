import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { RootWordsListComponent } from './root-words-list.component';
import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { RootWordItemDto, RootWordView } from '../../models/roots.models';

function wordItem(uniqueWordId: number, kind: RootWordView): RootWordItemDto {
  return {
    uniqueWordId,
    kind,
    displayText: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
  };
}

async function setup(): Promise<void> {
  await TestBed.configureTestingModule({
    imports: [RootWordsListComponent],
    providers: [provideRouter([]), provideLocationMocks()],
  }).compileComponents();
}

describe('RootWordsListComponent US3', () => {
  it.each([
    { wordView: 'simple' as const, uniqueWordId: 1003 },
    { wordView: 'tashkeel' as const, uniqueWordId: 2003 },
  ])('renders an overlay entity link mirroring the old $wordView deep link', async ({ wordView, uniqueWordId }) => {
    await setup();

    const fixture = TestBed.createComponent(RootWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(uniqueWordId, wordView)],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', wordView);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('[data-testid="root-word-link"]') as HTMLAnchorElement;
    expect(link).toBeTruthy();
    expect(link.getAttribute('href')).toContain(`qdDetail=v1~unique~${wordView}~${uniqueWordId}~ayahs~1`);
    expect(link.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(link.getAttribute('target')).toBeNull();
    expect(link.getAttribute('rel')).toBeNull();
  });

  it('intercepts an unmodified click in-app and leaves a modifier click to the browser', async () => {
    await setup();

    const fixture = TestBed.createComponent(RootWordsListComponent);
    fixture.componentRef.setInput('page', {
      page: 1,
      pageSize: 100,
      totalCount: 1,
      items: [wordItem(1003, 'simple')],
    });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', 'simple');
    fixture.detectChanges();

    const startSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'startStack')
      .mockReturnValue(undefined);
    const link = fixture.nativeElement.querySelector('[data-testid="root-word-link"]') as HTMLAnchorElement;

    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);
    expect(plainClick.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith({ kind: 'unique', mode: 'simple', id: 1003, view: 'ayahs', ayahPage: 1 });

    const modifiedClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ctrlKey: true });
    link.dispatchEvent(modifiedClick);
    expect(modifiedClick.defaultPrevented).toBe(false);
    expect(startSpy).toHaveBeenCalledTimes(1);
  });

  it('keeps column headers and renders skeleton rows while loading', async () => {
    await setup();

    const fixture = TestBed.createComponent(RootWordsListComponent);
    fixture.componentRef.setInput('loading', true);
    fixture.componentRef.setInput('page', { page: 1, pageSize: 100, totalCount: 0, items: [] });
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('wordView', 'simple');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.root-words-list__header')).toBeTruthy();
    expect(root.querySelector('[data-testid="root-words-list-loading"]')).toBeTruthy();
    expect(root.querySelectorAll('.root-words-list__row--loading').length).toBeGreaterThan(0);
    expect(root.querySelector('qd-pagination')).toBeNull();
  });
});
