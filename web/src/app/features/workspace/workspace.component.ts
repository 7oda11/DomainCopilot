import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { WorkspaceCase, WorkspaceDraft } from '../../models/api.models';
import { AuthSessionService } from '../../services/auth-session.service';
import { DomainCopilotApiService } from '../../services/domain-copilot-api.service';

@Component({ selector: 'app-workspace', imports: [CommonModule], templateUrl: './workspace.component.html', styleUrl: './workspace.component.css' })
export class WorkspaceComponent {
  private readonly api = inject(DomainCopilotApiService);
  protected readonly session = inject(AuthSessionService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly cases = signal<WorkspaceCase[]>([]);
  protected readonly drafts = signal<WorkspaceDraft[]>([]);

  constructor() { this.refresh(); }

  protected refresh(): void {
    this.busy.set(true); this.error.set('');
    forkJoin({ cases: this.api.workspaceCases(), drafts: this.api.workspaceDrafts() }).subscribe({
      next: data => { this.cases.set(data.cases); this.drafts.set(data.drafts); this.busy.set(false); },
      error: (error: HttpErrorResponse) => {
        this.error.set(error.status === 403 ? 'الحساب غير مصرح له بعرض السجلات.' : 'تعذر تحميل الحالات والمسودات.');
        this.busy.set(false);
      },
    });
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      AwaitingApproval: 'بانتظار اعتماد الطبيب', Approved: 'تمت الموافقة', Rejected: 'مرفوضة',
      Refused: 'لم تُنشأ مسودة', Failed: 'فشل التشغيل', Cancelled: 'أُلغي التشغيل',
      Researching: 'قيد البحث', SafetyChecking: 'مراجعة السلامة', Drafting: 'إنشاء المسودة', Created: 'جديد',
      EditedAndApproved: 'تمت الموافقة بعد التعديل',
    };
    return labels[status] ?? status;
  }

  protected statusClass(status: string): string {
    return status === 'Approved' || status === 'EditedAndApproved' ? 'approved'
      : status === 'AwaitingApproval' ? 'pending'
      : status === 'Rejected' || status === 'Refused' || status === 'Failed' ? 'declined' : 'neutral';
  }

  protected answer(item: WorkspaceDraft): string {
    return item.approvedContent || item.draftContent || 'لم تُنتج مسودة لهذا التشغيل.';
  }
}
