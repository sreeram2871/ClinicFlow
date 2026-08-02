import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DoctorSchedule } from '../../models/doctor-schedule.model';

@Injectable({
  providedIn: 'root',
})
export class ScheduleService {
  private readonly http = inject(HttpClient);

  getSchedule(doctorId: string, date: string): Observable<DoctorSchedule> {
    return this.http.get<DoctorSchedule>(`https://localhost:7008/api/v1/doctors/${doctorId}/schedule?date=${date}`);
  }
}
