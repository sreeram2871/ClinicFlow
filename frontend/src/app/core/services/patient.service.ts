import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { PatientListItem } from '../../models/patient.model';

@Injectable({
  providedIn: 'root',
})
export class PatientService {
  private readonly http = inject(HttpClient);

  getPatientsList(): Observable<PatientListItem[]> {
    return this.http.get<PatientListItem[]>('https://localhost:7008/api/v1/patients');
  }
}
