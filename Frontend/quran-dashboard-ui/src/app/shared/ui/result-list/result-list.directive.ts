import { Directive, booleanAttribute, input, numberAttribute } from '@angular/core';

export type QdResultListVariant = 'linked' | 'display-only' | 'master' | 'event' | 'quran-result';

@Directive({
  selector: '[qdResultList]',
  standalone: true,
  host: {
    role: 'list',
    class: 'qd-result-list',
    '[class.qd-result-list--linked]': "listVariant() === 'linked'",
    '[class.qd-result-list--display-only]': "listVariant() === 'display-only'",
    '[class.qd-result-list--master]': "listVariant() === 'master'",
    '[class.qd-result-list--event]': "listVariant() === 'event'",
    '[class.qd-result-list--quran-result]': "listVariant() === 'quran-result'",
    '[attr.aria-label]': 'listLabel()',
  },
})
export class QdResultListDirective {
  readonly listVariant = input<QdResultListVariant>('display-only');
  readonly listLabel = input<string | null>(null);
}

@Directive({
  selector: '[qdResultItem]',
  standalone: true,
  host: {
    role: 'listitem',
    class: 'qd-result-item',
    '[class.qd-result-item--selectable]': 'selectable()',
    '[class.qd-is-selected]': 'selected()',
    '[attr.aria-current]': "current() ? 'true' : null",
    '[attr.aria-posinset]': 'position()',
    '[attr.aria-setsize]': 'setSize()',
  },
})
export class QdResultItemDirective {
  readonly selected = input(false, { transform: booleanAttribute });
  readonly current = input(false, { transform: booleanAttribute });
  readonly selectable = input(false, { transform: booleanAttribute });
  readonly position = input<number | null, unknown>(null, { transform: nullableNumber });
  readonly setSize = input<number | null, unknown>(null, { transform: nullableNumber });
}

function nullableNumber(value: unknown): number | null {
  return value === null || value === undefined || value === '' ? null : numberAttribute(value);
}
