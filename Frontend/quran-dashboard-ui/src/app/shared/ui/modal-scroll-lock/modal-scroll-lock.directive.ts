import { Directive, OnDestroy, OnInit, inject } from '@angular/core';

import { ScrollLockHandle, ScrollLockService } from './scroll-lock.service';

@Directive({
  selector: '[qdModalScrollLock]',
  standalone: true,
})
export class ModalScrollLockDirective implements OnInit, OnDestroy {
  private readonly scrollLock = inject(ScrollLockService);
  private handle: ScrollLockHandle | null = null;

  ngOnInit(): void {
    this.handle ??= this.scrollLock.hold();
  }

  ngOnDestroy(): void {
    this.handle?.release();
    this.handle = null;
  }
}
