import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Prescription } from '../../models/prescription.model';

@Injectable({
  providedIn: 'root',
})
export class PrescriptionService {
  private readonly http = inject(HttpClient);

  getPrescriptions(patientId: string): Observable<Prescription[]> {
    return this.http.get<Prescription[]>(`https://localhost:7008/api/v1/patients/${patientId}/prescriptions`);
  }

  createPrescription(patientId: string, medicineName: string, dosage: string, notes: string): Observable<{ prescriptionId: string }> {
    return this.http.post<{ prescriptionId: string }>(`https://localhost:7008/api/v1/patients/${patientId}/prescriptions`, {
      medicineName,
      dosage,
      notes,
    });
  }
}
