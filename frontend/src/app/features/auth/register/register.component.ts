import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment';

interface RegisterPatientRequest {
  tenantId: string;
  fullName: string;
  email: string;
  password: string;
  phone: string;
  dateOfBirth: string;
}

function pastDateValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;

  if (!value) {
    return null;
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return { invalidDate: true };
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  parsed.setHours(0, 0, 0, 0);

  return parsed < today ? null : { notPastDate: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, MatIconModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly registerUrl = `${environment.apiUrl}/auth/register-patient`;
  private readonly tenantId = '11111111-1111-1111-1111-111111111111';

  readonly isSubmitting = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly registerForm = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8)],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    dateOfBirth: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, pastDateValidator],
    }),
  });

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const payload: RegisterPatientRequest = {
      tenantId: this.tenantId,
      fullName: this.registerForm.value.fullName ?? '',
      email: this.registerForm.value.email ?? '',
      password: this.registerForm.value.password ?? '',
      phone: this.registerForm.value.phone ?? '',
      dateOfBirth: this.registerForm.value.dateOfBirth ?? '',
    };

    this.http.post(this.registerUrl, payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Registration successful. Redirecting to login...');

        setTimeout(() => {
          this.router.navigateByUrl('/login');
        }, 900);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to complete registration.',
        );
      },
    });
  }
}
