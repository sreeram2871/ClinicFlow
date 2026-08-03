import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';

import { ScheduleService } from '../../../core/services/schedule.service';
import { PatientAppointmentsService } from '../../../core/services/patient-appointments.service';
import { DoctorListItem } from '../../../models/doctor.model';
import { AvailableSlot, DoctorSchedule } from '../../../models/doctor-schedule.model';
import { MyAppointment } from '../../../models/my-appointment.model';

function formatDateForInput(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

@Component({
  selector: 'app-my-appointments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './my-appointments.component.html',
  styleUrls: ['./my-appointments.component.scss'],
})
export class MyAppointmentsComponent implements OnInit {
  private readonly patientAppointmentsService = inject(PatientAppointmentsService);
  private readonly scheduleService = inject(ScheduleService);

  readonly myPatientId = signal<string>('');
  readonly appointments = signal<MyAppointment[]>([]);
  readonly doctors = signal<DoctorListItem[]>([]);
  readonly selectedDoctorId = signal<string>('');
  readonly selectedDate = signal<string>(formatDateForInput(new Date()));
  readonly availableSlots = signal<AvailableSlot[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.patientAppointmentsService.getMyPatientId().subscribe({
      next: (patientId) => {
        this.myPatientId.set(patientId);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Get my patient id failed', error);
        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to load your patient profile.',
        );
      },
    });

    this.loadMyAppointments();
    this.loadDoctors();
  }

  loadMyAppointments(): void {
    this.patientAppointmentsService.getMyAppointments().subscribe({
      next: (appointments) => {
        this.appointments.set(appointments);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Load my appointments failed', error);
        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to load your appointments.',
        );
      },
    });
  }

  onDoctorChange(id: string): void {
    this.selectedDoctorId.set(id);
    this.loadAvailableSlots();
  }

  onDateChange(date: string): void {
    this.selectedDate.set(date);
    this.loadAvailableSlots();
  }

  loadAvailableSlots(): void {
    const doctorId = this.selectedDoctorId();

    if (!doctorId) {
      this.availableSlots.set([]);
      return;
    }

    this.scheduleService.getSchedule(doctorId, this.selectedDate()).subscribe({
      next: (schedule: DoctorSchedule) => {
        this.availableSlots.set(schedule.availableSlots ?? []);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Load available slots failed', error);
        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to load available slots.',
        );
      },
    });
  }

  bookSlot(slot: AvailableSlot): void {
    if (!this.myPatientId()) {
      this.errorMessage.set('Your patient profile is not available yet.');
      return;
    }

    if (!this.selectedDoctorId()) {
      this.errorMessage.set('Please select a doctor before booking.');
      return;
    }

    this.scheduleService.bookAppointment(this.myPatientId(), this.selectedDoctorId(), slot.start, slot.end, false).subscribe({
      next: () => {
        this.successMessage.set('Appointment booked successfully.');
        this.loadAvailableSlots();
        this.loadMyAppointments();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Book appointment failed', error);
        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to book appointment.',
        );
      },
    });
  }

  cancelMyAppointment(id: string): void {
    this.scheduleService.cancelAppointment(id).subscribe({
      next: () => {
        this.loadMyAppointments();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Cancel appointment failed', error);
        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0 ? detail : 'Unable to cancel appointment.',
        );
      },
    });
  }

  private loadDoctors(): void {
    this.scheduleService.getDoctors().subscribe({
      next: (doctors) => {
        this.doctors.set(doctors);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Load doctors failed', error);
      },
    });
  }
}
