import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WorkflowProgress, WorkflowResponse } from '../../models/api.models';
import { DomainCopilotApiService } from '../../services/domain-copilot-api.service';

type StepState = 'waiting' | 'active' | 'completed' | 'retrying' | 'failed' | 'skipped';
interface WorkflowStep { id: string; label: string; state: StepState; detail: string; }

const EMPTY_STEPS: WorkflowStep[] = [
  { id: 'SaveCase', label: 'حفظ الحالة', state: 'waiting', detail: 'حفظ ملخص الحالة في قاعدة البيانات.' },
  { id: 'EvidenceSearch', label: 'استرجاع الأدلة (RAG)', state: 'waiting', detail: 'البحث بالمعنى والكلمات في المستندات.' },
  { id: 'GuidelineResearcher', label: 'الباحث', state: 'waiting', detail: 'استخراج المعلومات المدعومة بالمصادر.' },
  { id: 'DocumentationDrafter', label: 'كاتب المسودة', state: 'waiting', detail: 'صياغة مسودة من نتائج البحث.' },
  { id: 'SafetyChecker', label: 'مراجع السلامة', state: 'waiting', detail: 'مراجعة المسودة مقابل الأدلة.' },
  { id: 'SaveDraft', label: 'حفظ المسودة', state: 'waiting', detail: 'حفظها وربطها بمراجعة بشرية.' },
];

@Component({ selector: 'app-workflow', imports: [CommonModule, FormsModule], templateUrl: './workflow.component.html', styleUrl: './workflow.component.css' })
export class WorkflowComponent {
  private readonly api = inject(DomainCopilotApiService);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly steps = signal<WorkflowStep[]>(EMPTY_STEPS.map(step => ({ ...step })));
  protected readonly progressVisible = signal(false);
  protected readonly currentStep = signal('');
  protected summary = '';
  protected question = '';
  protected caseId = '';
  protected evidenceLimit = 5;
  protected result?: WorkflowResponse;
  protected editedDraft = '';
  protected reviewComment = '';

  protected createAndRun(): void {
    if (!this.summary.trim() || !this.question.trim()) { this.error.set('اكتب ملخص الحالة والسؤال الأول.'); return; }
    this.beginProgress();
    this.setStep('SaveCase', 'active', 'بنحفظ ملخص الحالة...');
    this.api.createCase(this.summary.trim()).subscribe({
      next: created => {
        this.caseId = created.caseId;
        this.setStep('SaveCase', 'completed', 'اتحفظت الحالة.');
        this.setStep('EvidenceSearch', 'active', 'بنبحث في المستندات عن أدلة مناسبة...');
        this.runDraft();
      },
      error: (err: HttpErrorResponse) => this.handleError(err),
    });
  }

  protected runOnExistingCase(): void {
    if (!this.caseId.trim() || !this.question.trim()) { this.error.set('اكتب رقم الحالة والسؤال.'); return; }
    this.beginProgress();
    this.setStep('SaveCase', 'completed', 'استخدمنا الحالة الموجودة.');
    this.setStep('EvidenceSearch', 'active', 'بنبحث في المستندات عن أدلة مناسبة...');
    this.runDraft();
  }

  protected review(action: 'Approved' | 'Rejected' | 'EditedAndApproved'): void {
    const draftId = this.result?.draftId;
    if (!draftId) return;
    this.busy.set(true); this.error.set(''); this.message.set('');
    this.api.reviewDraft(draftId, action, this.reviewComment, action === 'EditedAndApproved' ? this.editedDraft : undefined).subscribe({
      next: response => { this.message.set(`اتسجل القرار: ${response.action}. حالة التشغيل: ${response.status}.`); this.busy.set(false); },
      error: (err: HttpErrorResponse) => this.handleError(err),
    });
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = { AwaitingApproval: 'مستنية مراجعة الطبيب', Refused: 'اترفضت — الأدلة مش كفاية', Approved: 'تمت الموافقة', Rejected: 'تم الرفض', Researching: 'جاري البحث' };
    return labels[status] ?? status;
  }

  private runDraft(): void {
    this.busy.set(true);
    this.api.createDraftStream(this.caseId, this.question.trim(), Number(this.evidenceLimit)).subscribe({
      next: event => {
        if (event.type === 'progress') {
          this.applyProgress(event.progress);
        } else if (event.type === 'completed') {
          this.result = event.result;
          this.editedDraft = event.result.draft ?? '';
          this.finishProgress(event.result.refused);
          this.message.set(event.result.refused
            ? (event.result.refusalReason ?? 'المساعد وقف عشان الأدلة مش كفاية.')
            : 'الـworkflow خلص. راجع الأدلة والمسودة.');
        } else {
          this.error.set(event.problem.detail ?? event.problem.title ?? 'الـworkflow ماكملش.');
          this.failCurrentStep();
          this.busy.set(false);
        }
      },
      error: (err: HttpErrorResponse) => this.handleError(err),
      complete: () => this.busy.set(false),
    });
  }

  private handleError(err: HttpErrorResponse): void {
    this.error.set(err.status === 403
      ? 'حسابك محتاج دور Researcher أو Clinician لتنفيذ العملية.'
      : err.status === 404 ? 'رقم الحالة مش موجود في قاعدة البيانات.'
      : err.status === 0 ? 'مش قادر أوصل للـAPI.'
      : (err.error?.detail ?? err.error?.title ?? 'العملية فشلت.'));
    this.busy.set(false);
    this.failCurrentStep();
  }

  private beginProgress(): void {
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.result = undefined;
    this.progressVisible.set(true);
    this.steps.set(EMPTY_STEPS.map(step => ({ ...step })));
    this.currentStep.set('جارٍ تجهيز التشغيل...');
  }

  private applyProgress(progress: WorkflowProgress): void {
    const state: StepState = progress.outcome === 'started' ? 'active'
      : progress.outcome === 'completed' ? 'completed'
      : progress.outcome === 'retrying' ? 'retrying'
      : progress.outcome === 'failed' ? 'failed'
      : progress.outcome === 'cancelled' ? 'skipped' : 'active';
    const duration = progress.elapsedMilliseconds > 0 ? ` · ${progress.elapsedMilliseconds} ms` : '';
    const detail = progress.outcome === 'started' ? 'شغالة دلوقتي...'
      : progress.outcome === 'completed' ? `اكتملت${duration}`
      : progress.outcome === 'retrying' ? 'حصل عطل مؤقت، بيحاول تاني...'
      : progress.outcome === 'failed' ? 'الخطوة ماكملتش.'
      : progress.outcome === 'cancelled' ? 'تم إيقاف الخطوة.' : progress.providerId;
    this.setStep(progress.agentName, state, detail);
  }

  private setStep(id: string, state: StepState, detail: string): void {
    this.steps.update(items => items.map(item => item.id === id ? { ...item, state, detail } : item));
    const active = this.steps().filter(item => item.state === 'active' || item.state === 'retrying');
    this.currentStep.set(active.length > 0
      ? `${active.map(item => item.label).join(' و ')} ${active.length > 1 ? 'شغالين' : 'شغال'}...`
      : 'تم تحديث مراحل التشغيل.');
  }

  private finishProgress(refused: boolean): void {
    this.steps.update(items => items.map(item => {
      if (item.state === 'waiting') {
        return { ...item, state: 'skipped', detail: refused ? 'توقفت العملية قبل هذه المرحلة.' : 'لم تكن مطلوبة.' };
      }
      if (!refused && (item.state === 'active' || item.state === 'retrying')) {
        return { ...item, state: 'completed', detail: 'اكتملت.' };
      }
      return item;
    }));
    this.currentStep.set(refused ? 'انتهى التشغيل بدون مسودة.' : 'اكتمل التشغيل.');
  }

  private failCurrentStep(): void {
    this.steps.update(items => items.map(item => item.state === 'active' || item.state === 'retrying'
      ? { ...item, state: 'failed', detail: 'الخطوة ماكملتش.' }
      : item));
  }
}
