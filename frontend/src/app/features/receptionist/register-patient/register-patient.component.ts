import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { PatientService } from '../../../core/services/patient.service';

@Component({
  selector: 'app-register-patient',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-patient.component.html',
  styleUrls: ['./register-patient.component.scss'],
})
export class RegisterPatientComponent {
  private readonly patientService = inject(PatientService);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly registerForm = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    dateOfBirth: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email],
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

    const fullName = this.registerForm.value.fullName ?? '';
    const dateOfBirth = this.registerForm.value.dateOfBirth ?? '';
    const phone = this.registerForm.value.phone ?? '';
    const email = this.registerForm.value.email ?? '';

    this.patientService.registerWalkInPatient(fullName, dateOfBirth, phone, email).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set('Patient registered successfully.');
        this.registerForm.reset();
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        console.error('Register walk-in patient failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to register patient.',
        );
      },
    });
  }
}
