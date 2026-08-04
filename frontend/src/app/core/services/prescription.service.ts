import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { Prescription } from '../../models/prescription.model';

@Injectable({
  providedIn: 'root',
})
export class PrescriptionService {
  private readonly http = inject(HttpClient);

  getPrescriptions(patientId: string): Observable<Prescription[]> {
    return this.http.get<Prescription[]>(`${environment.apiUrl}/patients/${patientId}/prescriptions`);
  }

  createPrescription(patientId: string, medicineName: string, dosage: string, notes: string): Observable<{ prescriptionId: string }> {
    return this.http.post<{ prescriptionId: string }>(`${environment.apiUrl}/patients/${patientId}/prescriptions`, {
      medicineName,
      dosage,
      notes,
    });
  }
}
