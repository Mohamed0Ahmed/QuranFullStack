import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { UNIQUE_WORD_KIND_LABELS } from '../../models/unique-words.labels';
import { UNIQUE_WORD_KIND_KEYS, UniqueWordKind } from '../../models/unique-words.models';

interface UniqueWordsTabViewModel {
  key: UniqueWordKind;
  labelAr: string;
  route: string;
}

/**
 * Stable mode tabs (`tashkeel` / `simple`) as router links. Tabs are real
 * navigable links so refresh/share and `routerLinkActive` styling work without
 * bespoke state. Labels come from the labels module; route keys stay stable.
 */
@Component({
  selector: 'qd-unique-words-tabs',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './unique-words-tabs.component.html',
  styleUrl: './unique-words-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsTabsComponent {
  readonly activeMode = input.required<UniqueWordKind>();

  protected readonly tabs: readonly UniqueWordsTabViewModel[] = UNIQUE_WORD_KIND_KEYS.map((key) => ({
    key,
    labelAr: UNIQUE_WORD_KIND_LABELS[key],
    route: `/dashboard/words/unique/${key}`,
  }));
}
