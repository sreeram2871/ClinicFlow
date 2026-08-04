import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

import { ScheduleService } from '../../../core/services/schedule.service';
import { DoctorListItem } from '../../../models/doctor.model';
import { DoctorSchedule } from '../../../models/doctor-schedule.model';

function formatDateForInput(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [DatePipe, MatIconModule, NgClass, NgFor, NgIf, ReactiveFormsModule],
  templateUrl: './billing.component.html',
  styleUrls: ['./billing.component.scss'],
})
export class BillingComponent implements OnInit {
  private readonly scheduleService = inject(ScheduleService);

  readonly doctors = signal<DoctorListItem[]>([]);
  readonly selectedDoctorId = signal<string>('');
  readonly selectedDate = signal<string>(formatDateForInput(new Date()));
  readonly schedule = signal<DoctorSchedule | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly payingAppointmentId = signal<string | null>(null);

  readonly paymentForm = new FormGroup({
    amount: new FormControl<number | null>(null, {
      nonNullable: false,
      validators: [Validators.required, Validators.min(1)],
    }),
    method: new FormControl<'Cash' | 'Other' | ''>('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  ngOnInit(): void {
    this.loadDoctors();
  }

  loadSchedule(): void {
    const doctorId = this.selectedDoctorId();

    if (!doctorId) {
      this.schedule.set(null);
      return;
    }

    this.scheduleService.getSchedule(doctorId, this.selectedDate()).subscribe({
      next: (schedule) => {
        this.schedule.set(schedule);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Schedule load failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to load schedule.',
        );
      },
    });
  }

  onDoctorChange(id: string): void {
    this.selectedDoctorId.set(id);
    this.loadSchedule();
  }

  onDateChange(date: string): void {
    this.selectedDate.set(date);
    this.loadSchedule();
  }

  openPaymentForm(appointmentId: string): void {
    this.payingAppointmentId.set(appointmentId);
    this.paymentForm.reset({ amount: null, method: '' });
    this.errorMessage.set(null);
  }

  submitPayment(appointmentId: string): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const amount = Number(this.paymentForm.value.amount ?? 0);
    const method = this.paymentForm.value.method as 'Cash' | 'Other';

    this.scheduleService.recordPayment(appointmentId, amount, method).subscribe({
      next: () => {
        this.successMessage.set(`Payment recorded for appointment ${appointmentId}.`);
        this.payingAppointmentId.set(null);
        this.paymentForm.reset({ amount: null, method: '' });
        this.loadSchedule();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Record payment failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to record payment.',
        );
      },
    });
  }

  private loadDoctors(): void {
    this.scheduleService.getDoctors().subscribe({
      next: (doctors) => {
        this.doctors.set(doctors);
      },
      error: (error) => {
        console.error('Doctors load failed', error);
      },
    });
  }
}
