import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

import { MyAppointment } from '../../models/my-appointment.model';

interface PatientRecordResponse {
  id: string;
  fullName: string;
  dateOfBirth: string;
  phone: string;
  email: string;
  recentAppointments: Array<{
    id: string;
    scheduledStart: string;
    status: string;
    tokenNumber?: number | null;
    doctorName?: string;
  }>;
}

@Injectable({
  providedIn: 'root',
})
export class PatientAppointmentsService {
  private readonly http = inject(HttpClient);

  getMyAppointments(): Observable<MyAppointment[]> {
    return this.http.get<PatientRecordResponse>(`${environment.apiUrl}/patients/me`).pipe(
      map((response) => {
        const recentAppointments = response.recentAppointments ?? [];

        return recentAppointments.map((appointment) => {
          const start = appointment.scheduledStart;
          const startDate = new Date(start);
          const endDate = new Date(startDate.getTime() + 30 * 60 * 1000);

          return {
            appointmentId: appointment.id,
            start,
            end: endDate.toISOString(),
            status: appointment.status,
            tokenNumber: appointment.tokenNumber ?? null,
            doctorName: appointment.doctorName ?? '',
            appointmentDate: appointment.scheduledStart,
          };
        });
      }),
    );
  }

  getMyPatientId(): Observable<string> {
    return this.http.get<PatientRecordResponse>(`${environment.apiUrl}/patients/me`).pipe(
      map((response) => response.id),
    );
  }
}
