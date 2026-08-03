import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { MedicalRecordEntry } from '../../models/medical-record.model';
import { PatientDetail } from '../../models/patient-detail.model';
import { PatientListItem } from '../../models/patient.model';

@Injectable({
  providedIn: 'root',
})
export class PatientService {
  private readonly http = inject(HttpClient);

  getPatientsList(): Observable<PatientListItem[]> {
    return this.http.get<PatientListItem[]>('https://localhost:7008/api/v1/patients');
  }

  getPatientDetail(id: string): Observable<PatientDetail> {
    return this.http.get<PatientDetail>(`https://localhost:7008/api/v1/patients/${id}`);
  }

  getMedicalHistory(id: string): Observable<MedicalRecordEntry[]> {
    return this.http.get<MedicalRecordEntry[]>(`https://localhost:7008/api/v1/patients/${id}/records`);
  }

  addMedicalRecordEntry(id: string, notes: string, appointmentId: string | null): Observable<{ recordId: string }> {
    return this.http.post<{ recordId: string }>(`https://localhost:7008/api/v1/patients/${id}/records`, {
      notes,
      appointmentId,
    });
  }

  registerWalkInPatient(fullName: string, dateOfBirth: string, phone: string, email: string): Observable<{ patientId: string }> {
    return this.http.post<{ patientId: string }>('https://localhost:7008/api/v1/patients', {
      fullName,
      dateOfBirth,
      phone,
      email,
    });
  }
}
