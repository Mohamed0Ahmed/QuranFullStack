import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { RootStemsListComponent } from './root-stems-list.component';
import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';

async function createFixture() {
  await TestBed.configureTestingModule({
    imports: [RootStemsListComponent],
    providers: [provideRouter([]), provideLocationMocks()],
  }).compileComponents();

  const fixture = TestBed.createComponent(RootStemsListComponent);
  fixture.componentRef.setInput('stems', [
    { stemId: 200, stemText: 'أصل-اختبار', occurrencesCount: 2 },
  ]);
  fixture.detectChanges();
  return fixture;
}

describe('RootStemsListComponent US5', () => {
  it('renders stems as overlay entity anchors with counts', async () => {
    const fixture = await createFixture();

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector('[data-testid="root-stem-item"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.tagName).toBe('A');
    expect(link?.getAttribute('href')).toContain('qdDetail=v1~stem~200~words~simple~mentioned~1~-');
    expect(link?.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(link?.getAttribute('target')).toBeNull();
    expect(link?.getAttribute('rel')).toBeNull();
    expect(root.textContent).toContain('2');
  });

  it('intercepts an unmodified click in-app and leaves a modifier click to the browser', async () => {
    const fixture = await createFixture();
    const startSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'startStack')
      .mockReturnValue(undefined);
    const link = fixture.nativeElement.querySelector('[data-testid="root-stem-item"]') as HTMLAnchorElement;

    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);
    expect(plainClick.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith({
      kind: 'stem',
      id: 200,
      view: 'words',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    });

    const modifiedClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ctrlKey: true });
    link.dispatchEvent(modifiedClick);
    expect(modifiedClick.defaultPrevented).toBe(false);
    expect(startSpy).toHaveBeenCalledTimes(1);
  });
});
