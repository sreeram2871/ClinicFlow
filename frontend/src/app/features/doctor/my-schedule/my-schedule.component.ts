import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';

import { AuthService } from '../../../core/services/auth.service';
import { ScheduleService } from '../../../core/services/schedule.service';
import { DoctorSchedule } from '../../../models/doctor-schedule.model';

function formatDateForInput(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

@Component({
  selector: 'app-my-schedule',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf],
  templateUrl: './my-schedule.component.html',
  styleUrls: ['./my-schedule.component.scss'],
})
export class MyScheduleComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly scheduleService = inject(ScheduleService);

  readonly selectedDate = signal<string>(formatDateForInput(new Date()));
  readonly schedule = signal<DoctorSchedule | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadSchedule();
  }

  loadSchedule(): void {
    const doctorId = this.authService.currentUser()?.id;

    if (!doctorId) {
      this.errorMessage.set('Doctor identity is unavailable.');
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

  onDateChange(newDate: string): void {
    this.selectedDate.set(newDate);
    this.loadSchedule();
  }
}
