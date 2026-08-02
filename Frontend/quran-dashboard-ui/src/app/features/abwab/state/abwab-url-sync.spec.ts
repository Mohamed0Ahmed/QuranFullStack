import { describe, expect, it } from 'vitest';
import { convertToParamMap, ParamMap } from '@angular/router';

import { buildAbwabQueryParams, parseAbwabQueryParams } from './abwab-url-sync';

function params(query: string): ParamMap {
  return convertToParamMap(query ? Object.fromEntries(new URLSearchParams(query)) : {});
}

describe('parseAbwabQueryParams — M26', () => {
  it('fails closed to the documented defaults when every param is absent', () => {
    const parsed = parseAbwabQueryParams(params(''));

    expect(parsed).toEqual({ section: null, view: 'tree', archive: false, door: null, card: null, q: '', modal: null });
  });

  it('reads every valid value verbatim', () => {
    const parsed = parseAbwabQueryParams(params('section=4&view=cards&archive=1&door=9&card=2&q=رحمة&modal=edit'));

    expect(parsed).toEqual({
      section: 4,
      view: 'cards',
      archive: true,
      door: 9,
      card: 2,
      q: 'رحمة',
      modal: { kind: 'edit', closed: false, subjectDoorId: null },
    });
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

describe('parseAbwabQueryParams — the modal key', () => {
  it.each(['create', 'sections'] as const)(
    'reads the door-independent kind %s open and retained, with no door in the URL',
    (kind) => {
      expect(parseAbwabQueryParams(params(`modal=${kind}`)).modal).toEqual({ kind, closed: false, subjectDoorId: null });
      expect(parseAbwabQueryParams(params(`modal=${kind}-closed`)).modal).toEqual({ kind, closed: true, subjectDoorId: null });
    },
  );

  it.each(['child', 'edit', 'move', 'relations'] as const)(
    'reads the door-dependent kind %s open and retained once door parses',
    (kind) => {
      expect(parseAbwabQueryParams(params(`door=9&modal=${kind}`)).modal).toEqual({ kind, closed: false, subjectDoorId: null });
      expect(parseAbwabQueryParams(params(`door=9&modal=${kind}-closed`)).modal).toEqual({ kind, closed: true, subjectDoorId: null });
    },
  );

  it.each(['child', 'edit', 'move', 'relations'] as const)(
    'fails the door-dependent kind %s closed when door is absent or itself invalid',
    (kind) => {
      expect(parseAbwabQueryParams(params(`modal=${kind}`)).modal).toBeNull();
      expect(parseAbwabQueryParams(params(`door=abc&modal=${kind}`)).modal).toBeNull();
      expect(parseAbwabQueryParams(params(`door=0&modal=${kind}-closed`)).modal).toBeNull();
    },
  );

  it('fails closed on an unknown kind, a bare suffix, or a casing variant', () => {
    expect(parseAbwabQueryParams(params('modal=banana')).modal).toBeNull();
    expect(parseAbwabQueryParams(params('modal=-closed')).modal).toBeNull();
    expect(parseAbwabQueryParams(params('modal=Edit&door=9')).modal).toBeNull();
    expect(parseAbwabQueryParams(params('modal=edit-CLOSED&door=9')).modal).toBeNull();
    expect(parseAbwabQueryParams(params('modal=&door=9')).modal).toBeNull();
  });

  it('leaves the other six keys untouched when the modal value is garbage', () => {
    const parsed = parseAbwabQueryParams(params('section=4&door=9&modal=banana'));

    expect(parsed.section).toBe(4);
    expect(parsed.door).toBe(9);
    expect(parsed.modal).toBeNull();
  });

  // ux-slice-l widened the grammar with one id-carrying form, so the negative table below is
  // the fence: every malformed variant must land on null, never on a partial parse.
  describe('the id-carrying retained form, relations-<id>-closed', () => {
    it('parses the carried subject and does NOT need a door of its own', () => {
      // The point of the form: `door=` has moved on (a reveal put the target there), and the
      // key still names the source. So it must parse with no door at all...
      expect(parseAbwabQueryParams(params('modal=relations-17-closed')).modal).toEqual({
        kind: 'relations',
        closed: true,
        subjectDoorId: 17,
      });
      // ...and with a door that disagrees with it.
      expect(parseAbwabQueryParams(params('door=3&modal=relations-17-closed')).modal).toEqual({
        kind: 'relations',
        closed: true,
        subjectDoorId: 17,
      });
    });

    it('round-trips through serialize', () => {
      const built = buildAbwabQueryParams({
        modal: { kind: 'relations', closed: true, subjectDoorId: 17 },
      });

      expect(built).toEqual({ modal: 'relations-17-closed' });
      expect(parseAbwabQueryParams(params(`modal=${built['modal']}`)).modal).toEqual({
        kind: 'relations',
        closed: true,
        subjectDoorId: 17,
      });
    });

    it.each([
      // An open state's subject is always `door=`; an id there would split the modal's subject
      // from the selection, which `canOpen` exists to forbid.
      ['relations-17', 'an id on the open form'],
      // Only the relations modal has a reveal, so only it can be retained with a diverged subject.
      ['edit-17-closed', 'an id on another kind'],
      ['sections-4-closed', 'an id on a door-independent kind'],
      ['relations-0-closed', 'a non-positive id'],
      ['relations--3-closed', 'a negative id'],
      ['relations-x-closed', 'a non-numeric id'],
      ['relations--closed', 'an empty id'],
      ['relations-1.5-closed', 'a fractional id'],
    ])('fails closed on %s (%s)', (value) => {
      expect(parseAbwabQueryParams(params(`door=9&modal=${value}`)).modal).toBeNull();
      expect(parseAbwabQueryParams(params(`modal=${value}`)).modal).toBeNull();
    });
  });
});

describe('buildAbwabQueryParams', () => {
  it('emits only the keys present in the change set, as strings (view/q do not invalidate selection)', () => {
    expect(buildAbwabQueryParams({ view: 'cards' })).toEqual({ view: 'cards' });
    expect(buildAbwabQueryParams({ q: 'رحمة' })).toEqual({ q: 'رحمة' });
  });

  it('clears a key by writing null when the change value is null/blank', () => {
    expect(buildAbwabQueryParams({ section: null })).toEqual({ section: null, door: null, card: null, modal: null });
    expect(buildAbwabQueryParams({ q: '' })).toEqual({ q: null });
  });

  it('archive=1 serializes as the string "1"', () => {
    expect(buildAbwabQueryParams({ archive: true })).toEqual({ archive: '1', door: null, card: null, modal: null });
  });

  it('archive=false clears the key but does not touch door/card (turning archive off restores neither)', () => {
    expect(buildAbwabQueryParams({ archive: false })).toEqual({ archive: null });
  });

  it('switching section invalidates door and card (plan-slice-b.md §4.4)', () => {
    expect(buildAbwabQueryParams({ section: 7 })).toEqual({ section: '7', door: null, card: null, modal: null });
  });

  it('turning archive on invalidates door and card', () => {
    expect(buildAbwabQueryParams({ archive: true })).toEqual({ archive: '1', door: null, card: null, modal: null });
  });

  it('an explicit door/card in the same change as the invalidation wins over the clear', () => {
    expect(buildAbwabQueryParams({ section: 7, door: 3 })).toEqual({
      section: '7',
      door: '3',
      card: null,
      modal: null,
    });
  });
});

describe('buildAbwabQueryParams — the modal key', () => {
  it('serializes an open kind bare and a retained one with the -closed suffix', () => {
    expect(buildAbwabQueryParams({ modal: { kind: 'edit', closed: false, subjectDoorId: null } })).toEqual({ modal: 'edit' });
    expect(buildAbwabQueryParams({ modal: { kind: 'edit', closed: true, subjectDoorId: null } })).toEqual({ modal: 'edit-closed' });
  });

  it('clears the key when the change value is null', () => {
    expect(buildAbwabQueryParams({ modal: null })).toEqual({ modal: null });
  });

  it('folds a door write and a modal write into one patch', () => {
    expect(buildAbwabQueryParams({ door: 5, modal: { kind: 'relations', closed: false, subjectDoorId: null } })).toEqual({
      door: '5',
      modal: 'relations',
    });
  });

  it('an explicit modal in the same change as the invalidation wins over the clear', () => {
    expect(buildAbwabQueryParams({ section: 7, modal: { kind: 'sections', closed: false, subjectDoorId: null } })).toEqual({
      section: '7',
      door: null,
      card: null,
      modal: 'sections',
    });
  });

  it('round-trips every kind through build and back through parse', () => {
    for (const kind of ['create', 'child', 'edit', 'move', 'sections', 'relations'] as const) {
      for (const closed of [false, true]) {
        const built = buildAbwabQueryParams({ modal: { kind, closed, subjectDoorId: null } });
        const parsed = parseAbwabQueryParams(params(`door=9&modal=${built['modal']}`));

        expect(parsed.modal).toEqual({ kind, closed, subjectDoorId: null });
      }
    }
  });
});
