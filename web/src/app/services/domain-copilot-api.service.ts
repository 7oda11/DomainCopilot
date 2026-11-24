import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AuthResult, CreatedCase, RegisteredUser, ReviewResponse,
  SearchResponse, UploadResponse, WorkflowResponse, WorkflowProgress, WorkflowStreamEvent,
} from '../models/api.models';
import { AuthSessionService } from './auth-session.service';

function normalizeJsonKeys(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(normalizeJsonKeys);
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, nested]) => [
      key.length > 0 ? key[0].toLowerCase() + key.slice(1) : key,
      normalizeJsonKeys(nested),
    ]));
  }
  return value;
}

@Injectable({ providedIn: 'root' })
export class DomainCopilotApiService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSessionService);

  login(email: string, password: string): Observable<AuthResult> {
    return this.http.post<AuthResult>('/api/auth/login', { email, password });
  }

  register(email: string, password: string): Observable<RegisteredUser> {
    return this.http.post<RegisteredUser>('/api/auth/register', { email, password });
  }

  search(query: string, limit: number): Observable<SearchResponse> {
    return this.http.post<SearchResponse>('/api/search', { query, limit });
  }

  upload(file: File, version: string): Observable<UploadResponse> {
    const form = new FormData();
    form.append('file', file);
    form.append('version', version);
    return this.http.post<UploadResponse>('/api/documents', form);
  }

  createCase(summary: string): Observable<CreatedCase> {
    return this.http.post<CreatedCase>('/api/cases', { syntheticCaseSummary: summary });
  }

  createDraft(caseId: string, question: string, evidenceLimit: number): Observable<WorkflowResponse> {
    return this.http.post<WorkflowResponse>('/api/copilot/drafts', { caseId, question, evidenceLimit });
  }

  createDraftStream(caseId: string, question: string, evidenceLimit: number): Observable<WorkflowStreamEvent> {
    return new Observable<WorkflowStreamEvent>(subscriber => {
      const controller = new AbortController();
      const token = this.session.token();

      const readStream = async (): Promise<void> => {
        try {
          const response = await fetch('/api/copilot/drafts/stream', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              ...(token ? { Authorization: `Bearer ${token}` } : {}),
            },
            body: JSON.stringify({ caseId, question, evidenceLimit }),
            signal: controller.signal,
          });

          if (!response.ok) {
            const problem = await response.json().catch(() => null);
            subscriber.error(new HttpErrorResponse({
              status: response.status,
              statusText: response.statusText,
              error: problem,
              url: response.url,
            }));
            return;
          }

          if (!response.body) throw new Error('The API did not return a streaming response.');

          const reader = response.body.getReader();
          const decoder = new TextDecoder();
          let buffer = '';

          const dispatch = (block: string): void => {
            let eventName = 'message';
            const data: string[] = [];
            for (const line of block.split(/\r?\n/)) {
              if (line.startsWith('event:')) eventName = line.slice(6).trim();
              else if (line.startsWith('data:')) data.push(line.slice(5).trimStart());
            }
            if (data.length === 0) return;

            const payload = normalizeJsonKeys(JSON.parse(data.join('\n'))) as WorkflowProgress | WorkflowResponse | { status?: number; title?: string; detail?: string };
            if (eventName === 'progress') subscriber.next({ type: 'progress', progress: payload as WorkflowProgress });
            else if (eventName === 'completed') subscriber.next({ type: 'completed', result: payload as WorkflowResponse });
            else if (eventName === 'error') {
              subscriber.next({ type: 'error', problem: payload as { status?: number; title?: string; detail?: string } });
            }
          };

          while (true) {
            const { value, done } = await reader.read();
            buffer += decoder.decode(value, { stream: !done });
            const blocks = buffer.split(/\r?\n\r?\n/);
            buffer = blocks.pop() ?? '';
            for (const block of blocks) dispatch(block);
            if (done) break;
          }
          if (buffer.trim()) dispatch(buffer);
          subscriber.complete();
        } catch (error) {
          if (!controller.signal.aborted) subscriber.error(error);
        }
      };

      void readStream();
      return () => controller.abort();
    });
  }

  reviewDraft(draftId: string, action: 'Approved' | 'Rejected' | 'EditedAndApproved', comment: string, approvedContent?: string): Observable<ReviewResponse> {
    return this.http.post<ReviewResponse>(`/api/drafts/${draftId}/review`, {
      action, comment: comment || null, approvedContent: approvedContent ?? null,
    });
  }
}
