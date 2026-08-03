import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DoctorListItem } from '../../models/doctor.model';
import { DoctorSchedule } from '../../models/doctor-schedule.model';

@Injectable({
  providedIn: 'root',
})
export class ScheduleService {
  private readonly http = inject(HttpClient);

  getSchedule(doctorId: string, date: string): Observable<DoctorSchedule> {
    return this.http.get<DoctorSchedule>(`https://localhost:7008/api/v1/doctors/${doctorId}/schedule?date=${date}`);
  }

  getDoctors(): Observable<DoctorListItem[]> {
    return this.http.get<DoctorListItem[]>('https://localhost:7008/api/v1/doctors');
  }

  bookAppointment(
    patientId: string,
    doctorId: string,
    start: string,
    end: string,
    bookedByStaff: boolean,
  ): Observable<{ appointmentId: string; status: string }> {
    return this.http.post<{ appointmentId: string; status: string }>('https://localhost:7008/api/v1/appointments', {
      patientId,
      doctorId,
      start,
      end,
      bookedByStaff,
    });
  }

  confirmAppointment(id: string): Observable<void> {
    return this.http.patch<void>(`https://localhost:7008/api/v1/appointments/${id}/confirm`, {});
  }

  cancelAppointment(id: string): Observable<void> {
    return this.http.patch<void>(`https://localhost:7008/api/v1/appointments/${id}/cancel`, {});
  }

  completeAppointment(id: string, status: 'Completed' | 'NoShow'): Observable<void> {
    return this.http.patch<void>(`https://localhost:7008/api/v1/appointments/${id}/complete`, { status });
  }

  recordPayment(appointmentId: string, amount: number, method: 'Cash' | 'Other'): Observable<{ paymentId: string }> {
    return this.http.post<{ paymentId: string }>(`https://localhost:7008/api/v1/appointments/${appointmentId}/payment`, {
      amount,
      method,
    });
  }
}
