interface LinkingWeightedCacheEntry<T> {
  value: T;
  weight: number;
  expiresAt: number;
}

export class LinkingPageCache<T> {
  private readonly entries = new Map<string, LinkingWeightedCacheEntry<T>>();
  private currentWeight = 0;

  constructor(
    private readonly budget: number,
    private readonly ttlMs: number,
    private readonly dispose: (key: string, value: T) => void,
  ) {}

  get(key: string): T | null {
    const entry = this.entries.get(key);
    if (entry === undefined) {
      return null;
    }
    if (entry.expiresAt <= Date.now()) {
      this.remove(key, entry);
      return null;
    }
    this.entries.delete(key);
    this.entries.set(key, entry);
    return entry.value;
  }

  set(key: string, value: T, weight: number): boolean {
    if (!Number.isSafeInteger(weight) || weight <= 0 || weight > this.budget) {
      return false;
    }
    const existing = this.entries.get(key);
    if (existing !== undefined) {
      this.remove(key, existing);
    }
    const entry = { value, weight, expiresAt: Date.now() + this.ttlMs };
    this.entries.set(key, entry);
    this.currentWeight += weight;
    this.evictToBudget();
    return this.entries.has(key);
  }

  delete(key: string): void {
    const entry = this.entries.get(key);
    if (entry !== undefined) {
      this.remove(key, entry);
    }
  }

  clear(): void {
    for (const [key, entry] of this.entries) {
      this.dispose(key, entry.value);
    }
    this.entries.clear();
    this.currentWeight = 0;
  }

  deleteWhere(predicate: (key: string, value: T) => boolean): void {
    for (const [key, entry] of [...this.entries]) {
      if (predicate(key, entry.value)) {
        this.remove(key, entry);
      }
    }
  }

  private evictToBudget(): void {
    while (this.currentWeight > this.budget) {
      const oldest = this.entries.entries().next().value as
        | [string, LinkingWeightedCacheEntry<T>]
        | undefined;
      if (oldest === undefined) {
        return;
      }
      this.remove(oldest[0], oldest[1]);
    }
  }

  private remove(key: string, entry: LinkingWeightedCacheEntry<T>): void {
    if (!this.entries.delete(key)) {
      return;
    }
    this.currentWeight -= entry.weight;
    this.dispose(key, entry.value);
  }
}
