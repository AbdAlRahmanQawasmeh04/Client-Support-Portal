import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, Subject } from 'rxjs';

export const API_URL = 'https://localhost:7200/api';

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  userId: number;
  fullName: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly key = 'support-portal-session';

  private readonly session =
    signal<LoginResponse | null>(this.restore());

  readonly user = this.session.asReadonly();

  login(usernameOrEmail: string, password: string) {
    return this.http
      .post<LoginResponse>(`${API_URL}/auth/login`, {
        usernameOrEmail,
        password,
      })
      .pipe(
        tap(user => {
          this.session.set(user);

          try {
            sessionStorage.setItem(
              this.key,
              JSON.stringify(user)
            );
          } catch {
            
          }
        })
      );
  }

  readonly profileChanged = new Subject<void>();
  updateProfile(fullName: string): void {
    const user = this.session();
    if (!user) return;
    const updated = { ...user, fullName };
    this.session.set(updated);
    try { sessionStorage.setItem(this.key, JSON.stringify(updated)); } catch {}
    this.profileChanged.next();
  }

  logout(): void {
    this.session.set(null);

    try {
      sessionStorage.removeItem(this.key);
    } catch {

    }
  }

  private restore(): LoginResponse | null {
    try {
      const user = JSON.parse(
        sessionStorage.getItem(this.key) ?? 'null'
      );

      if (
        user &&
        typeof user.accessToken === 'string' &&
        user.accessToken &&
        typeof user.fullName === 'string' &&
        typeof user.userId === 'number' &&
        ['Client', 'SupportEmployee', 'SupportManager']
          .includes(user.role)
      ) {
        return user;
      }
    } catch {
      
    }

    return null;
  }
}