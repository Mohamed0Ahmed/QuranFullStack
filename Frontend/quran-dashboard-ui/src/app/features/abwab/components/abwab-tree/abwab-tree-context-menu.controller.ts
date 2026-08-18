export interface AbwabTreeMenuRequest {
  readonly id: number;
  readonly x: number;
  readonly y: number;
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
    this.open(id, event.clientX, event.clientY);
  }

  openFromButton(event: MouseEvent, id: number): void {
    event.stopPropagation();
    this.open(id, event.clientX, event.clientY);
  }

  openFromKeyboard(id: number, row: HTMLElement | null, direction: 'ltr' | 'rtl'): void {
    const rect = row?.getBoundingClientRect();
    const anchorX = direction === 'rtl' ? rect?.right : rect?.left;
    this.open(id, anchorX ?? 0, rect?.bottom ?? 0);
  }

  private open(id: number, x: number, y: number): void {
    this.focusDoor(id);
    this.selectDoor(id);
    this.requestMenu({ id, x, y });
  }
}
