import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { map } from 'rxjs';
import { AuthResult, RegisteredUser } from '../../models/api.models';
import { AuthSessionService } from '../../services/auth-session.service';
import { DomainCopilotApiService } from '../../services/domain-copilot-api.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly api = inject(DomainCopilotApiService);
  private readonly session = inject(AuthSessionService);
  readonly authenticated = output<void>();
  protected readonly registerMode = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected email = '';
  protected password = '';

  protected toggleMode(): void {
    this.registerMode.update(value => !value);
    this.error.set('');
    this.notice.set('');
  }

  protected submit(): void {
    this.error.set('');
    this.notice.set('');
    this.busy.set(true);
    const request = this.registerMode()
      ? this.api.register(this.email.trim(), this.password).pipe(map(user => user as AuthResult | RegisteredUser))
      : this.api.login(this.email.trim(), this.password).pipe(map(auth => auth as AuthResult | RegisteredUser));
    request.subscribe({
      next: (result) => {
        if ('accessToken' in result) {
          this.session.saveLogin(result);
          this.authenticated.emit();
        } else {
          this.registerMode.set(false);
          this.notice.set('الحساب اتعمل. سجّل دخولك بنفس البريد وكلمة المرور.');
        }
        this.busy.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.status === 0
          ? 'الـAPI مش شغالة. شغّل DomainCopilot.Api الأول.'
          : (err.error?.detail ?? err.error?.title ?? 'راجع البريد وكلمة المرور وحاول تاني.'));
        this.busy.set(false);
      },
    });
  }
}
