import { afterEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { StemLemmasListComponent } from './stem-lemmas-list.component';
import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';

async function createFixture() {
  await TestBed.configureTestingModule({
    imports: [StemLemmasListComponent],
    providers: [provideRouter([]), provideLocationMocks()],
    teardown: { destroyAfterEach: true },
  }).compileComponents();

  const fixture = TestBed.createComponent(StemLemmasListComponent);
  fixture.componentRef.setInput('lemmas', [
    { lemmaId: 502, lemmaText: 'عِلْم', occurrencesCount: 3 },
    { lemmaId: 504, lemmaText: 'مَعْرِفَة', occurrencesCount: 1 },
  ]);
  fixture.detectChanges();
  return fixture;
}

describe('StemLemmasListComponent US6', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders in-app overlay anchors and lemma text', async () => {
    const fixture = await createFixture();

    const root = fixture.nativeElement as HTMLElement;
    const links = root.querySelectorAll('[data-testid="stem-lemmas-list-link"]');

    expect(links).toHaveLength(2);
    expect(links[0].getAttribute('href')).toContain('qdDetail=v1~lemma~502~words~simple~mentioned~1~-');
    expect(links[0].getAttribute('href')).toContain('qdDetailOpen=1');
    expect(links[0].getAttribute('target')).toBeNull();
    expect(links[0].getAttribute('rel')).toBeNull();
    expect(root.textContent).toContain('عِلْم');
    expect(root.textContent).toContain('مَعْرِفَة');
    expect(root.textContent).toContain('3');
    expect(root.textContent).toContain('1');
  });

  it('intercepts an unmodified click in-app and leaves a modifier click to the browser', async () => {
    const fixture = await createFixture();
    const startSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'startStack')
      .mockReturnValue(undefined);
    const link = fixture.nativeElement.querySelector('[data-testid="stem-lemmas-list-link"]') as HTMLAnchorElement;

    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);
    expect(plainClick.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 502,
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
