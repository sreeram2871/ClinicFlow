import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

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

        this.currentUserSignal.set({
          id: '',
          fullName: response.fullName,
          email: request.email,
          role: response.role,
          tenantId: response.tenantId,
        });
      }),
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
