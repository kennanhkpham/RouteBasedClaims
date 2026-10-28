import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Observable, mergeMap, of, tap, throwError } from 'rxjs';
import { environment } from '../environments/environment';

interface LoginResponse {
  token: string;
  expiresAt: string;
  role: 'Admin' | 'Claimant';
}

export interface CreatedAccountResponse {
  id: number;
  username: string;
  isAdmin: boolean;
}

type StoredAuthSession = LoginResponse;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly userSessionKey = 'claimstream-user-session';
  private readonly adminSessionKey = 'claimstream-admin-session';
  private readonly userSession = signal<StoredAuthSession | null>(
    this.readSession(this.userSessionKey, 'Claimant'),
  );
  private readonly adminSession = signal<StoredAuthSession | null>(
    this.readSession(this.adminSessionKey, 'Admin'),
  );
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, { username, password })
      .pipe(
        mergeMap((response) => response.role === 'Claimant'
          ? of(response)
          : throwError(() => new Error('Administrator accounts must sign in from Claim Review.'))),
        tap((response) => this.saveSession(response, 'Claimant')),
      );
  }

  loginAdmin(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/admin-login`, { username, password })
      .pipe(
        mergeMap((response) => response.role === 'Admin'
          ? of(response)
          : throwError(() => new Error('Invalid administrator account response.'))),
        tap((response) => this.saveSession(response, 'Admin')),
      );
  }

  register(username: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/register`, { username, password })
      .pipe(tap((response) => this.saveSession(response, 'Claimant')));
  }

  createAccount(
    username: string,
    password: string,
    isAdmin: boolean,
  ): Observable<CreatedAccountResponse> {
    return this.http.post<CreatedAccountResponse>(`${this.apiUrl}/accounts`, {
      username,
      password,
      isAdmin,
    });
  }

  isAuthenticated(): boolean {
    return this.isSessionValid(this.userSession());
  }

  isAdmin(): boolean {
    return this.isSessionValid(this.adminSession());
  }

  getToken(): string | null {
    return this.getTokenForRequest(false);
  }

  getTokenForRequest(requiresAdmin: boolean): string | null {
    const session = requiresAdmin ? this.adminSession() : this.userSession();
    return this.isSessionValid(session) ? session?.token ?? null : null;
  }

  logout(): void {
    this.userSession.set(null);
    this.adminSession.set(null);
    if (this.isBrowser) {
      sessionStorage.removeItem(this.userSessionKey);
      sessionStorage.removeItem(this.adminSessionKey);
    }
  }

  logoutAdmin(): void {
    this.adminSession.set(null);
    if (this.isBrowser) {
      sessionStorage.removeItem(this.adminSessionKey);
    }
  }

  private saveSession(session: StoredAuthSession, role: 'Admin' | 'Claimant'): void {
    if (role === 'Admin') {
      this.adminSession.set(session);
    } else {
      this.userSession.set(session);
    }

    if (this.isBrowser) {
      const key = role === 'Admin' ? this.adminSessionKey : this.userSessionKey;
      sessionStorage.setItem(key, JSON.stringify(session));
    }
  }

  private isSessionValid(session: StoredAuthSession | null): boolean {
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  }

  private readSession(key: string, role: 'Admin' | 'Claimant'): StoredAuthSession | null {
    if (!this.isBrowser) {
      return null;
    }

    const storedSession = sessionStorage.getItem(key);
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
        'role' in parsedSession &&
        parsedSession.role === role &&
        Number.isFinite(new Date(parsedSession.expiresAt).getTime()) &&
        new Date(parsedSession.expiresAt).getTime() > Date.now()
      ) {
        return {
          token: parsedSession.token,
          expiresAt: parsedSession.expiresAt,
          role,
        };
      }
    } catch {
      sessionStorage.removeItem(key);
    }

    sessionStorage.removeItem(key);
    return null;
  }
}
