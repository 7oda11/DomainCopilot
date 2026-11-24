import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Evidence } from '../../models/api.models';
import { DomainCopilotApiService } from '../../services/domain-copilot-api.service';

@Component({ selector: 'app-search', imports: [CommonModule, FormsModule], templateUrl: './search.component.html', styleUrl: './search.component.css' })
export class SearchComponent {
  private readonly api = inject(DomainCopilotApiService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected query = '';
  protected limit = 5;
  protected results: Evidence[] = [];

  protected search(): void {
    this.busy.set(true); this.error.set(''); this.message.set(''); this.results = [];
    this.api.search(this.query.trim(), Number(this.limit)).subscribe({
      next: result => {
        this.results = result.evidence ?? [];
        this.message.set(result.refused ? (result.refusalReason ?? 'ملقيناش أدلة كفاية.') : `لقينا ${this.results.length} مقطع من المستندات.`);
        this.busy.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.status === 403 ? 'الدور الحالي غير مسموح له بالبحث.' : err.status === 0 ? 'مش قادر أوصل للـAPI.' : (err.error?.detail ?? err.error?.title ?? 'البحث فشل.'));
        this.busy.set(false);
      },
    });
  }
}
