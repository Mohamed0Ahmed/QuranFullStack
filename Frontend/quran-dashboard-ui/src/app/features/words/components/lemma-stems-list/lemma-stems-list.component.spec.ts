import { afterEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter } from '@angular/router';

import { LemmaStemsListComponent } from './lemma-stems-list.component';
import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';

async function createFixture() {
  await TestBed.configureTestingModule({
    imports: [LemmaStemsListComponent],
    providers: [provideRouter([]), provideLocationMocks()],
    teardown: { destroyAfterEach: true },
  }).compileComponents();

  const fixture = TestBed.createComponent(LemmaStemsListComponent);
  fixture.componentRef.setInput('stems', [
    { stemId: 600, stemText: 'كَلَّمَ', occurrencesCount: 11 },
    { stemId: 604, stemText: 'حَكَمَ', occurrencesCount: 4 },
  ]);
  fixture.detectChanges();
  return fixture;
}

describe('LemmaStemsListComponent US6', () => {
  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('renders in-app overlay anchors with related counts', async () => {
    const fixture = await createFixture();

    const root = fixture.nativeElement as HTMLElement;
    const links = root.querySelectorAll('[data-testid="lemma-stems-list-link"]');

    expect(links).toHaveLength(2);
    expect(links[0].getAttribute('href')).toContain('qdDetail=v1~stem~600~words~simple~mentioned~1~-');
    expect(links[0].getAttribute('href')).toContain('qdDetailOpen=1');
    expect(links[0].getAttribute('target')).toBeNull();
    expect(links[0].getAttribute('rel')).toBeNull();
    expect(links[0].getAttribute('role')).toBe('listitem');
    expect(links[0].closest('[role="list"]')?.classList.contains('qd-result-list--linked')).toBe(true);
    expect(root.textContent).toContain('11');
    expect(root.textContent).toContain('4');
  });

  it('intercepts an unmodified click in-app and leaves a modifier click to the browser', async () => {
    const fixture = await createFixture();
    const startSpy = vi
      .spyOn(TestBed.inject(DetailOverlayHistoryService), 'startStack')
      .mockReturnValue(undefined);
    const link = fixture.nativeElement.querySelector('[data-testid="lemma-stems-list-link"]') as HTMLAnchorElement;

    const plainClick = new MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
    link.dispatchEvent(plainClick);
    expect(plainClick.defaultPrevented).toBe(true);
    expect(startSpy).toHaveBeenCalledWith({
      kind: 'stem',
      id: 600,
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
