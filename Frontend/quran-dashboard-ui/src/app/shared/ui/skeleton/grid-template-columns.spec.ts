import { describe, expect, it } from 'vitest';

import { splitGridTemplateColumns } from './grid-template-columns';

describe('splitGridTemplateColumns', () => {
  it.each([
    ['2.5rem 1fr auto', ['2.5rem', '1fr', 'auto']],
    ['2rem minmax(0, 1fr) auto', ['2rem', 'minmax(0, 1fr)', 'auto']],
    ['2rem minmax(0, 1fr) minmax(0, 8rem) auto', ['2rem', 'minmax(0, 1fr)', 'minmax(0, 8rem)', 'auto']],
    ['minmax(0, 1fr)', ['minmax(0, 1fr)']],
    ['  1fr   auto  ', ['1fr', 'auto']],
    ['', []],
    ['   ', []],
  ])('splits %s into %j', (template, expected) => {
    expect(splitGridTemplateColumns(template)).toEqual(expected);
  });
});
