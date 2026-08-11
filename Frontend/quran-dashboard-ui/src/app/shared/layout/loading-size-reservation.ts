import { DestroyRef, ElementRef, Signal, computed, effect, inject, signal } from '@angular/core';

const RESERVATION_INLINE_SIZE_TOLERANCE_PX = 1;

interface NaturalSize {
  readonly blockSize: number;
  readonly inlineSize: number;
}

export interface QdLoadingSizeReservationOptions {
  readonly host: Signal<ElementRef<HTMLElement> | undefined>;
  readonly isLoading: Signal<boolean>;
  readonly isSettled: Signal<boolean>;
}

export interface QdLoadingSizeReservation {
  readonly reservedBlockSize: Signal<string | null>;
}

export function qdLoadingSizeReservation(
  options: QdLoadingSizeReservationOptions,
): QdLoadingSizeReservation {
  const destroyRef = inject(DestroyRef);
  const lastNaturalSize = signal<NaturalSize | null>(null);
  let observedSize: NaturalSize | null = null;

  const reservedBlockSize = computed<string | null>(() => {
    const natural = lastNaturalSize();
    return options.isLoading() && natural !== null ? `${natural.blockSize}px` : null;
  });

  const onHostResize = (entries: ResizeObserverEntry[]): void => {
    const target = entries[entries.length - 1]?.target;
    if (!target) {
      return;
    }
    const rect = target.getBoundingClientRect();
    observedSize = { blockSize: rect.height, inlineSize: rect.width };

    if (options.isSettled()) {
      lastNaturalSize.set(observedSize);
      return;
    }

    const natural = lastNaturalSize();
    if (
      options.isLoading() &&
      natural !== null &&
      Math.abs(natural.inlineSize - rect.width) > RESERVATION_INLINE_SIZE_TOLERANCE_PX
    ) {
      lastNaturalSize.set(null);
    }
  };

  if (typeof ResizeObserver !== 'undefined') {
    const observer = new ResizeObserver(onHostResize);
    destroyRef.onDestroy(() => observer.disconnect());

    effect(() => {
      const element = options.host()?.nativeElement;
      if (element) {
        observer.disconnect();
        observedSize = null;
        observer.observe(element);
      }
    });

    effect(() => {
      if (options.isSettled() && observedSize !== null) {
        lastNaturalSize.set(observedSize);
      }
    });
  }

  return { reservedBlockSize };
}
