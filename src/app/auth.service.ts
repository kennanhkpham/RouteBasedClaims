import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

interface LoginResponse {
  token: string;
  expiresAt: string;
}

type StoredAuthSession = LoginResponse;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly sessionKey = 'claim-review-admin-session';
  private readonly session = signal<StoredAuthSession | null>(this.readSession());

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('https://route-claims-api-kennan-endwesajfhaxe8gj.westcentralus-01.azurewebsites.net/api/auth/login', { username, password })
      .pipe(tap((response) => this.saveSession(response)));
  }

  isAdmin(): boolean {
    const session = this.session();
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  }

  getToken(): string | null {
    return this.isAdmin() ? this.session()?.token ?? null : null;
  }

  logout(): void {
    this.session.set(null);
    if (this.isBrowser) {
      sessionStorage.removeItem(this.sessionKey);
    }
  }

  private saveSession(session: StoredAuthSession): void {
    this.session.set(session);
    if (this.isBrowser) {
      sessionStorage.setItem(this.sessionKey, JSON.stringify(session));
    }
  }

  private readSession(): StoredAuthSession | null {
    if (!this.isBrowser) {
      return null;
    }

    const storedSession = sessionStorage.getItem(this.sessionKey);
    if (!storedSession) {
      return null;
    }

    try {
      const parsedSession: unknown = JSON.parse(storedSession);
      if (
        typeof parsedSession === 'object' &&
        parsedSession !== null &&
        'token' in parsedSession &&
        typeof parsedSession.token === 'string' &&
        'expiresAt' in parsedSession &&
        typeof parsedSession.expiresAt === 'string' &&
        Number.isFinite(new Date(parsedSession.expiresAt).getTime()) &&
        new Date(parsedSession.expiresAt).getTime() > Date.now()
      ) {
        return { token: parsedSession.token, expiresAt: parsedSession.expiresAt };
      }
    } catch {
      sessionStorage.removeItem(this.sessionKey);
    }

    sessionStorage.removeItem(this.sessionKey);
    return null;
  }
}
