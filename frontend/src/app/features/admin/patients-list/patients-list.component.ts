import { DatePipe, NgFor } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';

import { PatientService } from '../../../core/services/patient.service';
import { PatientListItem } from '../../../models/patient.model';
import { getInitials } from '../../../shared/utils/initials.util';

@Component({
  selector: 'app-patients-list',
  standalone: true,
  imports: [DatePipe, NgFor, MatIconModule],
  templateUrl: './patients-list.component.html',
  styleUrl: './patients-list.component.scss',
})
export class PatientsListComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);

  readonly patients = signal<PatientListItem[]>([]);
  readonly searchTerm = signal('');
  readonly getInitials = getInitials;
  readonly filteredPatients = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();

    if (!term) {
      return this.patients();
    }

    return this.patients().filter((patient) => patient.fullName.toLowerCase().includes(term));
  });

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

  calculateAge(dob: string): number {
    const birthDate = new Date(dob);

    if (Number.isNaN(birthDate.getTime())) {
      return 0;
    }

    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDifference = today.getMonth() - birthDate.getMonth();

    if (
      monthDifference < 0 ||
      (monthDifference === 0 && today.getDate() < birthDate.getDate())
    ) {
      age -= 1;
    }

    return age;
  }
}
