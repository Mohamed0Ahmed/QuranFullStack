import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import { buildAbwabQueryParams, parseAbwabQueryParams } from './abwab-url-sync';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseAbwabQueryParams — M26', () => {
  it('fails closed to the documented defaults when every param is absent', () => {
    const parsed = parseAbwabQueryParams(params(''));

    expect(parsed).toEqual({ section: null, view: 'tree', archive: false, door: null, card: null, q: '' });
  });

  it('reads every valid value verbatim', () => {
    const parsed = parseAbwabQueryParams(params('section=4&view=cards&archive=1&door=9&card=2&q=رحمة'));

    expect(parsed).toEqual({ section: 4, view: 'cards', archive: true, door: 9, card: 2, q: 'رحمة' });
  });

  it('fails closed on an unknown view instead of throwing', () => {
    expect(parseAbwabQueryParams(params('view=list')).view).toBe('tree');
  });

  it('fails closed on a non-positive or non-numeric section/door/card', () => {
    expect(parseAbwabQueryParams(params('section=-1')).section).toBeNull();
    expect(parseAbwabQueryParams(params('door=abc')).door).toBeNull();
    expect(parseAbwabQueryParams(params('card=0')).card).toBeNull();
  });

  it('treats any archive value other than exactly "1" as the live view', () => {
    expect(parseAbwabQueryParams(params('archive=true')).archive).toBe(false);
    expect(parseAbwabQueryParams(params('archive=0')).archive).toBe(false);
  });
});

describe('buildAbwabQueryParams', () => {
  it('emits only the keys present in the change set, as strings (view/q do not invalidate selection)', () => {
    expect(buildAbwabQueryParams({ view: 'cards' })).toEqual({ view: 'cards' });
    expect(buildAbwabQueryParams({ q: 'رحمة' })).toEqual({ q: 'رحمة' });
  });

  it('clears a key by writing null when the change value is null/blank', () => {
    expect(buildAbwabQueryParams({ section: null })).toEqual({ section: null, door: null, card: null });
    expect(buildAbwabQueryParams({ q: '' })).toEqual({ q: null });
  });

  it('archive=1 serializes as the string "1"', () => {
    expect(buildAbwabQueryParams({ archive: true })).toEqual({ archive: '1', door: null, card: null });
  });

  it('archive=false clears the key but does not touch door/card (turning archive off restores neither)', () => {
    expect(buildAbwabQueryParams({ archive: false })).toEqual({ archive: null });
  });

  it('switching section invalidates door and card (plan-slice-b.md §4.4)', () => {
    expect(buildAbwabQueryParams({ section: 7 })).toEqual({ section: '7', door: null, card: null });
  });

  it('turning archive on invalidates door and card', () => {
    expect(buildAbwabQueryParams({ archive: true })).toEqual({ archive: '1', door: null, card: null });
  });

  it('an explicit door/card in the same change as the invalidation wins over the clear', () => {
    expect(buildAbwabQueryParams({ section: 7, door: 3 })).toEqual({ section: '7', door: '3', card: null });
  });
});
