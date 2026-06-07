import { Component } from '@angular/core';
import { AppShellComponent } from './core/layout/app-shell/app-shell.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AppShellComponent],
  template: '<qd-app-shell />',
})
export class App {}
