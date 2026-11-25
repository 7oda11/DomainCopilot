import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { AuthSessionService } from './services/auth-session.service';
import { LoginComponent } from './features/auth/login.component';
import { OverviewComponent } from './features/overview/overview.component';
import { SearchComponent } from './features/search/search.component';
import { DocumentsComponent } from './features/documents/documents.component';
import { WorkflowComponent } from './features/workflow/workflow.component';
import { WorkspaceComponent } from './features/workspace/workspace.component';

export type AppSection = 'overview' | 'search' | 'documents' | 'workflow' | 'workspace';

@Component({
  selector: 'app-root',
  imports: [CommonModule, LoginComponent, OverviewComponent, SearchComponent, DocumentsComponent, WorkflowComponent, WorkspaceComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly section = signal<AppSection>('overview');
  protected readonly currentTitle = computed(() => this.navItems.find(item => item.id === this.section())?.label ?? 'نظرة عامة');
  protected readonly navItems: { id: AppSection; label: string; icon: string }[] = [
    { id: 'overview', label: 'نظرة عامة', icon: '⌂' },
    { id: 'search', label: 'البحث الذكي', icon: '⌕' },
    { id: 'workspace', label: 'الحالات والمسودات', icon: '▦' },
    { id: 'documents', label: 'المستندات', icon: '▤' },
    { id: 'workflow', label: 'مسودة جديدة', icon: '✳' },
  ];

  constructor(protected readonly session: AuthSessionService) {}

  protected navigate(section: AppSection): void { this.section.set(section); }
  protected canBrowseDocuments(): boolean { return this.session.isClinician() || this.session.isResearcher(); }
  protected logout(): void { this.session.clear(); }
}
