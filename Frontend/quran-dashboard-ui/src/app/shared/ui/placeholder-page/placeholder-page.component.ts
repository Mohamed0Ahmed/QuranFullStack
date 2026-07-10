import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'qd-placeholder-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './placeholder-page.component.html',
  styleUrls: ['./placeholder-page.component.scss'],
})
export class PlaceholderPageComponent {
  private route = inject(ActivatedRoute);
  titleAr$ = this.route.data;
}
