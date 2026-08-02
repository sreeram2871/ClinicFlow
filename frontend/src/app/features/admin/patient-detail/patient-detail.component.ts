import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { PatientService } from '../../../core/services/patient.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { PatientDetail } from '../../../models/patient-detail.model';
import { MedicalRecordEntry } from '../../../models/medical-record.model';
import { Prescription } from '../../../models/prescription.model';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule],
  templateUrl: './patient-detail.component.html',
  styleUrls: ['./patient-detail.component.scss'],
})
export class PatientDetailComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly patientService = inject(PatientService);
  private readonly prescriptionService = inject(PrescriptionService);
  readonly route = inject(ActivatedRoute);

  readonly patientId = this.route.snapshot.paramMap.get('id') ?? '';
  readonly patient = signal<PatientDetail | null>(null);
  readonly medicalHistory = signal<MedicalRecordEntry[]>([]);
  readonly prescriptions = signal<Prescription[]>([]);
  readonly historyLoaded = signal(false);
  readonly prescriptionsLoaded = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly historyError = signal<string | null>(null);
  readonly prescriptionsError = signal<string | null>(null);
  readonly isDoctor = computed(() => this.authService.currentUser()?.role === 'Doctor');
  readonly activeTab = signal<'profile' | 'history' | 'prescriptions'>('profile');

  readonly historyForm = new FormGroup({
    notes: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  readonly prescriptionForm = new FormGroup({
    medicineName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    dosage: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    notes: new FormControl('', {
      nonNullable: true,
    }),
  });

  ngOnInit(): void {
    this.patientService.getPatientDetail(this.patientId).subscribe({
      next: (patient) => {
        this.patient.set(patient);
        this.profileError.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Patient detail load failed', error);

        const detail = error.error?.detail;
        this.profileError.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to load patient details.',
        );
      },
    });
  }

  setTab(tab: 'profile' | 'history' | 'prescriptions'): void {
    this.activeTab.set(tab);

    if (tab === 'history' && !this.historyLoaded()) {
      this.loadHistory();
    }

    if (tab === 'prescriptions' && !this.prescriptionsLoaded()) {
      this.loadPrescriptions();
    }
  }

  saveHistoryEntry(): void {
    if (this.historyForm.invalid) {
      this.historyForm.markAllAsTouched();
      return;
    }

    const notes = this.historyForm.value.notes?.trim() ?? '';

    this.patientService.addMedicalRecordEntry(this.patientId, notes, null).subscribe({
      next: () => {
        this.historyForm.reset();
        this.loadHistory();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Medical record creation failed', error);

        const detail = error.error?.detail;
        this.historyError.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to save medical note.',
        );
      },
    });
  }

  savePrescription(): void {
    if (this.prescriptionForm.invalid) {
      this.prescriptionForm.markAllAsTouched();
      return;
    }

    const medicineName = this.prescriptionForm.value.medicineName?.trim() ?? '';
    const dosage = this.prescriptionForm.value.dosage?.trim() ?? '';
    const notes = this.prescriptionForm.value.notes?.trim() ?? '';

    this.prescriptionService.createPrescription(this.patientId, medicineName, dosage, notes).subscribe({
      next: () => {
        this.prescriptionForm.reset();
        this.loadPrescriptions();
      },
      error: (error: HttpErrorResponse) => {
        console.error('Prescription creation failed', error);

        const detail = error.error?.detail;
        this.prescriptionsError.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to save prescription.',
        );
      },
    });
  }

  private loadHistory(): void {
    this.patientService.getMedicalHistory(this.patientId).subscribe({
      next: (history) => {
        this.medicalHistory.set(history);
        this.historyLoaded.set(true);
        this.historyError.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Medical history load failed', error);

        const detail = error.error?.detail;
        this.historyError.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to load medical history.',
        );
      },
    });
  }

  private loadPrescriptions(): void {
    this.prescriptionService.getPrescriptions(this.patientId).subscribe({
      next: (prescriptions) => {
        this.prescriptions.set(prescriptions);
        this.prescriptionsLoaded.set(true);
        this.prescriptionsError.set(null);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Prescriptions load failed', error);

        const detail = error.error?.detail;
        this.prescriptionsError.set(
          typeof detail === 'string' && detail.trim().length > 0
            ? detail
            : 'Unable to load prescriptions.',
        );
      },
    });
  }
}
