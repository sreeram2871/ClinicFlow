import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { DoctorListItem } from '../../models/doctor.model';
import { DoctorSchedule } from '../../models/doctor-schedule.model';

@Injectable({
  providedIn: 'root',
})
export class ScheduleService {
  private readonly http = inject(HttpClient);

  getSchedule(doctorId: string, date: string): Observable<DoctorSchedule> {
    return this.http.get<DoctorSchedule>(`${environment.apiUrl}/doctors/${doctorId}/schedule?date=${date}`);
  }

  getDoctors(): Observable<DoctorListItem[]> {
    return this.http.get<DoctorListItem[]>(`${environment.apiUrl}/doctors`);
  }

  bookAppointment(
    patientId: string,
    doctorId: string,
    start: string,
    end: string,
    bookedByStaff: boolean,
  ): Observable<{ appointmentId: string; status: string }> {
    return this.http.post<{ appointmentId: string; status: string }>(`${environment.apiUrl}/appointments`, {
      patientId,
      doctorId,
      start,
      end,
      bookedByStaff,
    });
  }

  confirmAppointment(id: string): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/appointments/${id}/confirm`, {});
  }

  cancelAppointment(id: string): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/appointments/${id}/cancel`, {});
  }

  completeAppointment(id: string, status: 'Completed' | 'NoShow'): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/appointments/${id}/complete`, { status });
  }

  recordPayment(appointmentId: string, amount: number, method: 'Cash' | 'Other'): Observable<{ paymentId: string }> {
    return this.http.post<{ paymentId: string }>(`${environment.apiUrl}/appointments/${appointmentId}/payment`, {
      amount,
      method,
    });
  }
}
