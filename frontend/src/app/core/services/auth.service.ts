import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';

import { LoginRequest, LoginResponse } from '../../models/auth.model';
import { CurrentUser } from '../../models/user.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private accessToken: string | null = null;

  private currentUserSignal = signal<CurrentUser | null>(null);
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isLoggedIn = computed(() => this.currentUser() !== null);

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('https://localhost:7008/api/v1/auth/login', request, { withCredentials: true }).pipe(
      tap((response) => {
        this.accessToken = response.accessToken;
      }),
      switchMap((response) =>
        this.http.get<CurrentUser>('https://localhost:7008/api/v1/auth/me', { withCredentials: true }).pipe(
          map((currentUser) => {
            this.currentUserSignal.set(currentUser);
            return response;
          }),
        ),
      ),
    );
  }

  restoreSession(): Observable<CurrentUser | null> {
    return this.refreshAccessToken().pipe(
      switchMap(() =>
        this.http.get<CurrentUser>('https://localhost:7008/api/v1/auth/me', { withCredentials: true }).pipe(
          tap((currentUser) => {
            this.currentUserSignal.set(currentUser);
          }),
          map((currentUser) => currentUser),
        ),
      ),
      catchError(() => of(null)),
    );
  }

  refreshAccessToken(): Observable<{ accessToken: string }> {
    return this.http.post<{ accessToken: string }>('https://localhost:7008/api/v1/auth/refresh', {}, { withCredentials: true }).pipe(
      tap((response) => {
        this.accessToken = response.accessToken;
      }),
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>('https://localhost:7008/api/v1/auth/logout', {}, { withCredentials: true }).pipe(
      tap(() => {
        this.accessToken = null;
        this.currentUserSignal.set(null);
      }),
      catchError(() => {
        this.accessToken = null;
        this.currentUserSignal.set(null);
        return of(undefined);
      }),
    );
  }

  getToken(): string | null {
    return this.accessToken;
  }
}
