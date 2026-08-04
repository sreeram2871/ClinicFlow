import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { StaffMember } from '../../models/staff.model';

@Injectable({
  providedIn: 'root',
})
export class StaffService {
  private readonly http = inject(HttpClient);

  getStaffList(): Observable<StaffMember[]> {
    return this.http.get<StaffMember[]>(`${environment.apiUrl}/auth/staff`);
  }

  createStaff(request: {
    fullName: string;
    email: string;
    password: string;
    role: string;
  }): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(
      `${environment.apiUrl}/auth/register-staff`,
      request,
    );
  }
}
