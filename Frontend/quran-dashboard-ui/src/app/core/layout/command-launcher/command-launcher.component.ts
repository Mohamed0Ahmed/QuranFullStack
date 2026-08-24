import { A11yModule } from '@angular/cdk/a11y';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { Router, RouterLink, UrlTree } from '@angular/router';

import { NavItem } from '../../navigation/nav-items';
import { NavigationResumeService } from '../../navigation/navigation-resume.service';
import { QdActionDirective } from '../../../shared/ui/action/action.directive';
import { QdModalShellComponent } from '../../../shared/ui/modal-shell/modal-shell.component';
import { NavIconComponent } from '../nav-icon/nav-icon.component';

interface CommandDestination {
  item: NavItem;
  section: string;
}

@Component({
  selector: 'qd-command-launcher',
  standalone: true,
  imports: [
    A11yModule,
    RouterLink,
    QdActionDirective,
    QdModalShellComponent,
    NavIconComponent,
  ],
  templateUrl: './command-launcher.component.html',
  styleUrl: './command-launcher.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommandLauncherComponent {
  private readonly router = inject(Router);
  private readonly navigationResume = inject(NavigationResumeService);
  private readonly injector = inject(Injector);
  private readonly destinationOptions = viewChildren<ElementRef<HTMLElement>>('destinationOption');

  readonly items = input.required<readonly NavItem[]>();
  readonly requested = output<void>();
  readonly overlayStateChanged = output<boolean>();

  protected readonly launcherOpen = signal(false);
  protected readonly query = signal('');
  protected readonly selectedIndex = signal(0);
  protected readonly destinations = computed<readonly CommandDestination[]>(() =>
    this.items().flatMap((item) => [
      { item, section: 'التنقل الرئيسي' },
      ...(item.children ?? []).map((child) => ({ item: child, section: item.labelAr })),
    ]),
  );
  protected readonly results = computed(() => {
    const query = this.normalize(this.query());
    if (!query) {
      return this.destinations();
    }
    return this.destinations().filter(({ item, section }) =>
      this.normalize(`${item.labelAr} ${item.labelEn} ${item.route} ${section}`).includes(query),
    );
  });
  protected readonly activeDestinationId = computed(() => {
    const destination = this.results()[this.selectedIndex()];
    return destination ? this.destinationId(destination.item) : null;
  });

  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== 'k') {
      return;
    }
    event.preventDefault();
    this.requested.emit();
  }

  show(): void {
    this.query.set('');
    this.selectedIndex.set(0);
    this.launcherOpen.set(true);
    this.overlayStateChanged.emit(true);
  }

  protected closeLauncher(): void {
    if (!this.launcherOpen()) {
      return;
    }
    this.launcherOpen.set(false);
    this.overlayStateChanged.emit(false);
  }

  protected updateQuery(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
    this.selectedIndex.set(0);
  }

  protected handleSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.moveSelection(1);
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.moveSelection(-1);
      return;
    }
    if (event.key === 'Enter') {
      event.preventDefault();
      void this.activateSelected();
    }
  }

  protected navigationTarget(item: NavItem): UrlTree {
    return this.navigationResume.targetFor(item);
  }

  protected select(index: number): void {
    this.selectedIndex.set(index);
  }

  protected destinationId(item: NavItem): string {
    return `command-launcher-option-${item.key}`;
  }

  private moveSelection(step: number): void {
    const count = this.results().length;
    if (count === 0) {
      return;
    }
    const nextIndex = (this.selectedIndex() + step + count) % count;
    this.selectedIndex.set(nextIndex);
    afterNextRender(
      () => this.destinationOptions()[nextIndex]?.nativeElement.scrollIntoView({ block: 'nearest' }),
      { injector: this.injector },
    );
  }

  private async activateSelected(): Promise<void> {
    const destination = this.results()[this.selectedIndex()];
    if (!destination) {
      return;
    }
    this.closeLauncher();
    await this.router.navigateByUrl(this.navigationTarget(destination.item));
  }

  private normalize(value: string): string {
    return value
      .toLocaleLowerCase('ar')
      .normalize('NFKD')
      .replace(/[\u064b-\u065f\u0670]/g, '')
      .replace(/[إأآٱ]/g, 'ا')
      .replace(/ى/g, 'ي')
      .replace(/ة/g, 'ه')
      .trim();
  }
}
