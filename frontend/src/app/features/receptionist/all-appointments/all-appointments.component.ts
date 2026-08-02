import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { PatientService } from '../../../core/services/patient.service';
import { ScheduleService } from '../../../core/services/schedule.service';
import { DoctorListItem } from '../../../models/doctor.model';
import { DoctorSchedule } from '../../../models/doctor-schedule.model';
import { PatientListItem } from '../../../models/patient.model';

function formatDateForInput(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

@Component({
  selector: 'app-all-appointments',
  standalone: true,
  imports: [DatePipe, NgClass, NgFor, NgIf, ReactiveFormsModule],
  templateUrl: './all-appointments.component.html',
  styleUrls: ['./all-appointments.component.scss'],
})
export class AllAppointmentsComponent implements OnInit {
  private readonly scheduleService = inject(ScheduleService);
  private readonly patientService = inject(PatientService);

  readonly doctors = signal<DoctorListItem[]>([]);
  readonly patients = signal<PatientListItem[]>([]);
  readonly selectedDoctorId = signal<string>('');
  readonly selectedDate = signal<string>(formatDateForInput(new Date()));
  readonly schedule = signal<DoctorSchedule | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly actionInProgress = signal<string | null>(null);

  readonly bookingForm = new FormGroup({
    patientId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    start: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    end: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  ngOnInit(): void {
    this.loadDoctors();
    this.loadPatients();
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
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to load schedule.',
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

  confirmAppointment(id: string): void {
    this.actionInProgress.set(id);
    this.scheduleService.confirmAppointment(id).subscribe({
      next: () => {
        this.actionInProgress.set(null);
        this.loadSchedule();
      },
      error: (error: HttpErrorResponse) => {
        this.actionInProgress.set(null);
        console.error('Confirm appointment failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to confirm appointment.',
        );
      },
    });
  }

  cancelAppointment(id: string): void {
    this.actionInProgress.set(id);
    this.scheduleService.cancelAppointment(id).subscribe({
      next: () => {
        this.actionInProgress.set(null);
        this.loadSchedule();
      },
      error: (error: HttpErrorResponse) => {
        this.actionInProgress.set(null);
        console.error('Cancel appointment failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to cancel appointment.',
        );
      },
    });
  }

  completeAppointment(id: string, status: 'Completed' | 'NoShow'): void {
    this.actionInProgress.set(id);
    this.scheduleService.completeAppointment(id, status).subscribe({
      next: () => {
        this.actionInProgress.set(null);
        this.loadSchedule();
      },
      error: (error: HttpErrorResponse) => {
        this.actionInProgress.set(null);
        console.error('Complete appointment failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to update appointment.',
        );
      },
    });
  }

  onSubmit(): void {
    if (this.bookingForm.invalid || !this.selectedDoctorId()) {
      this.bookingForm.markAllAsTouched();
      return;
    }

    const patientId = this.bookingForm.value.patientId ?? '';
    const start = this.bookingForm.value.start ?? '';
    const end = this.bookingForm.value.end ?? '';

    this.scheduleService.bookAppointment(patientId, this.selectedDoctorId(), start, end, true).subscribe({
      next: () => {
        this.bookingForm.reset();
        this.loadSchedule();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Book appointment failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to book appointment.',
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

  private loadPatients(): void {
    this.patientService.getPatientsList().subscribe({
      next: (patients) => {
        this.patients.set(patients);
      },
      error: (error) => {
        console.error('Patients load failed', error);
      },
    });
  }
}
