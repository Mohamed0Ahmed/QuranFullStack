export type ExplorerColumnDirection = 'left' | 'right';

export function resolveAdjacentColumn<T extends string>(
  order: readonly T[],
  current: T,
  direction: ExplorerColumnDirection,
  isEnabled: (column: T) => boolean,
): T | null {
  const currentIndex = order.indexOf(current);
  if (currentIndex === -1) {
    return null;
  }

  const step = direction === 'right' ? 1 : -1;

  for (let index = currentIndex + step; index >= 0 && index < order.length; index += step) {
    const candidate = order[index];
    if (isEnabled(candidate)) {
      return candidate;
    }
  }

  return null;
}
