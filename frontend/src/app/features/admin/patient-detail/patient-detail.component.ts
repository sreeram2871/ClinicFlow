import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { PatientService } from '../../../core/services/patient.service';
import { PatientDetail } from '../../../models/patient-detail.model';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf],
  templateUrl: './patient-detail.component.html',
  styleUrls: ['./patient-detail.component.scss'],
})
export class PatientDetailComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  readonly route = inject(ActivatedRoute);

  readonly patientId = this.route.snapshot.paramMap.get('id') ?? '';
  readonly patient = signal<PatientDetail | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly activeTab = signal<'profile' | 'history' | 'prescriptions'>('profile');

  ngOnInit(): void {
    this.patientService.getPatientDetail(this.patientId).subscribe({
      next: (patient) => {
        this.patient.set(patient);
        this.errorMessage.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Patient detail load failed', error);

        const detail = error.error?.detail;
        this.errorMessage.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to load patient details.',
        );
      },
    });
  }

  setTab(tab: 'profile' | 'history' | 'prescriptions'): void {
    this.activeTab.set(tab);
  }
}
