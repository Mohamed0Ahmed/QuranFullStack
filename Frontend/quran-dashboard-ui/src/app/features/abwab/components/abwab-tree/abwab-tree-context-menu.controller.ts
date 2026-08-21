export interface AbwabTreeMenuRequest {
  readonly id: number;
  readonly x: number;
  readonly y: number;
  readonly kind: 'details' | 'operations';
}

export class AbwabTreeContextMenuController {
  constructor(
    private readonly focusDoor: (id: number) => void,
    private readonly selectDoor: (id: number) => void,
    private readonly requestMenu: (request: AbwabTreeMenuRequest) => void,
  ) {}

  openFromRow(event: MouseEvent, id: number, bulkMode: boolean): void {
    event.preventDefault();
    if (bulkMode) {
      return;
    }
    this.open(id, event.clientX, event.clientY, 'operations');
  }

  openFromButton(event: MouseEvent, id: number): void {
    event.stopPropagation();
    this.open(id, event.clientX, event.clientY, 'operations');
  }

  openDetailsFromButton(event: MouseEvent, id: number): void {
    event.stopPropagation();
    this.open(id, event.clientX, event.clientY, 'details');
  }

  openFromKeyboard(id: number, row: HTMLElement | null, direction: 'ltr' | 'rtl'): void {
    const rect = row?.getBoundingClientRect();
    const anchorX = direction === 'rtl' ? rect?.right : rect?.left;
    this.open(id, anchorX ?? 0, rect?.bottom ?? 0, 'operations');
  }

  private open(id: number, x: number, y: number, kind: AbwabTreeMenuRequest['kind']): void {
    this.focusDoor(id);
    this.selectDoor(id);
    this.requestMenu({ id, x, y, kind });
  }
}
