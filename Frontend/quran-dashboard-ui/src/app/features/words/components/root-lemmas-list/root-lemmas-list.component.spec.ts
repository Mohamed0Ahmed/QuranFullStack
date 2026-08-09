import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { RootLemmasListComponent } from './root-lemmas-list.component';
import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';

async function createFixture() {
  await TestBed.configureTestingModule({
    imports: [RootLemmasListComponent],
    providers: [provideRouter([]), provideLocationMocks()],
  }).compileComponents();

  const fixture = TestBed.createComponent(RootLemmasListComponent);
  fixture.componentRef.setInput('lemmas', [
    { lemmaId: 100, lemmaText: 'صيغة-اختبار', occurrencesCount: 3 },
  ]);
  fixture.detectChanges();
  return fixture;
}

describe('RootLemmasListComponent US5', () => {
  it('renders lemmas as overlay entity anchors with counts', async () => {
    const fixture = await createFixture();

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector('[data-testid="root-lemma-item"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.tagName).toBe('A');
    expect(link?.getAttribute('href')).toContain('qdDetail=v1~lemma~100~words~simple~mentioned~1~-');
    expect(link?.getAttribute('href')).toContain('qdDetailOpen=1');
    expect(link?.getAttribute('target')).toBeNull();
    expect(link?.getAttribute('rel')).toBeNull();
    expect(link?.getAttribute('role')).toBe('listitem');
    expect(link?.closest('[role="list"]')?.classList.contains('qd-result-list--linked')).toBe(true);
    expect(root.textContent).toContain('3');
  });

  it('intercepts an unmodified click in-app and leaves a modifier click to the browser', async () => {
    const fixture = await createFixture();
    const startSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'startStack')
      .mockReturnValue(undefined);
    const link = fixture.nativeElement.querySelector('[data-testid="root-lemma-item"]') as HTMLAnchorElement;

    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);
    expect(plainClick.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 100,
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
