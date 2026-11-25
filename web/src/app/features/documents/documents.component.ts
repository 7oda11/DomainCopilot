import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomainCopilotApiService } from '../../services/domain-copilot-api.service';
import { AuthSessionService } from '../../services/auth-session.service';
import { WorkspaceDocument, WorkspaceDocumentContent } from '../../models/api.models';

@Component({ selector: 'app-documents', imports: [CommonModule, FormsModule], templateUrl: './documents.component.html', styleUrl: './documents.component.css' })
export class DocumentsComponent {
  private readonly api = inject(DomainCopilotApiService);
  protected readonly session = inject(AuthSessionService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');
  protected file?: File;
  protected version = 'v1';
  protected dragOver = false;
  protected readonly documents = signal<WorkspaceDocument[]>([]);
  protected readonly openedDocument = signal<WorkspaceDocumentContent | null>(null);

  constructor() {
    if (this.session.isClinician()) this.loadDocuments();
  }

  protected loadDocuments(): void {
    this.busy.set(true); this.error.set('');
    this.api.allDocuments().subscribe({
      next: documents => { this.documents.set(documents); this.busy.set(false); },
      error: () => { this.error.set('تعذر تحميل ملفات النظام.'); this.busy.set(false); },
    });
  }

  protected openDocument(document: WorkspaceDocument): void {
    this.busy.set(true); this.error.set(''); this.openedDocument.set(null);
    this.api.documentContent(document.documentId).subscribe({
      next: content => { this.openedDocument.set(content); this.busy.set(false); },
      error: () => { this.error.set('تعذر فتح المحتوى المستخرج من المستند.'); this.busy.set(false); },
    });
  }

  protected closeDocument(): void { this.openedDocument.set(null); }

  protected selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file = input.files?.[0]; this.error.set(''); this.success.set('');
  }

  protected upload(): void {
    if (!this.file) { this.error.set('اختار ملف PDF أو DOCX الأول.'); return; }
    if (!/\.(pdf|docx)$/i.test(this.file.name)) { this.error.set('الملفات المدعومة هي PDF وDOCX فقط.'); return; }
    if (this.file.size > 25 * 1024 * 1024) { this.error.set('حجم الملف لازم يكون أقل من 25 ميجابايت.'); return; }
    this.busy.set(true); this.error.set(''); this.success.set('');
    this.api.upload(this.file, this.version.trim() || 'v1').subscribe({
      next: result => {
        this.success.set(result.alreadyIndexed ? 'المستند ده متفهرس من قبل.' : `تمت الفهرسة: المستند اتقسم لـ ${result.chunkCount} مقطع.`);
        this.file = undefined; this.busy.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.status === 403 ? 'الدور الحالي غير مسموح له برفع المستندات.' : err.status === 0 ? 'مش قادر أوصل للـAPI.' : (err.error?.detail ?? err.error?.title ?? 'رفع المستند فشل.'));
        this.busy.set(false);
      },
    });
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault(); this.dragOver = false;
    const dropped = event.dataTransfer?.files.item(0);
    if (dropped) { this.file = dropped; this.error.set(''); }
  }
}
