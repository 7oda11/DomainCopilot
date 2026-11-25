import { Injectable, computed, signal } from '@angular/core';
import { AuthResult, JwtPayload, RegisteredUser } from '../models/api.models';

const TOKEN_KEY = 'domain-copilot-token';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly accessToken = signal(sessionStorage.getItem(TOKEN_KEY) ?? '');
  readonly token = this.accessToken.asReadonly();
  readonly isAuthenticated = computed(() => this.accessToken().length > 0);
  readonly claims = computed(() => this.decode(this.accessToken()));
  readonly email = computed(() => this.claims()?.email ?? 'مستخدم');
  readonly role = computed(() => {
    const role = this.claims()?.role;
    return Array.isArray(role) ? role.join(', ') : (role ?? 'غير معروف');
  });
  readonly isClinician = computed(() => {
    const role = this.claims()?.role;
    return Array.isArray(role) ? role.includes('Clinician') : role === 'Clinician';
  });
  readonly isResearcher = computed(() => {
    const role = this.claims()?.role;
    return Array.isArray(role) ? role.includes('Researcher') : role === 'Researcher';
  });

  saveLogin(result: AuthResult): void {
    sessionStorage.setItem(TOKEN_KEY, result.accessToken);
    this.accessToken.set(result.accessToken);
  }

  clear(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    this.accessToken.set('');
  }

  private decode(token: string): JwtPayload | null {
    try {
      const part = token.split('.')[1];
      if (!part) return null;
      const base64 = part.replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(Array.from(atob(base64), char =>
        `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`).join(''));
      const payload = JSON.parse(json) as JwtPayload;
      if (payload.exp && payload.exp * 1000 < Date.now()) {
        this.clear();
        return null;
      }
      return payload;
    } catch {
      return null;
    }
  }
}
