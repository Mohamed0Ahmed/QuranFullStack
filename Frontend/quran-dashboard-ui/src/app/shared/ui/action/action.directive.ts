import { Directive, computed, input } from '@angular/core';

export type QdActionVariant =
  | 'primary'
  | 'secondary'
  | 'tertiary'
  | 'warning'
  | 'info'
  | 'danger'
  | 'icon-only'
  | 'toolbar'
  | 'row-action';

export type QdActionSize = 'sm' | 'md' | 'lg';

@Directive({
  selector: 'button[qdAction], a[qdAction]',
  standalone: true,
  host: {
    class: 'qd-action',
    '[class.qd-action--primary]': "variant() === 'primary'",
    '[class.qd-action--secondary]': "variant() === 'secondary'",
    '[class.qd-action--tertiary]': "variant() === 'tertiary'",
    '[class.qd-action--warning]': "variant() === 'warning'",
    '[class.qd-action--info]': "variant() === 'info'",
    '[class.qd-action--danger]': "variant() === 'danger'",
    '[class.qd-action--icon-only]': "variant() === 'icon-only'",
    '[class.qd-action--toolbar]': "variant() === 'toolbar'",
    '[class.qd-action--row-action]': "variant() === 'row-action'",
    '[class.qd-action--sm]': "size() === 'sm'",
    '[class.qd-action--md]': "size() === 'md'",
    '[class.qd-action--lg]': "size() === 'lg'",
    '[class.qd-action--busy-slot]': 'busy() !== undefined',
    '[class.qd-action--busy]': 'busy() === true',
    '[attr.aria-busy]': "busy() === true ? 'true' : null",
  },
})
export class QdActionDirective {
  readonly qdAction = input<QdActionVariant | ''>('');
  readonly size = input<QdActionSize>('md');
  readonly busy = input<boolean | undefined>(undefined);

  protected readonly variant = computed<QdActionVariant>(() => this.qdAction() || 'secondary');
}
