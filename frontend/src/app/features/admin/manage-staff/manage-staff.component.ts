import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

import { StaffService } from '../../../core/services/staff.service';
import { StaffMember } from '../../../models/staff.model';
import { getInitials } from '../../../shared/utils/initials.util';

@Component({
  selector: 'app-manage-staff',
  standalone: true,
  imports: [ReactiveFormsModule, MatIconModule],
  templateUrl: './manage-staff.component.html',
  styleUrl: './manage-staff.component.scss',
})
export class ManageStaffComponent {
  private readonly staffService = inject(StaffService);
  readonly getInitials = getInitials;

  readonly staffList = signal<StaffMember[]>([]);
  readonly isSubmitting = signal(false);
  readonly showPassword = signal(false);
  readonly staffCounts = computed(() => {
    const staff = this.staffList();

    return {
      total: staff.length,
      admins: staff.filter((member) => member.role === 'Admin').length,
      doctors: staff.filter((member) => member.role === 'Doctor').length,
      receptionists: staff.filter((member) => member.role === 'Receptionist').length,
      active: staff.filter((member) => member.isActive).length,
    };
  });
  errorMessage: string | null = null;

  readonly staffForm = new FormGroup({
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
    role: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  ngOnInit(): void {
    this.loadStaffList();
  }

  toggleShowPassword(): void {
    this.showPassword.update((value) => !value);
  }

  onSubmit(): void {
    if (this.staffForm.invalid) {
      this.staffForm.markAllAsTouched();
      return;
    }

    this.errorMessage = null;
    this.isSubmitting.set(true);

    const request = {
      fullName: this.staffForm.value.fullName ?? '',
      email: this.staffForm.value.email ?? '',
      password: this.staffForm.value.password ?? '',
      role: this.staffForm.value.role ?? '',
    };

    this.staffService.createStaff(request).subscribe({
      next: () => {
        this.staffForm.reset();
        this.isSubmitting.set(false);
        this.loadStaffList();
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        const detail = error.error?.detail;
        this.errorMessage = typeof detail === 'string' && detail.trim().length > 0
          ? detail
          : 'Failed to create staff member';
      },
    });
  }

  private loadStaffList(): void {
    this.staffService.getStaffList().subscribe({
      next: (staff) => {
        this.staffList.set(staff);
      },
      error: (err) => {
        console.error('Staff list load failed', err);
      },
    });
  }
}
