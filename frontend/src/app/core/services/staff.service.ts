import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { StaffMember } from '../../models/staff.model';

@Injectable({
  providedIn: 'root',
})
export class StaffService {
  private readonly http = inject(HttpClient);

  getStaffList(): Observable<StaffMember[]> {
    return this.http.get<StaffMember[]>('https://localhost:7008/api/v1/auth/staff');
  }

  createStaff(request: {
    fullName: string;
    email: string;
    password: string;
    role: string;
  }): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(
      'https://localhost:7008/api/v1/auth/register-staff',
      request,
    );
  }
}
