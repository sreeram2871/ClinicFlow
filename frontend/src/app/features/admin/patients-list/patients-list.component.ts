import { DatePipe, NgFor } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';

import { PatientService } from '../../../core/services/patient.service';
import { PatientListItem } from '../../../models/patient.model';

@Component({
  selector: 'app-patients-list',
  standalone: true,
  imports: [DatePipe, NgFor],
  templateUrl: './patients-list.component.html',
  styleUrl: './patients-list.component.scss',
})
export class PatientsListComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);

  readonly patients = signal<PatientListItem[]>([]);

  ngOnInit(): void {
    this.patientService.getPatientsList().subscribe({
      next: (patients) => {
        this.patients.set(patients);
      },
      error: (error) => {
        console.error('Patients list load failed', error);
      },
    });
  }

  goToPatient(id: string): void {
    this.router.navigateByUrl(`/dashboard/patients/${id}`);
  }
}
