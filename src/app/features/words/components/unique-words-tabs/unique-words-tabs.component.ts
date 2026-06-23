import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { UNIQUE_WORD_KIND_LABELS } from '../../models/unique-words.labels';
import { UNIQUE_WORD_KIND_KEYS, UniqueWordKind } from '../../models/unique-words.models';

interface UniqueWordsTabViewModel {
  key: UniqueWordKind;
  labelAr: string;
  route: string;
}

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
