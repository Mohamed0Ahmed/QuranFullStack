import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: '[qdAyahCard]',
  standalone: true,
  templateUrl: './ayah-card.component.html',
  styleUrl: './ayah-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'qd-ayah-card' },
})
export class AyahCardComponent {}
