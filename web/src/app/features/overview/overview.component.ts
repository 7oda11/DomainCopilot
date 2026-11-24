import { Component, output } from '@angular/core';
import type { AppSection } from '../../app';

@Component({
  selector: 'app-overview',
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.css',
})
export class OverviewComponent {
  readonly navigate = output<AppSection>();
}
