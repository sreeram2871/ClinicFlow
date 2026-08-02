import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, switchMap, tap } from 'rxjs/operators';

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
    return this.http.post<LoginResponse>('https://localhost:7008/api/v1/auth/login', request).pipe(
      tap((response) => {
        this.accessToken = response.accessToken;
      }),
      switchMap((response) =>
        this.http.get<CurrentUser>('https://localhost:7008/api/v1/auth/me').pipe(
          map((currentUser) => {
            this.currentUserSignal.set(currentUser);
            return response;
          }),
        ),
      ),
    );
  }

  logout(): void {
    this.accessToken = null;
    this.currentUserSignal.set(null);
  }

  getToken(): string | null {
    return this.accessToken;
  }
}
