import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { LoginRequest } from '../../../models/auth.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  errorMessage: string | null = null;

  readonly loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.isSubmitting.set(true);

    const request: LoginRequest = {
      email: this.loginForm.value.email ?? '',
      password: this.loginForm.value.password ?? '',
    };

    this.authService.login(request).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage = this.extractErrorMessage(error);
      },
    });
  }

  private extractErrorMessage(error: HttpErrorResponse): string {
    const detail = error.error?.detail;

    if (typeof detail === 'string' && detail.trim().length > 0) {
      return detail;
    }

    return 'Login failed';
  }
}
