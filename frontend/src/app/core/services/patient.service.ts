import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { MedicalRecordEntry } from '../../models/medical-record.model';
import { PatientDetail } from '../../models/patient-detail.model';
import { PatientListItem } from '../../models/patient.model';

@Injectable({
  providedIn: 'root',
})
export class PatientService {
  private readonly http = inject(HttpClient);

  getPatientsList(): Observable<PatientListItem[]> {
    return this.http.get<PatientListItem[]>(`${environment.apiUrl}/patients`);
  }

  getPatientDetail(id: string): Observable<PatientDetail> {
    return this.http.get<PatientDetail>(`${environment.apiUrl}/patients/${id}`);
  }

  getMedicalHistory(id: string): Observable<MedicalRecordEntry[]> {
    return this.http.get<MedicalRecordEntry[]>(`${environment.apiUrl}/patients/${id}/records`);
  }

  addMedicalRecordEntry(id: string, notes: string, appointmentId: string | null): Observable<{ recordId: string }> {
    return this.http.post<{ recordId: string }>(`${environment.apiUrl}/patients/${id}/records`, {
      notes,
      appointmentId,
    });
  }

  registerWalkInPatient(fullName: string, dateOfBirth: string, phone: string, email: string): Observable<{ patientId: string }> {
    return this.http.post<{ patientId: string }>(`${environment.apiUrl}/patients`, {
      fullName,
      dateOfBirth,
      phone,
      email,
    });
  }
}
