export class FenwickDeltaTree {
  private values: number[] = [0];

  reset(length: number): void {
    this.values = new Array(length + 1).fill(0);
  }

  add(index: number, delta: number): void {
    for (let position = index + 1; position < this.values.length; position += position & -position) {
      this.values[position] += delta;
    }
  }

  prefix(length: number): number {
    let total = 0;
    for (let position = length; position > 0; position -= position & -position) {
      total += this.values[position];
    }
    return total;
  }
}
