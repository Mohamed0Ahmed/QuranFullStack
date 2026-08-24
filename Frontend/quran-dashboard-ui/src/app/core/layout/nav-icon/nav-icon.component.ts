import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { NavIconName } from '../../navigation/nav-items';

const ICON_PATHS: Record<NavIconName, string> = {
  archive: 'M4 7h16v13H4z M3 4h18v3H3z M9 11h6',
  book: `M4 4.5A3.5 3.5 0 0 1 7.5 2H12v18H7.5A3.5 3.5 0 0 0 4 22z
    M20 4.5A3.5 3.5 0 0 0 16.5 2H12v18h4.5A3.5 3.5 0 0 1 20 22z`,
  compare: 'M8 4h12 M16 1l4 3-4 3 M16 20H4 M8 17l-4 3 4 3 M6 9h12 M6 15h12',
  dashboard: 'M3 3h7v7H3z M14 3h7v4h-7z M14 11h7v10h-7z M3 14h7v7H3z',
  languages: 'M4 5h9 M8.5 3v2 M6 5c.7 3 2.5 5 6 6 M11 5c-.8 3-2.5 5-6 6 M14 21l4-10 4 10 M15.5 17h5',
  lemma: 'M5 4h14 M5 9h10 M5 14h14 M5 19h8',
  library: 'M4 5h5v15H4z M10 3h5v17h-5z M16 6h4v14h-4z',
  link: 'M10 13a5 5 0 0 0 7.5.5l2-2a5 5 0 0 0-7-7l-1.1 1.1 M14 11a5 5 0 0 0-7.5-.5l-2 2a5 5 0 0 0 7 7l1.1-1.1',
  login: 'M10 4H5v16h5 M14 8l4 4-4 4 M18 12H9',
  logout: 'M14 4h5v16h-5 M10 8l-4 4 4 4 M6 12h9',
  menu: 'M4 7h16 M4 12h16 M4 17h16',
  moon: 'M20 15.5A8 8 0 0 1 8.5 4 8.5 8.5 0 1 0 20 15.5z',
  more: 'M5 12h.01 M12 12h.01 M19 12h.01',
  pen: 'm4 20 4.2-1 10.9-10.9a2.2 2.2 0 0 0-3.2-3.2L5 15.8z M14.5 6.5l3 3',
  root: 'M12 21V8 M12 13c-4 0-7-2.2-7-6 4 0 7 2.2 7 6z M12 11c4 0 7-2.2 7-6-4 0-7 2.2-7 6z',
  search: 'M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14z M20 20l-4-4',
  settings: `M12 15.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4z
    M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-2.8 2.8-.1-.1a1.7 1.7 0 0 0-1.9-.3
    1.7 1.7 0 0 0-1 1.6v.2h-4V21a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1
    L4.2 17l.1-.1a1.7 1.7 0 0 0 .3-1.9A1.7 1.7 0 0 0 3 14H2.8v-4H3a1.7 1.7 0 0 0
    1.6-1 1.7 1.7 0 0 0-.3-1.9L4.2 7 7 4.2l.1.1a1.7 1.7 0 0 0 1.9.3A1.7 1.7 0 0 0
    10 3V2.8h4V3a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1L19.8 7l-.1.1a1.7 1.7 0 0 0
    -.3 1.9 1.7 1.7 0 0 0 1.6 1h.2v4H21a1.7 1.7 0 0 0-1.6 1z`,
  stem: 'M5 20V9 M5 15c5 0 8-3 8-8-5 0-8 3-8 8z M13 20V12 M13 16c4 0 6-2.4 6-6-4 0-6 2.4-6 6z',
  sun: `M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7z M12 2v2 M12 20v2 M2 12h2
    M20 12h2 M4.9 4.9l1.4 1.4 M17.7 17.7l1.4 1.4 M4.9 19.1l1.4-1.4 M17.7 6.3l1.4-1.4`,
  tag: 'M20 13 13 20 4 11V4h7z M8 8h.01',
  template: 'M4 4h16v16H4z M4 9h16 M10 9v11',
  tree: 'M12 4v5 M6 20v-4h12v4 M6 16V9h12v7 M3 4h6v5H3z M15 4h6v5h-6z M3 16h6v5H3z M15 16h6v5h-6z',
  user: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M4 21a8 8 0 0 1 16 0',
  volume: 'M5 10H2v4h3l4 4V6z M13 9a4 4 0 0 1 0 6 M16 6a8 8 0 0 1 0 12',
  words: 'M5 4h14 M5 9h10 M5 14h14 M5 19h8 M3 4h.01 M3 9h.01 M3 14h.01 M3 19h.01',
};

@Component({
  selector: 'qd-nav-icon',
  standalone: true,
  templateUrl: './nav-icon.component.html',
  styleUrl: './nav-icon.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavIconComponent {
  readonly name = input.required<NavIconName>();
  protected readonly path = computed(() => ICON_PATHS[this.name()]);
}
