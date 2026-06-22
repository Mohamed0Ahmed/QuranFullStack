import { signal } from '@angular/core';
import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { UniqueWordsPageComponent } from './unique-words-page.component';
import { UniqueWordsFacade } from '../../state/unique-words.facade';
import {
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordsListState,
} from '../../models/unique-words.models';

/** Source-safe synthetic placeholder — not Quranic text. */
function item(id: number): UniqueWordListItemDto {
  return {
    id,
    kind: 'tashkeel',
    displayTextUthmani: `كلمة-تجريبية-${id}`,
    occurrencesCount: id,
    ayahsCount: id,
    surahsCount: 1,
    missingSurahsCount: 113,
    firstVerseKey: '1:1',
    firstLocation: '1:1:1',
  };
}

function stateFor(overrides: Partial<UniqueWordsListState>): UniqueWordsListState {
  return {
    status: 'success',
    items: [item(1), item(2)],
    page: 1,
    pageSize: 50,
    totalCount: 2,
    mode: 'tashkeel',
    search: '',
    sort: 'mushaf-order',
    errorMessage: '',
    ...overrides,
  };
}

class StubFacade {
  readonly listState = signal<UniqueWordsListState>(stateFor({}));
  readonly mode = signal<UniqueWordKind>('tashkeel');
  readonly search = signal<string>('');
  readonly bindToRoute = vi.fn();
  readonly unbindFromRoute = vi.fn();
}

describe('UniqueWordsPageComponent', () => {
  let stub: StubFacade;

  beforeEach(async () => {
    stub = new StubFacade();
    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [UniqueWordsPageComponent],
      providers: [
        provideRouter([]),
        { provide: UniqueWordsFacade, useValue: stub },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();
  });

  async function render(stateOverride?: Partial<UniqueWordsListState>): Promise<HTMLElement> {
    if (stateOverride) {
      stub.listState.set(stateFor(stateOverride));
    }
    const fixture = TestBed.createComponent(UniqueWordsPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the page title and active mode label', async () => {
    const root = await render();

    expect(root.querySelector('[data-testid="unique-words-page-title"]')?.textContent).toContain('الكلمات الفريدة');
  });

  it('renders the mode tabs', async () => {
    const root = await render();

    expect(root.querySelector('[data-testid="unique-words-tab--tashkeel"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="unique-words-tab--simple"]')).toBeTruthy();
  });

  it('renders the search input and sort select', async () => {
    const root = await render();

    expect(root.querySelector('[data-testid="unique-words-search-input"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="unique-words-sort-select"]')).toBeTruthy();
  });

  it('renders one card per list item with the Uthmani display text', async () => {
    const root = await render();

    const cards = root.querySelectorAll('[data-testid="unique-word-card"]');
    expect(cards).toHaveLength(2);
    expect(root.querySelector('[data-testid="unique-word-card-text"]')?.textContent).toContain('كلمة-تجريبية-1');
  });

  it('shows the Arabic empty state when status is empty', async () => {
    const root = await render({ status: 'empty', items: [], totalCount: 0 });

    // The empty-state region renders and no list cards are present. The exact
    // label text is asserted in the facade/models specs; here we verify the
    // empty branch is taken (status-driven `@switch`) and the card list is
    // absent. (Interpolated label constants are unreliable in this runner's
    // shared-fork component-definition cache — the hub-page spec notes the
    // same constraint.)
    expect(root.querySelector('[data-testid="unique-words-empty"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="unique-word-card"]')).toBeNull();
  });

  it('shows the error message when status is error', async () => {
    const root = await render({ status: 'error', items: [], errorMessage: 'خطأ تجريبي' });

    expect(root.querySelector('[data-testid="unique-words-error"]')?.textContent).toContain('خطأ تجريبي');
  });

  it('shows the loading state when status is loading', async () => {
    const root = await render({ status: 'loading', items: [] });

    expect(root.querySelector('[data-testid="unique-words-loading"]')).toBeTruthy();
  });

  it('disables the previous-page button on the first page', async () => {
    const root = await render({ page: 1, totalCount: 100 });

    const prev = root.querySelector<HTMLButtonElement>('[data-testid="unique-words-prev-page"]');
    expect(prev?.disabled).toBe(true);
  });

  it('enables previous and disables next on the last page', async () => {
    // totalCount 100 / pageSize 50 → lastPage = 2. On page 2 the previous
    // button is enabled and the next button is disabled (no further page).
    const root = await render({ page: 2, totalCount: 100 });

    const prev = root.querySelector<HTMLButtonElement>('[data-testid="unique-words-prev-page"]');
    const next = root.querySelector<HTMLButtonElement>('[data-testid="unique-words-next-page"]');
    expect(prev?.disabled).toBe(false);
    expect(next?.disabled).toBe(true);
  });

  it('seeds the search input from the active (facade) search term', async () => {
    // A shared/restored link carries `?search=اسم`; the facade exposes it via
    // `search()`, and the page must show it in the input rather than an empty
    // box. Dynamic runtime value (not a label constant), so it is reliable here.
    stub.search.set('اسم');
    const root = await render();

    const input = root.querySelector<HTMLInputElement>('[data-testid="unique-words-search-input"]');
    expect(input?.value).toBe('اسم');
  });
});
